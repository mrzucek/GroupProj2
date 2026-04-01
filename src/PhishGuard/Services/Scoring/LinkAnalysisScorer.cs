using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public class LinkAnalysisScorer : IScoringDimension
{
    private readonly PhishGuardContext _db;

    public LinkAnalysisScorer(PhishGuardContext db) => _db = db;

    public ScoringDimension Dimension => ScoringDimension.LinkAnalysis;

    public async Task<EmailScore> AnalyzeAsync(Email email)
    {
        var urls = await _db.EmailUrls
            .Where(u => u.EmailId == email.EmailId && u.CheckedAt != null)
            .ToListAsync();

        if (!urls.Any())
        {
            return new EmailScore
            {
                EmailId = email.EmailId,
                Dimension = Dimension,
                Score = 0,
                Weight = 1.0m,
                Details = "No URLs found in email"
            };
        }

        var maxThreatScore = urls.Max(u => u.ThreatScore);
        var dangerousUrls = urls.Where(u => u.IsSafe == false).ToList();

        var details = dangerousUrls.Any()
            ? $"Dangerous links detected: {string.Join(", ", dangerousUrls.Select(u => u.Domain ?? u.OriginalUrl))}"
            : "All links appear safe";

        return new EmailScore
        {
            EmailId = email.EmailId,
            Dimension = Dimension,
            Score = maxThreatScore,
            Weight = 1.0m,
            Details = details
        };
    }
}
