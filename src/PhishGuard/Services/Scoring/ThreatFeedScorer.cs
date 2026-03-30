using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public class ThreatFeedScorer : IScoringDimension
{
    private readonly PhishGuardContext _db;

    public ThreatFeedScorer(PhishGuardContext db)
    {
        _db = db;
    }

    public ScoringDimension Dimension => ScoringDimension.ThreatFeed;

    public async Task<EmailScore> AnalyzeAsync(Email email)
    {
        var senderDomain = email.SenderAddress.Split('@').LastOrDefault()?.ToLowerInvariant() ?? "";
        decimal score = 0;
        var details = new List<string>();

        // Check sender email against threat indicators
        var senderMatch = await _db.ThreatIndicators
            .Where(t => t.IsActive && t.Type == IndicatorType.SenderEmail && t.Value == email.SenderAddress.ToLowerInvariant())
            .FirstOrDefaultAsync();

        if (senderMatch != null)
        {
            score = Math.Max(score, 85);
            details.Add($"Sender matched threat feed ({senderMatch.Source})");
        }

        // Check sender domain against threat indicators
        var domainMatch = await _db.ThreatIndicators
            .Where(t => t.IsActive && t.Type == IndicatorType.Domain && t.Value == senderDomain)
            .FirstOrDefaultAsync();

        if (domainMatch != null)
        {
            score = Math.Max(score, 90);
            details.Add($"Domain matched threat feed ({domainMatch.Source})");
        }

        return new EmailScore
        {
            EmailId = email.EmailId,
            Dimension = Dimension,
            Score = score,
            Weight = 1.0m,
            Details = details.Count > 0 ? string.Join("; ", details) : "No threat feed matches"
        };
    }
}
