using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services.Training;

public class PhishingGeneratorService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly PhishGuardContext _db;
    private readonly ILogger<PhishingGeneratorService> _logger;

    public PhishingGeneratorService(
        IHttpClientFactory httpFactory, IConfiguration config,
        PhishGuardContext db, ILogger<PhishingGeneratorService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _db = db;
        _logger = logger;
    }

    public async Task<PhishingCampaign?> GeneratePhishingEmailAsync(
        Difficulty difficulty, string employeeName, string? department, int createdBy)
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? _config["Claude:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return null;

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.Timeout = TimeSpan.FromSeconds(20);

            var difficultyGuide = difficulty switch
            {
                Difficulty.Easy => "Create a phishing email with noticeable red flags: misspelled sender domain, urgency language, link to a non-matching domain.",
                Difficulty.Medium => "Create a professional phishing email with subtle domain spoofing (e.g., microsoft-365.cloud). Requires careful sender/link inspection.",
                Difficulty.Hard => "Create a highly convincing phishing email mimicking real corporate communication. Only domain and link URL give it away.",
                Difficulty.Expert => "Create a phishing email nearly indistinguishable from legitimate internal email. Use casual colleague tone, disguised subdomain links.",
                _ => "Create a medium-difficulty phishing email."
            };

            var prompt = $$"""
                Generate a simulated phishing email for corporate security training.

                Target: {{employeeName}}, Department: {{department ?? "General"}}, Difficulty: {{difficulty}}

                {{difficultyGuide}}

                Include a phishing URL in the body. Respond in this exact JSON format only:
                {
                  "campaign_name": "<2-4 word name>",
                  "subject": "<subject line>",
                  "body": "<email body with \n for newlines, include phishing URL>",
                  "sender": "<fake sender email>",
                  "phishing_indicators": "<semicolon-separated red flags>"
                }
                """;

            var requestBody = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 600,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(responseBody);
            var textContent = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            if (string.IsNullOrEmpty(textContent)) return null;

            var jsonMatch = Regex.Match(textContent, @"\{[\s\S]*\}");
            if (!jsonMatch.Success) return null;

            using var aiDoc = JsonDocument.Parse(jsonMatch.Value);
            var root = aiDoc.RootElement;

            var campaign = new PhishingCampaign
            {
                CampaignName = root.GetProperty("campaign_name").GetString() ?? "AI Generated",
                Difficulty = difficulty,
                TemplateSubject = root.GetProperty("subject").GetString() ?? "Test Email",
                TemplateBody = (root.GetProperty("body").GetString() ?? "").Replace("\\n", "\n"),
                TemplateSender = root.GetProperty("sender").GetString() ?? "test@example.com",
                PhishingIndicators = root.GetProperty("phishing_indicators").GetString() ?? "",
                CreatedBy = createdBy,
                IsGenerated = true
            };

            _db.PhishingCampaigns.Add(campaign);
            await _db.SaveChangesAsync();
            return campaign;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate phishing email");
            return null;
        }
    }
}
