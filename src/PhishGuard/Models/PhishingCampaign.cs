using System.ComponentModel.DataAnnotations;

namespace PhishGuard.Models;

public class PhishingCampaign
{
    public int CampaignId { get; set; }

    [Required, MaxLength(200)]
    public string CampaignName { get; set; } = string.Empty;

    [Required]
    public Difficulty Difficulty { get; set; }

    [Required, MaxLength(500)]
    public string TemplateSubject { get; set; } = string.Empty;

    [Required]
    public string TemplateBody { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string TemplateSender { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string PhishingIndicators { get; set; } = string.Empty;

    public int CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Employee Creator { get; set; } = null!;
    public ICollection<SimulationEmail> Simulations { get; set; } = new List<SimulationEmail>();
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}
