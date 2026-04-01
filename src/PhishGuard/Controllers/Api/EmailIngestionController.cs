using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;
using PhishGuard.Services;
using PhishGuard.Services.Scoring;

namespace PhishGuard.Controllers.Api;

[ApiController]
[Route("api/emails")]
public class EmailIngestionController : ControllerBase
{
    private readonly PhishGuardContext _db;
    private readonly ScoringEngine _scoring;
    private readonly UrlSafetyService _urlSafety;

    public EmailIngestionController(PhishGuardContext db, ScoringEngine scoring, UrlSafetyService urlSafety)
    {
        _db = db;
        _scoring = scoring;
        _urlSafety = urlSafety;
    }

    [HttpPost]
    public async Task<IActionResult> IngestEmail([FromBody] IngestEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SenderAddress))
            return BadRequest(new { error = "SenderAddress is required." });

        int recipientId;
        if (!string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            var recipient = await _db.Employees.FirstOrDefaultAsync(e => e.Email == request.RecipientEmail);
            recipientId = recipient?.EmployeeId
                ?? (await _db.Employees.FirstAsync(e => e.Role == EmployeeRole.Admin)).EmployeeId;
        }
        else
        {
            recipientId = (await _db.Employees.FirstAsync(e => e.Role == EmployeeRole.Admin)).EmployeeId;
        }

        var email = new Email
        {
            RecipientId = recipientId,
            SenderAddress = request.SenderAddress,
            SenderDisplayName = request.SenderDisplayName,
            Subject = request.Subject,
            BodyPreview = request.Body?.Length > 1000 ? request.Body[..1000] : request.Body,
            ReceivedAt = request.ReceivedAt ?? DateTime.UtcNow
        };

        _db.Emails.Add(email);
        await _db.SaveChangesAsync();

        // Extract URLs
        var urls = LinkAnalysisService.ExtractUrls(request.Body)
            .Concat(LinkAnalysisService.ExtractUrls(request.Subject))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var url in urls)
        {
            try
            {
                var uri = new Uri(url);
                _db.EmailUrls.Add(new EmailUrl { EmailId = email.EmailId, OriginalUrl = url, Domain = uri.Host });
            }
            catch
            {
                _db.EmailUrls.Add(new EmailUrl { EmailId = email.EmailId, OriginalUrl = url });
            }
        }
        await _db.SaveChangesAsync();

        // Check URLs BEFORE scoring
        var emailUrls = await _db.EmailUrls.Where(u => u.EmailId == email.EmailId).ToListAsync();
        foreach (var emailUrl in emailUrls)
        {
            try
            {
                var checkResult = await _urlSafety.CheckUrlAsync(emailUrl.OriginalUrl);
                emailUrl.IsSafe = checkResult.IsSafe;
                emailUrl.ThreatScore = checkResult.ThreatScore;
                emailUrl.FinalUrl = checkResult.FinalUrl;
                emailUrl.SafeBrowsingResult = string.Join("; ", checkResult.Reasons);
                emailUrl.CheckedAt = DateTime.UtcNow;
            }
            catch { emailUrl.CheckedAt = DateTime.UtcNow; }
        }
        await _db.SaveChangesAsync();

        var result = await _scoring.AnalyzeEmailAsync(email);

        return Ok(new
        {
            emailId = email.EmailId,
            overallScore = result.OverallScore,
            classification = result.Classification.ToString(),
            warningReasons = result.WarningReasons,
            urlsFound = urls.Count
        });
    }
}

public class IngestEmailRequest
{
    public string SenderAddress { get; set; } = string.Empty;
    public string? SenderDisplayName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public DateTime? ReceivedAt { get; set; }
}
