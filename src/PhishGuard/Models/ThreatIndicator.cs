using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class ThreatIndicator
{
    public int IndicatorId { get; set; }

    [Required]
    public IndicatorType Type { get; set; }

    [Required, MaxLength(500)]
    public string Value { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Source { get; set; } = string.Empty;

    public ThreatSeverity Severity { get; set; } = ThreatSeverity.Medium;
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public enum IndicatorType
{
    Domain,
    Url,
    SenderEmail,
    IpAddress,
    KeywordPattern
}

public enum ThreatSeverity
{
    Low,
    Medium,
    High,
    Critical
}
