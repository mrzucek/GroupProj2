using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public interface IScoringDimension
{
    ScoringDimension Dimension { get; }
    Task<EmailScore> AnalyzeAsync(Email email);
}
