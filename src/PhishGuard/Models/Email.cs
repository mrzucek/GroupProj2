using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class Email
{
    public int EmailId { get; set; }

    public int RecipientId { get; set; }

    [Required, MaxLength(255)]
    public string SenderAddress { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? SenderDisplayName { get; set; }

    [MaxLength(500)]
    public string? Subject { get; set; }

    [MaxLength(1000)]
    public string? BodyPreview { get; set; }

    public DateTime ReceivedAt { get; set; }
    public decimal OverallScore { get; set; }
    public EmailClassification Classification { get; set; } = EmailClassification.Safe;
    public bool IsReported { get; set; }
    public DateTime? ReportedAt { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Employee Recipient { get; set; } = null!;
    public ICollection<EmailScore> Scores { get; set; } = new List<EmailScore>();
    public ICollection<EmailUrl> Urls { get; set; } = new List<EmailUrl>();
    public ICollection<PhishingReport> Reports { get; set; } = new List<PhishingReport>();
}

public enum EmailClassification
{
    Safe,
    Warning,
    Blocked
}
