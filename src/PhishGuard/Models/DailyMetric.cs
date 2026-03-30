namespace PhishGuard.Models;

public class DailyMetric
{
    public int MetricId { get; set; }
    public DateOnly MetricDate { get; set; }
    public int TotalEmailsScanned { get; set; }
    public int TotalBlocked { get; set; }
    public int TotalWarnings { get; set; }
    public int TotalSafe { get; set; }
    public int TotalEmployeeReports { get; set; }
    public int TotalConfirmedPhishing { get; set; }
    public int TotalUrlsChecked { get; set; }
    public int TotalUrlsBlocked { get; set; }
    public int TotalSimulationsSent { get; set; }
    public int TotalSimulationsCaught { get; set; }
    public decimal? CompanySecurityScore { get; set; }
}
