namespace PhishGuard.Models.ViewModels;

public class QuizGateViewModel
{
    public int SimulationId { get; set; }
    public string PhishingSlot { get; set; } = "left"; // "left" or "right"

    // The phishing email (from campaign)
    public string PhishSenderDisplay { get; set; } = string.Empty;
    public string PhishSenderAddress { get; set; } = string.Empty;
    public string PhishSubject { get; set; } = string.Empty;
    public string PhishBodyPreview { get; set; } = string.Empty;

    // The legitimate email (from inbox)
    public string LegitSenderDisplay { get; set; } = string.Empty;
    public string LegitSenderAddress { get; set; } = string.Empty;
    public string LegitSubject { get; set; } = string.Empty;
    public string LegitBodyPreview { get; set; } = string.Empty;
}

public class QuizResultViewModel
{
    public bool IsCorrect { get; set; }
    public string PhishSenderDisplay { get; set; } = string.Empty;
    public string PhishSenderAddress { get; set; } = string.Empty;
    public string PhishSubject { get; set; } = string.Empty;
    public string PhishingIndicators { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
    public bool StreakReset { get; set; }
}
