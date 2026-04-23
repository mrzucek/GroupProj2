namespace PhishGuard.Models;

public class SimulationEmail
{
    public int SimulationId { get; set; }
    public int CampaignId { get; set; }
    public int TargetEmployeeId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedLinkAt { get; set; }
    public DateTime? ReportedAt { get; set; }
    public SimulationResult Result { get; set; } = SimulationResult.Pending;
    public bool RequiresLoginQuiz { get; set; } = false;

    // Navigation
    public PhishingCampaign Campaign { get; set; } = null!;
    public Employee TargetEmployee { get; set; } = null!;
}

public enum SimulationResult
{
    Pending,
    NoAction,
    Clicked,
    Reported
}
