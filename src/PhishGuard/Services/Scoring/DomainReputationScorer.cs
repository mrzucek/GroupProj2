using PhishGuard.Models;

namespace PhishGuard.Services.Scoring;

public class DomainReputationScorer : IScoringDimension
{
    public ScoringDimension Dimension => ScoringDimension.DomainReputation;

    public Task<EmailScore> AnalyzeAsync(Email email)
    {
        var senderDomain = email.SenderAddress.Split('@').LastOrDefault()?.ToLowerInvariant() ?? "";
        decimal score = 0;
        var details = new List<string>();

        // Check for suspicious domain characteristics
        // Free email providers sending "business" emails
        var freeProviders = new HashSet<string>
        {
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com",
            "aol.com", "protonmail.com", "mail.com", "yandex.com"
        };

        // Check for lookalike domains (common typosquatting patterns)
        var trustedDomains = new Dictionary<string, string>
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

        foreach (var (keyword, legitimate) in trustedDomains)
        {
            if (senderDomain.Contains(keyword) && senderDomain != legitimate && legitimate != "various")
            {
                score = Math.Max(score, 60);
                details.Add($"Lookalike domain: contains '{keyword}' but is not {legitimate}");
            }
        }

        // Check for excessive subdomains (e.g., login.paypal.com.evil.com)
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

        return Task.FromResult(new EmailScore
        {
            EmailId = email.EmailId,
            Dimension = Dimension,
            Score = score,
            Weight = 0.9m,
            Details = details.Count > 0 ? string.Join("; ", details) : "Domain appears legitimate"
        });
    }
}
