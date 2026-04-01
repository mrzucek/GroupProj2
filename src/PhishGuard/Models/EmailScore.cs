using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class EmailScore
{
    public int ScoreId { get; set; }
    public int EmailId { get; set; }

    [Required]
    public ScoringDimension Dimension { get; set; }

    public decimal Score { get; set; }
    public decimal Weight { get; set; } = 1.00m;

    [MaxLength(500)]
    public string? Details { get; set; }

    // Navigation
    public Email Email { get; set; } = null!;
}

public enum ScoringDimension
{
    ThreatFeed,
    DomainReputation,
    LanguageAnalysis,
    BehavioralTiming,
    HtmlFingerprint,
    SenderHistory,
    LinkAnalysis
}
