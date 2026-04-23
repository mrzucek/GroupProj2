using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Services.Scoring;

namespace PhishGuard.Services;

public class OpenAiEmailGeneratorService
{
    private readonly HttpClient _http;
    private readonly PhishGuardContext _db;
    private readonly ScoringEngine _scoring;
    private readonly string? _apiKey;

    public OpenAiEmailGeneratorService(IHttpClientFactory factory, PhishGuardContext db, ScoringEngine scoring, IConfiguration config)
    {
        _http = factory.CreateClient("OpenAI");
        _db = db;
        _scoring = scoring;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? config["OpenAI:ApiKey"];
    }

    public async Task SeedEmailsForUserAsync(int employeeId, string recipientName, string recipientEmail)
    {
        // Remove existing emails
        var existing = _db.Emails.Where(e => e.RecipientId == employeeId);
        _db.Emails.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var generated = await GenerateEmailsAsync(recipientName, recipientEmail, 10);

        var rng = new Random();
        var usedOffsets = new HashSet<int>();

        foreach (var gen in generated)
        {
            int offsetMinutes;
            do { offsetMinutes = rng.Next(5, 2880); }
            while (!usedOffsets.Add(offsetMinutes));

            var email = new Email
            {
                RecipientId = employeeId,
                SenderAddress = gen.SenderAddress,
                SenderDisplayName = gen.SenderDisplayName,
                Subject = gen.Subject,
                BodyPreview = gen.BodyPreview?.Length > 1000
                    ? gen.BodyPreview[..1000]
                    : gen.BodyPreview,
                ReceivedAt = DateTime.UtcNow.AddMinutes(-offsetMinutes)
            };

            _db.Emails.Add(email);
            await _db.SaveChangesAsync();
            await _scoring.AnalyzeEmailAsync(email);
        }
    }

    private async Task<List<GeneratedEmail>> GenerateEmailsAsync(string recipientName, string recipientEmail, int count)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return Fallback(recipientName);

        try
        {
            var prompt = $@"Generate exactly {count} professional corporate emails as a JSON array.
The recipient's name is ""{recipientName}"" and their email is ""{recipientEmail}"".

Rules:
- At least 3 emails MUST have ""isPhishing"": true
- The rest are legitimate business emails (HR updates, IT notices, meeting invites, vendor invoices, newsletters, etc.)
- BOTH legitimate and phishing emails should sometimes address the recipient by name (""{recipientName}"") to make phishing harder to detect
- Phishing emails must look professional and convincing — NOT obviously fake. Use urgency, authority, or trust-building language subtly.
- Phishing emails must embed at least one suspicious URL in the body text (e.g. http://secure-payroll-portal.xyz/login or http://microsoft-account-verify.net/confirm)
- Phishing sender domains must look like lookalikes (e.g. paypa1.com, hr-portal-company.net, microsoft-support.co)
- Legitimate emails use realistic company domains (e.g. hr@acmecorp.com, it-support@techfirm.io, noreply@slack.com)
- Vary subjects, tones, and formats across all emails
- Body text should be realistic and 2-4 sentences
- Do NOT include any explanation — return ONLY a raw JSON array with no markdown

JSON format:
[
  {{
    ""senderAddress"": ""hr@acmecorp.com"",
    ""senderDisplayName"": ""ACME HR Team"",
    ""subject"": ""Q2 Benefits Enrollment Open Now"",
    ""bodyPreview"": ""Dear {recipientName}, your Q2 enrollment window is open until April 30th. Log into the HR portal to select your benefits. Contact hr@acmecorp.com with questions."",
    ""isPhishing"": false
  }}
]";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an email generator for a corporate cybersecurity training platform. Always return only valid JSON." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.9,
                max_tokens = 3000
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "[]";

            // Strip markdown code fences if present
            content = content.Trim();
            if (content.StartsWith("```")) content = content[(content.IndexOf('\n') + 1)..];
            if (content.EndsWith("```")) content = content[..content.LastIndexOf("```")].Trim();

            var emails = JsonSerializer.Deserialize<List<GeneratedEmail>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return emails?.Count > 0 ? emails : Fallback(recipientName);
        }
        catch
        {
            return Fallback(recipientName);
        }
    }

    private static List<GeneratedEmail> Fallback(string recipientName) => new()
    {
        new() { SenderAddress = "security@paypa1-verify.com", SenderDisplayName = "PayPal Security", Subject = "URGENT: Your account has been suspended", BodyPreview = $"Dear {recipientName}, your PayPal account has been locked due to unauthorized access. Click here to verify your identity: http://paypa1-verify.com/login", IsPhishing = true },
        new() { SenderAddress = "it-support@micros0ft-help.com", SenderDisplayName = "Microsoft Support", Subject = "Your password expires today", BodyPreview = $"Hi {recipientName}, your Microsoft 365 password will expire in 2 hours. Update now at http://micros0ft-help.com/reset to avoid losing access.", IsPhishing = true },
        new() { SenderAddress = "ceo@company-secure-docs.net", SenderDisplayName = "CEO Office", Subject = "Quick favor needed", BodyPreview = $"Hi {recipientName}, I'm in a meeting and need you to purchase gift cards for a client. Please send 5x $100 Apple gift card codes to this email immediately.", IsPhishing = true },
        new() { SenderAddress = "hr@company.com", SenderDisplayName = "HR Department", Subject = "Updated PTO Policy - Please Review", BodyPreview = "Hi team, we've updated our PTO policy for this year. Please review the attached document and reach out with any questions." },
        new() { SenderAddress = "notifications@github.com", SenderDisplayName = "GitHub", Subject = "Pull request #47 merged", BodyPreview = "The pull request 'Update scoring engine weights' has been merged into main. 3 files changed, 42 insertions, 12 deletions." },
        new() { SenderAddress = "calendar@google.com", SenderDisplayName = "Google Calendar", Subject = "Reminder: Team standup in 30 minutes", BodyPreview = "This is a reminder for your event 'Team Standup' at 10:00 AM today. Join the meeting at meet.google.com." },
        new() { SenderAddress = "no-reply@slack.com", SenderDisplayName = "Slack", Subject = "New messages in #engineering", BodyPreview = "You have 4 unread messages in #engineering. Mike posted: 'Deploy went smoothly, monitoring dashboards look clean.'" },
        new() { SenderAddress = "sarah.jones@company.com", SenderDisplayName = "Sarah Jones", Subject = "Re: Q2 Budget Review", BodyPreview = $"Hi {recipientName}, thanks for sending that over. I've reviewed the numbers and everything looks good. Let's discuss the marketing allocation tomorrow." },
        new() { SenderAddress = "orders@amazon.com", SenderDisplayName = "Amazon", Subject = "Your order has shipped!", BodyPreview = "Your order #112-9384756 has shipped and is expected to arrive by Thursday. Track your package for the latest delivery updates." },
        new() { SenderAddress = "benefits@company-hr-portal.com", SenderDisplayName = "Employee Benefits", Subject = "Action Required: Update your direct deposit information", BodyPreview = $"Dear {recipientName}, our records show your direct deposit information may be outdated. Please log in to confirm your banking details before the next pay period." }
    };
}

public class GeneratedEmail
{
    public string SenderAddress { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? BodyPreview { get; set; }
    public bool IsPhishing { get; set; }
}
