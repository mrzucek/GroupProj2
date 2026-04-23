using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Services;
using System.Security.Claims;

namespace PhishGuard.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly PhishGuardContext _db;
    private readonly OpenAiEmailGeneratorService _emailGenerator;

    public AdminController(PhishGuardContext db, OpenAiEmailGeneratorService emailGenerator)
    {
        _db = db;
        _emailGenerator = emailGenerator;
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Admin dashboard — overview of system health and key metrics.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var model = new AdminDashboardViewModel
        {
            // Email stats
            TotalEmailsScanned = await _db.Emails.CountAsync(),
            TotalBlocked = await _db.Emails.CountAsync(e => e.Classification == EmailClassification.Blocked),
            TotalWarnings = await _db.Emails.CountAsync(e => e.Classification == EmailClassification.Warning),
            TotalSafe = await _db.Emails.CountAsync(e => e.Classification == EmailClassification.Safe),

            // Reports
            PendingReports = await _db.PhishingReports
                .CountAsync(r => r.IsConfirmedPhishing == null),
            ConfirmedPhishing = await _db.PhishingReports
                .CountAsync(r => r.IsConfirmedPhishing == true),

            // Training
            TotalEmployees = await _db.Employees.CountAsync(e => e.IsActive),
            TotalSimulationsSent = await _db.SimulationEmails.CountAsync(),
            SimulationsClicked = await _db.SimulationEmails
                .CountAsync(s => s.Result == SimulationResult.Clicked),
            SimulationsReported = await _db.SimulationEmails
                .CountAsync(s => s.Result == SimulationResult.Reported),

            // Threat database
            ActiveThreatIndicators = await _db.ThreatIndicators.CountAsync(t => t.IsActive),
            ActiveScoringRules = await _db.ScoringRules.CountAsync(r => r.IsActive),

            // Recent reports for review
            RecentReports = await _db.PhishingReports
                .Include(r => r.Email)
                .Include(r => r.Reporter)
                .Include(r => r.Reviewer)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync(),

            // Employee performance
            EmployeeStats = await _db.EmployeeTrainings
                .Include(t => t.Employee)
                .Where(t => t.Employee.IsActive)
                .OrderByDescending(t => t.ScorePoints)
                .ToListAsync(),

            // Top attacked senders
            TopPhishingSenders = await _db.Emails
                .Where(e => e.Classification == EmailClassification.Blocked)
                .GroupBy(e => e.SenderAddress)
                .Select(g => new SenderStat { SenderAddress = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToListAsync()
        };

        // Calculate company security score
        if (model.TotalSimulationsSent > 0)
        {
            model.CompanySecurityScore = Math.Round(
                (decimal)model.SimulationsReported / model.TotalSimulationsSent * 100, 1);
        }

        return View(model);
    }

    /// <summary>
    /// Review a phishing report — confirm or reject.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReviewReport(int reportId, bool isPhishing)
    {
        var report = await _db.PhishingReports
            .Include(r => r.Email)
            .FirstOrDefaultAsync(r => r.ReportId == reportId);

        if (report == null) return NotFound();

        report.IsConfirmedPhishing = isPhishing;
        report.ReviewedBy = GetEmployeeId();
        report.ReviewedAt = DateTime.UtcNow;

        // Award points to reporter if confirmed as phishing
        if (isPhishing)
        {
            var training = await _db.EmployeeTrainings
                .FirstOrDefaultAsync(t => t.EmployeeId == report.ReportedBy);

            if (training != null)
            {
                training.ScorePoints += 10;
                training.CurrentStreak++;
                if (training.CurrentStreak > training.BestStreak)
                    training.BestStreak = training.CurrentStreak;
            }

            // Add sender to threat database if not already there
            var senderExists = await _db.ThreatIndicators
                .AnyAsync(t => t.Type == IndicatorType.SenderEmail
                    && t.Value == report.Email.SenderAddress.ToLowerInvariant());

            if (!senderExists)
            {
                _db.ThreatIndicators.Add(new ThreatIndicator
                {
                    Type = IndicatorType.SenderEmail,
                    Value = report.Email.SenderAddress.ToLowerInvariant(),
                    Source = "Employee Report",
                    Severity = ThreatSeverity.High
                });
            }

            // Also add domain
            var domain = report.Email.SenderAddress.Split('@').LastOrDefault()?.ToLowerInvariant();
            if (domain != null)
            {
                var domainExists = await _db.ThreatIndicators
                    .AnyAsync(t => t.Type == IndicatorType.Domain && t.Value == domain);

                if (!domainExists)
                {
                    _db.ThreatIndicators.Add(new ThreatIndicator
                    {
                        Type = IndicatorType.Domain,
                        Value = domain,
                        Source = "Employee Report",
                        Severity = ThreatSeverity.Medium
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = isPhishing
            ? "Report confirmed as phishing. Sender added to threat database and reporter awarded 10 points."
            : "Report marked as not phishing.";

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Manage scoring rules.
    /// </summary>
    public async Task<IActionResult> ScoringRules()
    {
        var rules = await _db.ScoringRules.OrderBy(r => r.Dimension).ThenBy(r => r.RuleName).ToListAsync();
        return View(rules);
    }

    /// <summary>
    /// Toggle a scoring rule on/off.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleRule(int ruleId)
    {
        var rule = await _db.ScoringRules.FindAsync(ruleId);
        if (rule == null) return NotFound();

        rule.IsActive = !rule.IsActive;
        rule.UpdatedBy = GetEmployeeId();
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Rule '{rule.RuleName}' {(rule.IsActive ? "enabled" : "disabled")}.";
        return RedirectToAction("ScoringRules");
    }

    /// <summary>
    /// View threat indicator database.
    /// </summary>
    public async Task<IActionResult> ThreatDatabase()
    {
        var indicators = await _db.ThreatIndicators
            .OrderByDescending(t => t.LastSeen)
            .Take(100)
            .ToListAsync();
        return View(indicators);
    }

    /// <summary>
    /// Send simulations to all active employees.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendCompanySimulation()
    {
        var employees = await _db.Employees
            .Where(e => e.IsActive && e.Role == EmployeeRole.Employee)
            .ToListAsync();

        var trainings = await _db.EmployeeTrainings.ToListAsync();
        int sent = 0;

        foreach (var employee in employees)
        {
            var training = trainings.FirstOrDefault(t => t.EmployeeId == employee.EmployeeId);
            if (training == null) continue;

            var campaign = await _db.PhishingCampaigns
                .Where(c => c.IsActive && c.Difficulty == training.CurrentDifficulty)
                .OrderBy(_ => EF.Functions.Random())
                .FirstOrDefaultAsync();

            if (campaign == null) continue;

            _db.SimulationEmails.Add(new SimulationEmail
            {
                CampaignId = campaign.CampaignId,
                TargetEmployeeId = employee.EmployeeId,
                RequiresLoginQuiz = true
            });

            training.TotalSimulationsReceived++;
            training.LastSimulationAt = DateTime.UtcNow;
            sent++;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Sent simulated phishing emails to {sent} employees at their individual difficulty levels.";
        return RedirectToAction("Index");
    }

    // ── Trusted Domains ──────────────────────────────────────────────────────

    public async Task<IActionResult> TrustedDomains()
    {
        var domains = await _db.TrustedDomains
            .Include(t => t.CreatedBy)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return View(domains);
    }

    [HttpPost]
    public async Task<IActionResult> AddTrustedDomain(string domain, string? notes)
    {
        // Normalize
        var normalized = domain?.Trim()
            .Replace("https://", "").Replace("http://", "")
            .TrimStart('w').TrimStart('w').TrimStart('w').TrimStart('.')
            .TrimEnd('/') ?? "";

        // Simpler normalization: strip scheme and www
        if (normalized.StartsWith("www.")) normalized = normalized[4..];

        if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains(' '))
        {
            TempData["Error"] = "Invalid domain. Please enter a valid domain without spaces.";
            return RedirectToAction("TrustedDomains");
        }

        var duplicate = await _db.TrustedDomains.AnyAsync(t => t.Domain.ToLower() == normalized.ToLower());
        if (duplicate)
        {
            TempData["Error"] = $"'{normalized}' is already on the trusted list.";
            return RedirectToAction("TrustedDomains");
        }

        _db.TrustedDomains.Add(new TrustedDomain
        {
            Domain = normalized.ToLower(),
            Notes = notes?.Trim(),
            CreatedById = GetEmployeeId(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"'{normalized}' added to trusted domains.";
        return RedirectToAction("TrustedDomains");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTrustedDomain(int id)
    {
        var domain = await _db.TrustedDomains.FindAsync(id);
        if (domain != null)
        {
            _db.TrustedDomains.Remove(domain);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"'{domain.Domain}' removed from trusted list.";
        }
        return RedirectToAction("TrustedDomains");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTrustedDomain(int trustedDomainId, string domain, string? notes)
    {
        var entry = await _db.TrustedDomains.FindAsync(trustedDomainId);
        if (entry == null) return NotFound();

        var normalized = domain?.Trim().Replace("https://", "").Replace("http://", "").TrimEnd('/') ?? "";
        if (normalized.StartsWith("www.")) normalized = normalized[4..];

        entry.Domain = normalized.ToLower();
        entry.Notes = notes?.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = "Trusted domain updated.";
        return RedirectToAction("TrustedDomains");
    }

    // ── Employee Management ───────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddEmployee(string displayName, string email, string password, string? department, string role)
    {
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Display name, email, and password are required.";
            return RedirectToAction("Index");
        }

        var exists = await _db.Employees.AnyAsync(e => e.Email.ToLower() == email.ToLower());
        if (exists)
        {
            TempData["Error"] = $"An account with email '{email}' already exists.";
            return RedirectToAction("Index");
        }

        var employeeRole = role == "Admin" ? EmployeeRole.Admin : EmployeeRole.Employee;
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        var employee = new Employee
        {
            Email = email.Trim().ToLower(),
            DisplayName = displayName.Trim(),
            Department = department?.Trim(),
            Role = employeeRole,
            PasswordHash = hash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        _db.EmployeeTrainings.Add(new EmployeeTraining { EmployeeId = employee.EmployeeId });
        await _db.SaveChangesAsync();

        // Seed inbox for the new employee
        await _emailGenerator.SeedEmailsForUserAsync(employee.EmployeeId, employee.DisplayName, employee.Email);

        TempData["Success"] = $"Employee '{displayName}' created successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var currentId = GetEmployeeId();

        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
        {
            TempData["Error"] = "Employee not found.";
            return RedirectToAction("Index");
        }
        if (employee.Role == EmployeeRole.Admin)
        {
            TempData["Error"] = "Admin accounts cannot be deleted.";
            return RedirectToAction("Index");
        }
        if (employee.EmployeeId == currentId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction("Index");
        }

        // Remove related records if cascade isn't configured
        var emails = _db.Emails.Where(e => e.RecipientId == id);
        _db.Emails.RemoveRange(emails);

        var sims = _db.SimulationEmails.Where(s => s.TargetEmployeeId == id);
        _db.SimulationEmails.RemoveRange(sims);

        var training = await _db.EmployeeTrainings.FirstOrDefaultAsync(t => t.EmployeeId == id);
        if (training != null) _db.EmployeeTrainings.Remove(training);

        var reports = _db.PhishingReports.Where(r => r.ReportedBy == id);
        _db.PhishingReports.RemoveRange(reports);

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Employee '{employee.DisplayName}' deleted.";
        return RedirectToAction("Index");
    }
}

public class AdminDashboardViewModel
{
    public int TotalEmailsScanned { get; set; }
    public int TotalBlocked { get; set; }
    public int TotalWarnings { get; set; }
    public int TotalSafe { get; set; }
    public int PendingReports { get; set; }
    public int ConfirmedPhishing { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalSimulationsSent { get; set; }
    public int SimulationsClicked { get; set; }
    public int SimulationsReported { get; set; }
    public decimal CompanySecurityScore { get; set; }
    public int ActiveThreatIndicators { get; set; }
    public int ActiveScoringRules { get; set; }
    public List<PhishingReport> RecentReports { get; set; } = new();
    public List<EmployeeTraining> EmployeeStats { get; set; } = new();
    public List<SenderStat> TopPhishingSenders { get; set; } = new();
}

public class SenderStat
{
    public string SenderAddress { get; set; } = string.Empty;
    public int Count { get; set; }
}
