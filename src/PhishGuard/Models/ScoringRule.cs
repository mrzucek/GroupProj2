using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class ScoringRule
{
    public int RuleId { get; set; }

    [Required]
    public ScoringDimension Dimension { get; set; }

    [Required, MaxLength(100)]
    public string RuleName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? RuleDescription { get; set; }

    [MaxLength(500)]
    public string? Pattern { get; set; }

    public decimal ScoreValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedBy { get; set; }

    // Navigation
    public Employee? UpdatedByEmployee { get; set; }
}
