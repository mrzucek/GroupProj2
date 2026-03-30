using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Services.Scoring;
using System.Security.Claims;

namespace PhishGuard.Controllers;

[Authorize]
public class DemoController : Controller
{
    private readonly PhishGuardContext _db;
    private readonly ScoringEngine _scoring;

    public DemoController(PhishGuardContext db, ScoringEngine scoring)
    {
        _db = db;
        _scoring = scoring;
    }

    [HttpPost]
    public async Task<IActionResult> SeedTestEmails()
    {
        var employeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var testEmails = new[]
        {
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "security@paypa1-verify.com",
                SenderDisplayName = "PayPal Security",
                Subject = "URGENT: Your account has been suspended - verify now",
                BodyPreview = "Your PayPal account has been locked due to unauthorized access. Click here to log in and verify your identity immediately or your account will be permanently closed.",
                ReceivedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "hr@company.com",
                SenderDisplayName = "HR Department",
                Subject = "Updated PTO Policy - Please Review",
                BodyPreview = "Hi team, we've updated our PTO policy for 2026. Please review the attached document and reach out if you have questions.",
                ReceivedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "prize-winner@free-rewards-now.xyz",
                SenderDisplayName = "Rewards Center",
                Subject = "Congratulations! You've won a $500 Amazon Gift Card!",
                BodyPreview = "You've been selected as our lucky winner! Claim your prize now by clicking the link below. Act now - this offer expires today! Verify your account to receive your free gift.",
                ReceivedAt = DateTime.UtcNow.AddMinutes(-30)
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "jsmith@gmail.com",
                SenderDisplayName = "John Smith",
                Subject = "Invoice #4821 - Payment Required",
                BodyPreview = "Please find attached the invoice for last month's consulting services. Payment is due within 30 days. Let me know if you have any questions.",
                ReceivedAt = DateTime.UtcNow.AddHours(-3)
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "it-support@micros0ft-help.com",
                SenderDisplayName = "Microsoft Support",
                Subject = "Your password expires today - update your information",
                BodyPreview = "Your Microsoft 365 password will expire in 2 hours. Click here to log in and update your information immediately to avoid losing access to your account.",
                ReceivedAt = DateTime.UtcNow.AddMinutes(-45)
            }
        };

        foreach (var email in testEmails)
        {
            _db.Emails.Add(email);
            await _db.SaveChangesAsync();
            await _scoring.AnalyzeEmailAsync(email);
        }

        TempData["Success"] = $"Seeded {testEmails.Length} test emails and ran them through the scoring engine.";
        return RedirectToAction("Index", "Dashboard");
    }
}
