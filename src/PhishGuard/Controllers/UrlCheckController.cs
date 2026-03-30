using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhishGuard.Models.ViewModels;
using PhishGuard.Services;

namespace PhishGuard.Controllers;

[Authorize]
public class UrlCheckController : Controller
{
    private readonly UrlSafetyService _urlService;

    public UrlCheckController(UrlSafetyService urlService)
    {
        _urlService = urlService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Check(string url, int? emailId)
    {
        if (string.IsNullOrWhiteSpace(url))
            return RedirectToAction("Index");

        // Ensure URL has a scheme
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        var result = await _urlService.CheckUrlAsync(url);

        var model = new UrlCheckViewModel
        {
            Url = result.OriginalUrl,
            Domain = result.Domain,
            FinalUrl = result.FinalUrl,
            IsSafe = result.IsSafe,
            ThreatScore = result.ThreatScore,
            TriggersDownload = result.TriggersDownload,
            Reasons = result.Reasons,
            EmailId = emailId
        };

        return View("Result", model);
    }

    [HttpPost]
    public async Task<IActionResult> CheckPost(string url, int? emailId)
    {
        return await Check(url, emailId);
    }
}
