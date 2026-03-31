namespace PhishGuard.Models.ViewModels;

public class DashboardViewModel
{
    public Employee Employee { get; set; } = null!;
    public List<EmailViewModel> RecentEmails { get; set; } = new();
    public EmployeeTraining? Training { get; set; }
    public DashboardStats Stats { get; set; } = new();
}

public class EmailViewModel
{
    public int EmailId { get; set; }
    public string SenderAddress { get; set; } = string.Empty;
    public string? SenderDisplayName { get; set; }
    public string? Subject { get; set; }
    public string? BodyPreview { get; set; }
    public DateTime ReceivedAt { get; set; }
    public decimal OverallScore { get; set; }
    public EmailClassification Classification { get; set; }
    public bool IsReported { get; set; }
    public List<string> WarningReasons { get; set; } = new();
    public List<EmailScoreViewModel> Scores { get; set; } = new();
}

public class EmailScoreViewModel
{
    public string Dimension { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Details { get; set; }
}

public class DashboardStats
{
    public int TotalEmails { get; set; }
    public int BlockedCount { get; set; }
    public int WarningCount { get; set; }
    public int SafeCount { get; set; }
    public int ReportedCount { get; set; }
    public int TrainingPoints { get; set; }
    public int CurrentStreak { get; set; }
}
