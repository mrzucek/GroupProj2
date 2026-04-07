using Microsoft.EntityFrameworkCore;
using PhishGuard.Models;

namespace PhishGuard.Data;

public class PhishGuardContext : DbContext
{
    public PhishGuardContext(DbContextOptions<PhishGuardContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Email> Emails => Set<Email>();
    public DbSet<EmailScore> EmailScores => Set<EmailScore>();
    public DbSet<EmailUrl> EmailUrls => Set<EmailUrl>();
    public DbSet<ThreatIndicator> ThreatIndicators => Set<ThreatIndicator>();
    public DbSet<ScoringRule> ScoringRules => Set<ScoringRule>();
    public DbSet<PhishingReport> PhishingReports => Set<PhishingReport>();
    public DbSet<PhishingCampaign> PhishingCampaigns => Set<PhishingCampaign>();
    public DbSet<SimulationEmail> SimulationEmails => Set<SimulationEmail>();
    public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();
    public DbSet<DailyMetric> DailyMetrics => Set<DailyMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Employee
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.EmployeeId);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.ClerkUserId).IsUnique();
        });

        // Email
        modelBuilder.Entity<Email>(e =>
        {
            e.HasKey(x => x.EmailId);
            e.HasOne(x => x.Recipient)
                .WithMany(x => x.Emails)
                .HasForeignKey(x => x.RecipientId);
            e.HasIndex(x => new { x.RecipientId, x.ReceivedAt });
            e.HasIndex(x => x.Classification);
            e.HasIndex(x => x.OverallScore);
            e.Property(x => x.OverallScore).HasPrecision(5, 2);
        });

        // EmailScore
        modelBuilder.Entity<EmailScore>(e =>
        {
            e.HasKey(x => x.ScoreId);
            e.HasOne(x => x.Email)
                .WithMany(x => x.Scores)
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EmailId, x.Dimension });
            e.Property(x => x.Score).HasPrecision(5, 2);
            e.Property(x => x.Weight).HasPrecision(3, 2);
        });

        // EmailUrl
        modelBuilder.Entity<EmailUrl>(e =>
        {
            e.HasKey(x => x.UrlId);
            e.HasOne(x => x.Email)
                .WithMany(x => x.Urls)
                .HasForeignKey(x => x.EmailId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Domain);
            e.Property(x => x.ThreatScore).HasPrecision(5, 2);
        });

        // ThreatIndicator
        modelBuilder.Entity<ThreatIndicator>(e =>
        {
            e.HasKey(x => x.IndicatorId);
            e.HasIndex(x => new { x.Type, x.Value });
        });

        // ScoringRule
        modelBuilder.Entity<ScoringRule>(e =>
        {
            e.HasKey(x => x.RuleId);
            e.HasOne(x => x.UpdatedByEmployee)
                .WithMany()
                .HasForeignKey(x => x.UpdatedBy)
                .IsRequired(false);
            e.Property(x => x.ScoreValue).HasPrecision(5, 2);
        });

        // PhishingReport
        modelBuilder.Entity<PhishingReport>(e =>
        {
            e.HasKey(x => x.ReportId);
            e.HasOne(x => x.Email)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.EmailId);
            e.HasOne(x => x.Reporter)
                .WithMany()
                .HasForeignKey(x => x.ReportedBy)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Reviewer)
                .WithMany()
                .HasForeignKey(x => x.ReviewedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.EmailId);
        });

        // PhishingCampaign
        modelBuilder.Entity<PhishingCampaign>(e =>
        {
            e.HasKey(x => x.CampaignId);
            e.HasOne(x => x.Creator)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy);
        });

        // SimulationEmail
        modelBuilder.Entity<SimulationEmail>(e =>
        {
            e.HasKey(x => x.SimulationId);
            e.HasOne(x => x.Campaign)
                .WithMany(x => x.Simulations)
                .HasForeignKey(x => x.CampaignId);
            e.HasOne(x => x.TargetEmployee)
                .WithMany()
                .HasForeignKey(x => x.TargetEmployeeId);
            e.HasIndex(x => new { x.TargetEmployeeId, x.Result });
        });

        // EmployeeTraining
        modelBuilder.Entity<EmployeeTraining>(e =>
        {
            e.HasKey(x => x.TrainingId);
            e.HasOne(x => x.Employee)
                .WithOne(x => x.Training)
                .HasForeignKey<EmployeeTraining>(x => x.EmployeeId);
            e.HasIndex(x => x.EmployeeId).IsUnique();
        });

        // DailyMetric
        modelBuilder.Entity<DailyMetric>(e =>
        {
            e.HasKey(x => x.MetricId);
            e.HasIndex(x => x.MetricDate).IsUnique();
            e.Property(x => x.CompanySecurityScore).HasPrecision(5, 2);
        });

        SeedDefaultRules(modelBuilder);
    }

    private static void SeedDefaultRules(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<ScoringRule>().HasData(
            // Threat feed rules
            new ScoringRule { RuleId = 1, Dimension = ScoringDimension.ThreatFeed, RuleName = "Known phishing domain", RuleDescription = "Domain found in threat intelligence feeds", ScoreValue = 90, CreatedAt = seedDate },
            new ScoringRule { RuleId = 2, Dimension = ScoringDimension.ThreatFeed, RuleName = "Known phishing URL", RuleDescription = "Exact URL found in threat feeds", ScoreValue = 95, CreatedAt = seedDate },
            new ScoringRule { RuleId = 3, Dimension = ScoringDimension.ThreatFeed, RuleName = "Known phishing sender", RuleDescription = "Sender email in threat database", ScoreValue = 85, CreatedAt = seedDate },

            // Domain reputation rules
            new ScoringRule { RuleId = 4, Dimension = ScoringDimension.DomainReputation, RuleName = "New domain (< 30 days)", RuleDescription = "Sender domain registered less than 30 days ago", ScoreValue = 40, CreatedAt = seedDate },
            new ScoringRule { RuleId = 5, Dimension = ScoringDimension.DomainReputation, RuleName = "Very new domain (< 7 days)", RuleDescription = "Sender domain registered less than 7 days ago", ScoreValue = 70, CreatedAt = seedDate },
            new ScoringRule { RuleId = 6, Dimension = ScoringDimension.DomainReputation, RuleName = "Lookalike domain", RuleDescription = "Domain similar to a known legitimate domain", ScoreValue = 60, CreatedAt = seedDate },

            // Language analysis rules
            new ScoringRule { RuleId = 7, Dimension = ScoringDimension.LanguageAnalysis, RuleName = "Urgency language", Pattern = "act now|immediate action|expires today|last chance|urgent", RuleDescription = "Email contains artificial urgency", ScoreValue = 25, CreatedAt = seedDate },
            new ScoringRule { RuleId = 8, Dimension = ScoringDimension.LanguageAnalysis, RuleName = "Authority manipulation", Pattern = "CEO|director|executive|management requires", RuleDescription = "Email impersonates authority figure", ScoreValue = 30, CreatedAt = seedDate },
            new ScoringRule { RuleId = 9, Dimension = ScoringDimension.LanguageAnalysis, RuleName = "Fear tactics", Pattern = "account suspended|security breach|unauthorized access|locked out", RuleDescription = "Email uses fear to prompt action", ScoreValue = 35, CreatedAt = seedDate },
            new ScoringRule { RuleId = 10, Dimension = ScoringDimension.LanguageAnalysis, RuleName = "Too-good-to-be-true", Pattern = "you've won|congratulations|free gift|claim your prize", RuleDescription = "Email promises unrealistic rewards", ScoreValue = 45, CreatedAt = seedDate },
            new ScoringRule { RuleId = 11, Dimension = ScoringDimension.LanguageAnalysis, RuleName = "Credential request", Pattern = "verify your account|confirm your password|update your information|click here to log in", RuleDescription = "Email requests sensitive information", ScoreValue = 50, CreatedAt = seedDate },

            // Behavioral timing rules
            new ScoringRule { RuleId = 12, Dimension = ScoringDimension.BehavioralTiming, RuleName = "Off-hours email", RuleDescription = "Email from internal sender outside business hours", ScoreValue = 15, CreatedAt = seedDate },
            new ScoringRule { RuleId = 13, Dimension = ScoringDimension.BehavioralTiming, RuleName = "First-time sender", RuleDescription = "Sender has never emailed this recipient before", ScoreValue = 20, CreatedAt = seedDate },

            // HTML fingerprint rules
            new ScoringRule { RuleId = 14, Dimension = ScoringDimension.HtmlFingerprint, RuleName = "Template mismatch", RuleDescription = "Email HTML structure doesn't match known sender templates", ScoreValue = 35, CreatedAt = seedDate },
            new ScoringRule { RuleId = 15, Dimension = ScoringDimension.HtmlFingerprint, RuleName = "Hidden text/links", RuleDescription = "Email contains hidden or obfuscated content", ScoreValue = 55, CreatedAt = seedDate }
        );
    }
}
