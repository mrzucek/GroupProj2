using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class TrustedDomain
{
    public int TrustedDomainId { get; set; }

    [Required, MaxLength(255)]
    public string Domain { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatedById { get; set; }
    public Employee CreatedBy { get; set; } = null!;
}
