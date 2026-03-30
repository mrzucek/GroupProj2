using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public class ScoringEngine
{
    private readonly PhishGuardContext _db;
    private readonly IEnumerable<IScoringDimension> _scorers;

    // Thresholds for classification
    private const decimal WarningThreshold = 30;
    private const decimal BlockedThreshold = 70;

    public ScoringEngine(PhishGuardContext db, IEnumerable<IScoringDimension> scorers)
    {
        _db = db;
        _scorers = scorers;
    }

    public async Task<EmailAnalysisResult> AnalyzeEmailAsync(Email email)
    {
        var scores = new List<EmailScore>();

        // Run all scoring dimensions
        foreach (var scorer in _scorers)
        {
            var score = await scorer.AnalyzeAsync(email);
            scores.Add(score);
        }

        // Calculate weighted average
        decimal totalWeightedScore = 0;
        decimal totalWeight = 0;

        foreach (var score in scores)
        {
            totalWeightedScore += score.Score * score.Weight;
            totalWeight += score.Weight;
        }

        var overallScore = totalWeight > 0 ? totalWeightedScore / totalWeight : 0;

        // Classify based on thresholds
        var classification = overallScore switch
        {
            >= BlockedThreshold => EmailClassification.Blocked,
            >= WarningThreshold => EmailClassification.Warning,
            _ => EmailClassification.Safe
        };

        // Update email record
        email.OverallScore = Math.Round(overallScore, 2);
        email.Classification = classification;
        email.ProcessedAt = DateTime.UtcNow;

        // Save scores to database
        foreach (var score in scores)
        {
            score.EmailId = email.EmailId;
            _db.EmailScores.Add(score);
        }

        await _db.SaveChangesAsync();

        // Build warning reasons for context-aware display
        var warningReasons = scores
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score * s.Weight)
            .Select(s => s.Details ?? s.Dimension.ToString())
            .ToList();

        return new EmailAnalysisResult
        {
            Email = email,
            Scores = scores,
            OverallScore = email.OverallScore,
            Classification = classification,
            WarningReasons = warningReasons
        };
    }
}

public class EmailAnalysisResult
{
    public Email Email { get; set; } = null!;
    public List<EmailScore> Scores { get; set; } = new();
    public decimal OverallScore { get; set; }
    public EmailClassification Classification { get; set; }
    public List<string> WarningReasons { get; set; } = new();
}
