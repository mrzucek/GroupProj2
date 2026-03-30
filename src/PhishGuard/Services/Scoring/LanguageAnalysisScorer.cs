using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public class LanguageAnalysisScorer : IScoringDimension
{
    private readonly PhishGuardContext _db;

    public LanguageAnalysisScorer(PhishGuardContext db)
    {
        _db = db;
    }

    public ScoringDimension Dimension => ScoringDimension.LanguageAnalysis;

    public async Task<EmailScore> AnalyzeAsync(Email email)
    {
        var textToAnalyze = $"{email.Subject} {email.BodyPreview}".ToLowerInvariant();
        decimal totalScore = 0;
        var details = new List<string>();

        // Load active language analysis rules with patterns
        var rules = await _db.ScoringRules
            .Where(r => r.IsActive && r.Dimension == ScoringDimension.LanguageAnalysis && r.Pattern != null)
            .ToListAsync();

        foreach (var rule in rules)
        {
            if (string.IsNullOrEmpty(rule.Pattern)) continue;

            try
            {
                if (Regex.IsMatch(textToAnalyze, rule.Pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
                {
                    totalScore += rule.ScoreValue;
                    details.Add(rule.RuleName);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Skip patterns that take too long — don't let a bad regex hang the system
            }
        }

        // Cap at 100
        totalScore = Math.Min(totalScore, 100);

        return new EmailScore
        {
            EmailId = email.EmailId,
            Dimension = Dimension,
            Score = totalScore,
            Weight = 0.8m,
            Details = details.Count > 0 ? $"Matched: {string.Join(", ", details)}" : "No suspicious language detected"
        };
    }
}
