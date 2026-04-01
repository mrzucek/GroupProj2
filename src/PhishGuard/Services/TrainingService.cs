using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Services.Training;

namespace PhishGuard.Services;

public class TrainingService
{
    private readonly PhishGuardContext _db;
    private readonly PhishingGeneratorService _generator;

    public TrainingService(PhishGuardContext db, PhishingGeneratorService generator)
    {
        _db = db;
        _generator = generator;
    }

    /// <summary>
    /// Send a simulated phishing email to an employee at their current difficulty level.
    /// Tries AI generation first, falls back to static campaigns.
    /// </summary>
    public async Task<SimulationEmail?> SendSimulationAsync(int employeeId)
    {
        var training = await _db.EmployeeTrainings
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

        if (training == null) return null;

        // Try AI-generated phishing email first
        PhishingCampaign? campaign = null;
        var employee = await _db.Employees.FindAsync(employeeId);
        if (employee != null)
        {
            campaign = await _generator.GeneratePhishingEmailAsync(
                training.CurrentDifficulty, employee.DisplayName, employee.Department, employeeId);
        }

        // Fall back to static campaigns
        if (campaign == null)
        {
            campaign = await _db.PhishingCampaigns
                .Where(c => c.IsActive && c.Difficulty == training.CurrentDifficulty)
                .OrderBy(_ => EF.Functions.Random())
                .FirstOrDefaultAsync();
        }

        if (campaign == null)
        {
            campaign = await _db.PhishingCampaigns
                .Where(c => c.IsActive)
                .OrderBy(_ => EF.Functions.Random())
                .FirstOrDefaultAsync();
        }

        if (campaign == null) return null;

        var simulation = new SimulationEmail
        {
            CampaignId = campaign.CampaignId,
            TargetEmployeeId = employeeId,
            SentAt = DateTime.UtcNow
        };

        _db.SimulationEmails.Add(simulation);
        training.TotalSimulationsReceived++;
        training.LastSimulationAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return simulation;
    }

