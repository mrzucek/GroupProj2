using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class PhishingReport
{
    public int ReportId { get; set; }
    public int EmailId { get; set; }
    public int ReportedBy { get; set; }

    [MaxLength(500)]
    public string? ReportReason { get; set; }

    public bool? IsConfirmedPhishing { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Email Email { get; set; } = null!;
    public Employee Reporter { get; set; } = null!;
    public Employee? Reviewer { get; set; }
}
