using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services;

public class LinkAnalysisService
{
    private readonly UrlSafetyService _urlSafety;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LinkAnalysisService> _logger;

    public LinkAnalysisService(
        UrlSafetyService urlSafety,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<LinkAnalysisService> logger)
    {
        _urlSafety = urlSafety;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<LinkAnalysisResult> AnalyzeLinkAsync(string url)
    {
        var heuristicResult = await _urlSafety.CheckUrlAsync(url);
        var aiAnalysis = await GetClaudeAnalysisAsync(url, heuristicResult);

        var heuristicScore = heuristicResult.ThreatScore;
        var aiScore = aiAnalysis.ThreatScore;
        var combinedScore = (heuristicScore * 0.4m) + (aiScore * 0.6m);

        var rating = combinedScore switch
        {
            >= 60 => LinkRating.Danger,
            >= 25 => LinkRating.Caution,
            _ => LinkRating.Safe
        };

        return new LinkAnalysisResult
        {
            Url = url,
            Rating = rating,
            CombinedScore = Math.Round(combinedScore, 1),
            HeuristicScore = heuristicResult.ThreatScore,
            AiScore = aiAnalysis.ThreatScore,
            Reasons = heuristicResult.Reasons.Concat(aiAnalysis.Reasons).ToList(),
            AiSummary = aiAnalysis.Summary
        };
    }

    public static List<string> ExtractUrls(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var pattern = @"https?://[^\s<>""'\)\]]+";
        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
        return matches
            .Select(m => m.Value.TrimEnd('.', ',', ';', ':', '!', '?'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<AiAnalysisResult> GetClaudeAnalysisAsync(string url, UrlCheckResult heuristicResult)
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? _config["Claude:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return new AiAnalysisResult
            {
                ThreatScore = heuristicResult.ThreatScore,
                Summary = "AI analysis unavailable.",
                Reasons = new List<string>()
            };
        }

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.Timeout = TimeSpan.FromSeconds(15);

            var prompt = $$"""
                You are a cybersecurity URL threat analyst. Analyze this URL for phishing/malware risk.

                URL: {{url}}
                Domain: {{heuristicResult.Domain}}
                Final URL (after redirects): {{heuristicResult.FinalUrl ?? "N/A"}}
                Heuristic findings: {{string.Join("; ", heuristicResult.Reasons)}}

                Respond in this exact JSON format only:
                {
                  "threat_score": <0-100 integer>,
                  "summary": "<one sentence summary of the risk>",
                  "reasons": ["<reason1>", "<reason2>"]
                }

                Only output the JSON, nothing else.
                """;

            var requestBody = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 200,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return FallbackResult(heuristicResult);

            using var doc = JsonDocument.Parse(responseBody);
            var textContent = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();

            if (string.IsNullOrEmpty(textContent))
                return FallbackResult(heuristicResult);

            var jsonMatch = Regex.Match(textContent, @"\{[\s\S]*\}");
            if (!jsonMatch.Success)
                return FallbackResult(heuristicResult);

            using var aiDoc = JsonDocument.Parse(jsonMatch.Value);
            var root = aiDoc.RootElement;

            return new AiAnalysisResult
            {
                ThreatScore = root.GetProperty("threat_score").GetDecimal(),
                Summary = root.GetProperty("summary").GetString() ?? "No summary.",
                Reasons = root.GetProperty("reasons").EnumerateArray()
                    .Select(r => r.GetString() ?? "").Where(r => !string.IsNullOrEmpty(r)).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude API call failed for URL {Url}", url);
            return FallbackResult(heuristicResult);
        }
    }

    private static AiAnalysisResult FallbackResult(UrlCheckResult heuristic) => new()
    {
        ThreatScore = heuristic.ThreatScore,
        Summary = "AI analysis unavailable.",
        Reasons = new List<string>()
    };
}

public enum LinkRating { Safe, Caution, Danger }

public class LinkAnalysisResult
{
    public string Url { get; set; } = string.Empty;
    public LinkRating Rating { get; set; }
    public decimal CombinedScore { get; set; }
    public decimal HeuristicScore { get; set; }
    public decimal AiScore { get; set; }
    public string? AiSummary { get; set; }
    public List<string> Reasons { get; set; } = new();
}

public class AiAnalysisResult
{
    public decimal ThreatScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = new();
}
