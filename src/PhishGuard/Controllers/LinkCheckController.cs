using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhishGuard.Services;

namespace PhishGuard.Controllers;

[Authorize]
[Route("api/links")]
public class LinkCheckController : Controller
{
    private readonly LinkAnalysisService _linkAnalysis;

    public LinkCheckController(LinkAnalysisService linkAnalysis) => _linkAnalysis = linkAnalysis;

    [HttpGet("check")]
    public async Task<IActionResult> Check([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "URL is required." });

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        var result = await _linkAnalysis.AnalyzeLinkAsync(url);

        return Json(new
        {
            url = result.Url,
            rating = result.Rating.ToString().ToLower(),
            combinedScore = result.CombinedScore,
            heuristicScore = result.HeuristicScore,
            aiScore = result.AiScore,
            aiSummary = result.AiSummary,
            reasons = result.Reasons
        });
    }
}
