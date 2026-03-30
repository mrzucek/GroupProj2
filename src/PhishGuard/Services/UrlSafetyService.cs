using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services;

public class UrlSafetyService
{
    private readonly PhishGuardContext _db;
    private readonly HttpClient _http;

    public UrlSafetyService(PhishGuardContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _http = httpFactory.CreateClient("SafeBrowsing");
    }

    public async Task<UrlCheckResult> CheckUrlAsync(string url)
    {
        var result = new UrlCheckResult { OriginalUrl = url };

        try
        {
            var uri = new Uri(url);
            result.Domain = uri.Host;
        }
        catch
        {
            result.IsSafe = false;
            result.Reasons.Add("Malformed URL — could not parse");
            return result;
        }

        // 1. Check against our threat indicator database
        await CheckThreatDatabase(result);

        // 2. Check domain reputation (age, lookalike, etc.)
        CheckDomainReputation(result);

        // 3. Follow redirects to find final destination
        await CheckRedirects(result);

        // 4. Check final URL against threat database too
        if (result.FinalUrl != null && result.FinalUrl != result.OriginalUrl)
        {
            await CheckThreatDatabase(result, result.FinalUrl);
        }

        // Determine overall safety
        result.IsSafe = result.ThreatScore < 50;

        return result;
    }

    private async Task CheckThreatDatabase(UrlCheckResult result, string? urlOverride = null)
    {
        var urlToCheck = urlOverride ?? result.OriginalUrl;
        var domain = result.Domain ?? "";

        // Check URL match
        var urlMatch = await _db.ThreatIndicators
            .Where(t => t.IsActive && t.Type == IndicatorType.Url && t.Value == urlToCheck.ToLowerInvariant())
            .FirstOrDefaultAsync();

        if (urlMatch != null)
        {
            result.ThreatScore = Math.Max(result.ThreatScore, 95);
            result.Reasons.Add($"URL found in threat database ({urlMatch.Source})");
        }

        // Check domain match
        var domainMatch = await _db.ThreatIndicators
            .Where(t => t.IsActive && t.Type == IndicatorType.Domain && t.Value == domain.ToLowerInvariant())
            .FirstOrDefaultAsync();

        if (domainMatch != null)
        {
            result.ThreatScore = Math.Max(result.ThreatScore, 90);
            result.Reasons.Add($"Domain found in threat database ({domainMatch.Source})");
        }
    }

    private void CheckDomainReputation(UrlCheckResult result)
    {
        var domain = result.Domain ?? "";

        // Lookalike detection for common brands
        var brandKeywords = new Dictionary<string, string>
        {
            { "paypal", "paypal.com" }, { "microsoft", "microsoft.com" },
            { "apple", "apple.com" }, { "amazon", "amazon.com" },
            { "google", "google.com" }, { "netflix", "netflix.com" },
            { "facebook", "facebook.com" }, { "instagram", "instagram.com" },
            { "linkedin", "linkedin.com" }, { "dropbox", "dropbox.com" }
        };

        foreach (var (keyword, legitimate) in brandKeywords)
        {
            if (domain.Contains(keyword) && domain != legitimate && domain != $"www.{legitimate}")
            {
                result.ThreatScore = Math.Max(result.ThreatScore, 70);
                result.Reasons.Add($"Lookalike domain: contains '{keyword}' but is not {legitimate}");
            }
        }

        // Suspicious TLDs
        var suspiciousTlds = new[] { ".xyz", ".tk", ".ml", ".ga", ".cf", ".gq", ".top", ".buzz", ".info" };
        if (suspiciousTlds.Any(tld => domain.EndsWith(tld)))
        {
            result.ThreatScore = Math.Max(result.ThreatScore, 30);
            result.Reasons.Add("Domain uses a TLD commonly associated with phishing");
        }

        // IP address as domain
        if (System.Text.RegularExpressions.Regex.IsMatch(domain, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
        {
            result.ThreatScore = Math.Max(result.ThreatScore, 60);
            result.Reasons.Add("URL uses an IP address instead of a domain name");
        }

        // Excessive subdomains
        var dotCount = domain.Count(c => c == '.');
        if (dotCount >= 4)
        {
            result.ThreatScore = Math.Max(result.ThreatScore, 40);
            result.Reasons.Add($"Suspicious number of subdomains ({dotCount} levels)");
        }
    }

    private async Task CheckRedirects(UrlCheckResult result)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, result.OriginalUrl);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            result.FinalUrl = response.RequestMessage?.RequestUri?.ToString();

            // If redirected to a different domain, that's suspicious
            if (result.FinalUrl != null)
            {
                var finalUri = new Uri(result.FinalUrl);
                if (finalUri.Host != result.Domain)
                {
                    result.ThreatScore = Math.Max(result.ThreatScore, 40);
                    result.Reasons.Add($"URL redirects to a different domain: {finalUri.Host}");
                }
            }

            // Check for file download content types
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var dangerousTypes = new[] {
                "application/octet-stream", "application/x-msdownload",
                "application/x-executable", "application/zip",
                "application/x-rar-compressed", "application/javascript"
            };

            if (dangerousTypes.Any(t => contentType.Contains(t)))
            {
                result.ThreatScore = Math.Max(result.ThreatScore, 80);
                result.Reasons.Add($"Link triggers a file download ({contentType})");
                result.TriggersDownload = true;
            }
        }
        catch (TaskCanceledException)
        {
            result.Reasons.Add("URL took too long to respond — could be suspicious");
            result.ThreatScore = Math.Max(result.ThreatScore, 20);
        }
        catch (HttpRequestException)
        {
            result.Reasons.Add("Could not reach URL — domain may not exist");
            result.ThreatScore = Math.Max(result.ThreatScore, 25);
        }
    }
}

public class UrlCheckResult
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string? FinalUrl { get; set; }
    public string? Domain { get; set; }
    public bool IsSafe { get; set; } = true;
    public decimal ThreatScore { get; set; }
    public bool TriggersDownload { get; set; }
    public List<string> Reasons { get; set; } = new();
}
