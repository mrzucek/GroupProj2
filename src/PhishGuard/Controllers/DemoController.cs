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
        var rng = new Random();

        // Clear existing emails for this user so we get a fresh randomized set
        var existingEmails = _db.Emails.Where(e => e.RecipientId == employeeId);
        _db.Emails.RemoveRange(existingEmails);
        await _db.SaveChangesAsync();

        var testEmails = new List<Email>
        {
            // --- Phishing / Scam ---
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "security@paypa1-verify.com",
                SenderDisplayName = "PayPal Security",
                Subject = "URGENT: Your account has been suspended - verify now",
                BodyPreview = "Your PayPal account has been locked due to unauthorized access. Click here to log in and verify your identity immediately or your account will be permanently closed."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "prize-winner@free-rewards-now.xyz",
                SenderDisplayName = "Rewards Center",
                Subject = "Congratulations! You've won a $500 Amazon Gift Card!",
                BodyPreview = "You've been selected as our lucky winner! Claim your prize now by clicking the link below. Act now - this offer expires today! Verify your account to receive your free gift."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "it-support@micros0ft-help.com",
                SenderDisplayName = "Microsoft Support",
                Subject = "Your password expires today - update your information",
                BodyPreview = "Your Microsoft 365 password will expire in 2 hours. Click here to log in and update your information immediately to avoid losing access to your account."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "ceo@company-secure-docs.net",
                SenderDisplayName = "David Chen (CEO)",
                Subject = "Quick favor - need you to handle something",
                BodyPreview = "Hey, I'm in a meeting and can't talk. I need you to purchase some gift cards for a client appreciation event. Can you get 5x $100 Apple gift cards and send me the codes? I'll reimburse you."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "noreply@acc0unt-verify.com",
                SenderDisplayName = "Apple ID Support",
                Subject = "Your Apple ID has been locked for security reasons",
                BodyPreview = "We detected unusual sign-in activity on your Apple account. Your account has been temporarily locked. Please verify your identity within 24 hours or your account will be permanently disabled."
            },

            // --- Legitimate ---
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "hr@company.com",
                SenderDisplayName = "HR Department",
                Subject = "Updated PTO Policy - Please Review",
                BodyPreview = "Hi team, we've updated our PTO policy for 2026. Please review the attached document and reach out if you have questions."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "jsmith@gmail.com",
                SenderDisplayName = "John Smith",
                Subject = "Invoice #4821 - Payment Required",
                BodyPreview = "Please find attached the invoice for last month's consulting services. Payment is due within 30 days. Let me know if you have any questions."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "notifications@github.com",
                SenderDisplayName = "GitHub",
                Subject = "[PhishGuard] Pull request #47 merged",
                BodyPreview = "The pull request 'Update scoring engine weights' by @mgarcia has been merged into main. 3 files changed, 42 insertions, 12 deletions."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "calendar@google.com",
                SenderDisplayName = "Google Calendar",
                Subject = "Reminder: Team standup in 30 minutes",
                BodyPreview = "This is a reminder for your event 'Team Standup' at 10:00 AM today. Join the meeting at meet.google.com/abc-defg-hij."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "sarah.jones@company.com",
                SenderDisplayName = "Sarah Jones",
                Subject = "Re: Q2 Budget Review",
                BodyPreview = "Thanks for sending that over. I've reviewed the numbers and everything looks good. Let's discuss the marketing allocation in our 1:1 tomorrow."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "no-reply@slack.com",
                SenderDisplayName = "Slack",
                Subject = "New messages in #engineering",
                BodyPreview = "You have 4 unread messages in #engineering. Mike posted: 'Deploy went smoothly, monitoring dashboards look clean.'"
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "orders@amazon.com",
                SenderDisplayName = "Amazon",
                Subject = "Your order has shipped!",
                BodyPreview = "Your order #112-9384756-2938471 has shipped and is expected to arrive by Thursday, April 3. Track your package for the latest delivery updates."
            },

            // --- Borderline / Warning ---
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "admin@dropbox-share.info",
                SenderDisplayName = "Dropbox",
                Subject = "Someone shared a file with you",
                BodyPreview = "A document titled 'Confidential_Report_2026.pdf' has been shared with you. Click here to view the document in your Dropbox account."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "benefits@company-hr-portal.com",
                SenderDisplayName = "Employee Benefits",
                Subject = "Action Required: Update your direct deposit information",
                BodyPreview = "Our records show your direct deposit information may be outdated. Please log in to the employee portal and confirm your banking details before the next pay period."
            },
            new Email
            {
                RecipientId = employeeId,
                SenderAddress = "shipping@fedex-tracking.co",
                SenderDisplayName = "FedEx Delivery",
                Subject = "Delivery attempt failed - action needed",
                BodyPreview = "We attempted to deliver your package but no one was available. Please reschedule your delivery or pick up your package at the nearest FedEx location by clicking below."
            }
        };

        // Shuffle the list so they appear in random order
        for (int i = testEmails.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (testEmails[i], testEmails[j]) = (testEmails[j], testEmails[i]);
        }

        // Generate unique random times spread across last 48 hours
        var now = DateTime.UtcNow;
        var usedMinutes = new HashSet<int>();
        foreach (var email in testEmails)
        {
            int offset;
            do { offset = rng.Next(5, 2880); }
            while (!usedMinutes.Add(offset));
            email.ReceivedAt = now.AddMinutes(-offset);
        }

        foreach (var email in testEmails)
        {
            _db.Emails.Add(email);
            await _db.SaveChangesAsync();
            await _scoring.AnalyzeEmailAsync(email);
        }

        TempData["Success"] = $"Seeded {testEmails.Count} test emails and ran them through the scoring engine.";
        return RedirectToAction("Index", "Dashboard");
    }
}
