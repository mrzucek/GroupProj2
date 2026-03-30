namespace PhishGuard.Models;

public class EmployeeTraining
{
    public int TrainingId { get; set; }
    public int EmployeeId { get; set; }
    public Difficulty CurrentDifficulty { get; set; } = Difficulty.Easy;
    public int TotalSimulationsReceived { get; set; }
    public int TotalCorrectlyReported { get; set; }
    public int TotalClicked { get; set; }
    public int TotalIgnored { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public int ScorePoints { get; set; }
    public DateTime? LastSimulationAt { get; set; }
    public DateTime? DifficultyUpdatedAt { get; set; }

    // Navigation
    public Employee Employee { get; set; } = null!;
}
