using Microsoft.EntityFrameworkCore;
using PhishGuard.Data;
using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public class DomainReputationScorer : IScoringDimension
{
    private readonly PhishGuardContext _db;

    public DomainReputationScorer(PhishGuardContext db)
    {
        _db = db;
    }

    public ScoringDimension Dimension => ScoringDimension.DomainReputation;

    public async Task<EmailScore> AnalyzeAsync(Email email)
    {
        var senderDomain = email.SenderAddress.Split('@').LastOrDefault()?.ToLowerInvariant() ?? "";

        // Short-circuit: if domain is trusted, score 0
        var isTrusted = await _db.TrustedDomains
            .AnyAsync(t => t.Domain.ToLower() == senderDomain);
        if (isTrusted)
        {
            return new EmailScore
            {
                EmailId = email.EmailId,
                Dimension = Dimension,
                Score = 0,
                Weight = 0.9m,
                Details = "Domain is on the trusted list"
            };
        }

        decimal score = 0;
        var details = new List<string>();

        // Free email providers sending "business" emails
        var freeProviders = new HashSet<string>
        {
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com",
            "aol.com", "protonmail.com", "mail.com", "yandex.com"
        };

        // Check for lookalike domains (common typosquatting patterns)
        var knownBrands = new Dictionary<string, string>
        {
            { "paypal", "paypal.com" },
            { "microsoft", "microsoft.com" },
            { "apple", "apple.com" },
            { "amazon", "amazon.com" },
            { "google", "google.com" },
            { "facebook", "facebook.com" },
            { "netflix", "netflix.com" },
            { "bank", "various" }
        };

        foreach (var (keyword, legitimate) in knownBrands)
        {
            if (senderDomain.Contains(keyword) && senderDomain != legitimate && legitimate != "various")
            {
                score = Math.Max(score, 60);
                details.Add($"Lookalike domain: contains '{keyword}' but is not {legitimate}");
            }
        }

        // Check for excessive subdomains
        var dotCount = senderDomain.Count(c => c == '.');
        if (dotCount >= 3)
        {
            score = Math.Max(score, 30);
            details.Add($"Suspicious subdomain depth ({dotCount} levels)");
        }

        // Check for IP-based sender
        if (System.Text.RegularExpressions.Regex.IsMatch(senderDomain, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
        {
            score = Math.Max(score, 50);
            details.Add("IP address used instead of domain");
        }

        return new EmailScore
        {
            EmailId = email.EmailId,
            Dimension = Dimension,
            Score = score,
            Weight = 0.9m,
            Details = details.Count > 0 ? string.Join("; ", details) : "Domain appears legitimate"
        };
    }
}
