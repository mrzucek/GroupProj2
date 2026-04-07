using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public class AiPhishScoreService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public AiPhishScoreService(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _http = httpFactory.CreateClient("OpenAI");
    }

    public async Task<decimal?> TryAssignOverallScoreAsync(
        Email email,
        IReadOnlyList<EmailScore> dimensionScores,
        decimal heuristicOverallScore,
        CancellationToken ct = default)
    {
        if (!_config.GetValue("AI:Enabled", false))
            return null;

        var apiKey =
            _config["OPENAI_API_KEY"] ??
            _config["AI:OpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var model = _config["AI:OpenAI:Model"] ?? "gpt-4o-mini";

        // Keep prompt small + deterministic-ish. We want a single integer score (0-100).
        var features = dimensionScores
            .Select(s => new { dimension = s.Dimension.ToString(), score = s.Score, weight = s.Weight, details = s.Details })
            .ToList();

        var userPayload = new
        {
            subject = email.Subject ?? "",
            preview = email.BodyPreview ?? "",
            sender = new { address = email.SenderAddress, name = email.SenderDisplayName ?? "" },
            heuristicScore = heuristicOverallScore,
            dimensions = features
        };

        var systemPrompt =
            "You are PhishNET's scoring model. Output ONLY valid JSON (no markdown, no prose). " +
            "Return: {\"score\": <integer 0-100>}. " +
            "Score is phishing risk: 0=very safe, 100=definitely phishing. " +
            "Use the provided heuristicScore and dimension signals; do not invent facts.";

        var req = new
        {
            model,
            temperature = 0.1,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = JsonSerializer.Serialize(userPayload) }
            }
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpReq.Content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // choices[0].message.content should be JSON string because we requested json_object
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            return null;

        using var outDoc = JsonDocument.Parse(content);
        if (!outDoc.RootElement.TryGetProperty("score", out var scoreEl))
            return null;

        if (!scoreEl.TryGetInt32(out var scoreInt))
            return null;

        scoreInt = Math.Clamp(scoreInt, 0, 100);
        return scoreInt;
    }
}

