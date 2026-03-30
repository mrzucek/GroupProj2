namespace PhishGuard.Models.ViewModels;

public class UrlCheckViewModel
{
    public string Url { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? FinalUrl { get; set; }
    public bool IsSafe { get; set; }
    public decimal ThreatScore { get; set; }
    public bool TriggersDownload { get; set; }
    public List<string> Reasons { get; set; } = new();
    public int? EmailId { get; set; }
}