    /// <summary>
    /// Record that an employee clicked a simulated phishing link.
    /// </summary>
    public async Task<SimulationEmail?> RecordClickAsync(int simulationId, int employeeId)
    {
        var sim = await _db.SimulationEmails
            .Include(s => s.Campaign)
            .FirstOrDefaultAsync(s => s.SimulationId == simulationId && s.TargetEmployeeId == employeeId);

        if (sim == null || sim.Result != SimulationResult.Pending) return sim;

        sim.ClickedLinkAt = DateTime.UtcNow;
        sim.Result = SimulationResult.Clicked;

        var training = await _db.EmployeeTrainings
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

        if (training != null)
        {
            training.TotalClicked++;
            training.CurrentStreak = 0; // Reset streak on click

            // Decrease difficulty if struggling
            if (training.TotalClicked > 0 &&
                training.TotalClicked % 3 == 0 &&
                training.CurrentDifficulty > Difficulty.Easy)
            {
                training.CurrentDifficulty--;
                training.DifficultyUpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return sim;
    }

    /// <summary>
    /// Record that an employee correctly reported a simulated phishing email.
    /// </summary>
    public async Task<SimulationEmail?> RecordReportAsync(int simulationId, int employeeId)
    {
        var sim = await _db.SimulationEmails
            .Include(s => s.Campaign)
            .FirstOrDefaultAsync(s => s.SimulationId == simulationId && s.TargetEmployeeId == employeeId);

        if (sim == null || sim.Result != SimulationResult.Pending) return sim;

        sim.ReportedAt = DateTime.UtcNow;
        sim.Result = SimulationResult.Reported;

        var training = await _db.EmployeeTrainings
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId);

        if (training != null)
        {
            training.TotalCorrectlyReported++;
            training.CurrentStreak++;
            if (training.CurrentStreak > training.BestStreak)
                training.BestStreak = training.CurrentStreak;

            // Award points based on difficulty
            var points = sim.Campaign.Difficulty switch
            {
                Difficulty.Easy => 5,
                Difficulty.Medium => 10,
                Difficulty.Hard => 20,
                Difficulty.Expert => 40,
                _ => 10
            };
            training.ScorePoints += points;

            // Increase difficulty after consistent success
            if (training.CurrentStreak > 0 &&
                training.CurrentStreak % 5 == 0 &&
                training.CurrentDifficulty < Difficulty.Expert)
            {
                training.CurrentDifficulty++;
                training.DifficultyUpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return sim;
    }

    /// <summary>
    /// Get leaderboard data ranked by points.
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
    {
        return await _db.EmployeeTrainings
            .Include(t => t.Employee)
            .Where(t => t.Employee.IsActive)
            .OrderByDescending(t => t.ScorePoints)
            .ThenByDescending(t => t.BestStreak)
            .Select(t => new LeaderboardEntry
            {
                EmployeeId = t.EmployeeId,
                DisplayName = t.Employee.DisplayName,
                Department = t.Employee.Department,
                ScorePoints = t.ScorePoints,
                CurrentStreak = t.CurrentStreak,
                BestStreak = t.BestStreak,
                CurrentDifficulty = t.CurrentDifficulty,
                TotalSimulations = t.TotalSimulationsReceived,
                TotalReported = t.TotalCorrectlyReported,
                TotalClicked = t.TotalClicked,
                ReportRate = t.TotalSimulationsReceived > 0
                    ? Math.Round((decimal)t.TotalCorrectlyReported / t.TotalSimulationsReceived * 100, 1)
                    : 0
            })
            .ToListAsync();
    }

    /// <summary>
    /// Get pending (unanswered) simulations for an employee.
    /// </summary>
    public async Task<List<SimulationEmail>> GetPendingSimulationsAsync(int employeeId)
    {
        return await _db.SimulationEmails
            .Include(s => s.Campaign)
            .Where(s => s.TargetEmployeeId == employeeId && s.Result == SimulationResult.Pending)
            .OrderByDescending(s => s.SentAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get simulation history for an employee.
    /// </summary>
    public async Task<List<SimulationEmail>> GetHistoryAsync(int employeeId)
    {
        return await _db.SimulationEmails
            .Include(s => s.Campaign)
            .Where(s => s.TargetEmployeeId == employeeId)
            .OrderByDescending(s => s.SentAt)
            .Take(50)
            .ToListAsync();
    }

    /// <summary>
    /// Seed default phishing campaign templates across all difficulty levels.
    /// </summary>
    public async Task SeedCampaignsAsync(int adminId)
    {
        if (await _db.PhishingCampaigns.AnyAsync()) return;

        var campaigns = new List<PhishingCampaign>
        {
            // Easy
            new()
            {
                CampaignName = "Nigerian Prince",
                Difficulty = Difficulty.Easy,
                TemplateSubject = "URGENT: I Need Your Help Transferring $15,000,000",
                TemplateBody = "Dear friend, I am Prince Abubakar from Nigeria. I have $15,000,000 USD that I need to transfer out of the country. I will give you 30% if you help me. Please send me your bank account details immediately. God bless you.",
                TemplateSender = "prince.abubakar@mail-nigeria.xyz",
                PhishingIndicators = "Nigerian prince scam; requests bank details; unrealistic money offer; suspicious domain (.xyz); urgency language",
                CreatedBy = adminId
            },
            new()
            {
                CampaignName = "Obvious Prize Scam",
                Difficulty = Difficulty.Easy,
                TemplateSubject = "CONGRATULATIONS!!! You Won $1,000 Walmart Gift Card!!!",
                TemplateBody = "YOU HAVE BEEN SELECTED AS TODAY'S LUCKY WINNER!!! Click here NOW to claim your FREE $1,000 Walmart Gift Card! This offer expires in 1 HOUR! Verify your account to receive your prize immediately!!!",
                TemplateSender = "winner@free-prizes-today.tk",
                PhishingIndicators = "Prize scam; excessive punctuation; all caps; extreme urgency; suspicious TLD (.tk); too good to be true",
                CreatedBy = adminId
            },
            new()
            {
                CampaignName = "Bad Grammar Phishing",
                Difficulty = Difficulty.Easy,
                TemplateSubject = "Your acount has been compromise - take action immeditly",
                TemplateBody = "Dear valued costumer, we have detect unusual activty on your acount. Your acount will be permanantly close if you do not verify your informations within 24 hours. Click the link below to confirm your identiy and password.",
                TemplateSender = "security@amaz0n-alerts.com",
                PhishingIndicators = "Multiple spelling errors; grammar mistakes; fake Amazon domain; urgency; requests password",
                CreatedBy = adminId
            },

            // Medium
            new()
            {
                CampaignName = "Password Expiry Notice",
                Difficulty = Difficulty.Medium,
                TemplateSubject = "Action Required: Your password expires in 24 hours",
                TemplateBody = "Your company email password will expire tomorrow. To avoid losing access to your mailbox, please update your password by clicking the link below. This is an automated message from IT Support.",
                TemplateSender = "it-support@company-portal.net",
                PhishingIndicators = "Fake IT support; urgency around password; external domain pretending to be internal; generic greeting",
                CreatedBy = adminId
            },
            new()
            {
                CampaignName = "Fake Invoice",
                Difficulty = Difficulty.Medium,
                TemplateSubject = "Invoice #INV-2026-4821 - Payment Due",
                TemplateBody = "Please find attached the invoice for services rendered in February 2026. The total amount due is $3,450.00. Payment is due within 15 business days. If you have questions about this invoice, please reply to this email.",
                TemplateSender = "billing@quickbooks-invoice.com",
                PhishingIndicators = "Fake billing domain; unsolicited invoice; no specific company name; attachment likely malicious",
                CreatedBy = adminId
            },
            new()
            {
                CampaignName = "Shared Document",
                Difficulty = Difficulty.Medium,
                TemplateSubject = "John Smith shared a document with you",
                TemplateBody = "John Smith has shared a document with you via OneDrive. Click below to view 'Q1 Budget Report.xlsx'. You may need to sign in with your Microsoft account to access this file.",
                TemplateSender = "no-reply@onedrive-share.com",
                PhishingIndicators = "Fake OneDrive domain; credential harvesting; impersonates known contact pattern; requests sign-in",
                CreatedBy = adminId
            },

            // Hard
            new()
            {
                CampaignName = "CEO Wire Transfer",
                Difficulty = Difficulty.Hard,
                TemplateSubject = "Quick favor needed - confidential",
                TemplateBody = "Hey, I'm in a meeting and can't talk right now. I need you to process a wire transfer for a vendor payment - it's time-sensitive. Can you handle this? I'll send the details once you confirm. Please keep this between us for now. Thanks.",
                TemplateSender = "ceo.name@company-mail.com",
                PhishingIndicators = "CEO impersonation (BEC); urgency; secrecy request; external domain similar to company; no specific details",
                CreatedBy = adminId
            },
            new()
            {
                CampaignName = "HR Benefits Update",
                Difficulty = Difficulty.Hard,
                TemplateSubject = "Open Enrollment: Updated Health Benefits for 2026",
                TemplateBody = "As part of our annual benefits review, we're pleased to announce updated health insurance options for 2026. To review your current elections and make changes, please log in to the benefits portal using your company credentials. The enrollment window closes March 31st.",
                TemplateSender = "benefits@hr-portal-company.com",
                PhishingIndicators = "Fake HR portal; timely/seasonal topic; credential harvesting; professional tone; external domain",
                CreatedBy = adminId
            },

            // Expert
            new()
            {
                CampaignName = "Vendor Payment Update",
                Difficulty = Difficulty.Expert,
                TemplateSubject = "Updated banking details for upcoming payment",
                TemplateBody = "Hi, I hope you're doing well. I wanted to let you know that we've recently changed banks. Could you please update our payment details for the next invoice? I've attached the new banking information. Please confirm once updated. Best regards.",
                TemplateSender = "accounting@vendor-company.com",
                PhishingIndicators = "Vendor impersonation; banking detail change (common BEC); polite professional tone; no red flags in language; requires knowledge of actual vendor relationships",
                CreatedBy = adminId
            },
            new()
            {
                CampaignName = "Conference Registration",
                Difficulty = Difficulty.Expert,
                TemplateSubject = "Early bird registration: Industry Conference 2026",
                TemplateBody = "You're invited to register for the annual Industry Security Conference, April 15-17 in Austin, TX. Early bird pricing ends this Friday. Your manager has pre-approved attendance. Click below to complete registration with your company profile.",
                TemplateSender = "registration@industry-conf-2026.com",
                PhishingIndicators = "Fake conference; claims manager approval; credential harvesting via registration; timely event; professional formatting; very subtle indicators",
                CreatedBy = adminId
            }
        };

        _db.PhishingCampaigns.AddRange(campaigns);
        await _db.SaveChangesAsync();
    }
}

public class LeaderboardEntry
{
    public int EmployeeId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int ScorePoints { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public Difficulty CurrentDifficulty { get; set; }
    public int TotalSimulations { get; set; }
    public int TotalReported { get; set; }
    public int TotalClicked { get; set; }
    public decimal ReportRate { get; set; }
}
