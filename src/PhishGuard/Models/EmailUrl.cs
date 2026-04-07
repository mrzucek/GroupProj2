using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class EmailUrl
{
    public int UrlId { get; set; }
    public int EmailId { get; set; }

    [Required, MaxLength(2000)]
    public string OriginalUrl { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? FinalUrl { get; set; }

    [MaxLength(255)]
    public string? Domain { get; set; }

    public decimal ThreatScore { get; set; }
    public bool? IsSafe { get; set; }

    [MaxLength(100)]
    public string? SafeBrowsingResult { get; set; }

    public int? DomainAgeDays { get; set; }
    public DateTime? CheckedAt { get; set; }

    // Navigation
    public Email Email { get; set; } = null!;
}
