using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Models.ViewModels;
using System.Security.Claims;

namespace PhishGuard.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly PhishGuardContext _db;

    public DashboardController(PhishGuardContext db)
    {
        _db = db;
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var employeeId = GetEmployeeId();
        var employee = await _db.Employees.FindAsync(employeeId);
        if (employee == null) return RedirectToAction("Login", "Account");

        var emails = await _db.Emails
            .Where(e => e.RecipientId == employeeId)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(50)
            .Include(e => e.Scores)
            .ToListAsync();

        var training = await _db.EmployeeTrainings
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

        var emailViewModels = emails.Select(e => new EmailViewModel
        {
            EmailId = e.EmailId,
            SenderAddress = e.SenderAddress,
            SenderDisplayName = e.SenderDisplayName,
            Subject = e.Subject,
            BodyPreview = e.BodyPreview,
            ReceivedAt = e.ReceivedAt,
            OverallScore = e.OverallScore,
            Classification = e.Classification,
            IsReported = e.IsReported,
            WarningReasons = e.Scores
                .Where(s => s.Score > 0)
                .OrderByDescending(s => s.Score * s.Weight)
                .Select(s => s.Details ?? s.Dimension.ToString())
                .ToList(),
            Scores = e.Scores.Select(s => new EmailScoreViewModel
            {
                Dimension = s.Dimension.ToString(),
                Score = s.Score,
                Details = s.Details
            }).ToList()
        }).ToList();

        var model = new DashboardViewModel
        {
            Employee = employee,
            RecentEmails = emailViewModels,
            Training = training,
            Stats = new DashboardStats
            {
                TotalEmails = await _db.Emails.CountAsync(e => e.RecipientId == employeeId),
                BlockedCount = await _db.Emails.CountAsync(e => e.RecipientId == employeeId && e.Classification == EmailClassification.Blocked),
                WarningCount = await _db.Emails.CountAsync(e => e.RecipientId == employeeId && e.Classification == EmailClassification.Warning),
                SafeCount = await _db.Emails.CountAsync(e => e.RecipientId == employeeId && e.Classification == EmailClassification.Safe),
                ReportedCount = await _db.PhishingReports.CountAsync(r => r.ReportedBy == employeeId),
                TrainingPoints = training?.ScorePoints ?? 0,
                CurrentStreak = training?.CurrentStreak ?? 0
            }
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReportPhishing(int emailId, string? reason)
    {
        var employeeId = GetEmployeeId();
        var email = await _db.Emails.FindAsync(emailId);

        if (email == null || email.RecipientId != employeeId)
            return NotFound();

        if (await _db.PhishingReports.AnyAsync(r => r.EmailId == emailId && r.ReportedBy == employeeId))
            return RedirectToAction("Index");

        var report = new PhishingReport
        {
            EmailId = emailId,
            ReportedBy = employeeId,
            ReportReason = reason
        };

        _db.PhishingReports.Add(report);

        email.IsReported = true;
        email.ReportedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Email reported as phishing. An admin will review your report — if confirmed, you'll earn points!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> EmailDetails(int id)
    {
        var employeeId = GetEmployeeId();
        var email = await _db.Emails
            .Include(e => e.Scores)
            .Include(e => e.Urls)
            .FirstOrDefaultAsync(e => e.EmailId == id && e.RecipientId == employeeId);

        if (email == null) return NotFound();

        return View(email);
    }
}
