using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhishGuard.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DailyMetrics",
                columns: table => new
                {
                    MetricId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MetricDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalEmailsScanned = table.Column<int>(type: "int", nullable: false),
                    TotalBlocked = table.Column<int>(type: "int", nullable: false),
                    TotalWarnings = table.Column<int>(type: "int", nullable: false),
                    TotalSafe = table.Column<int>(type: "int", nullable: false),
                    TotalEmployeeReports = table.Column<int>(type: "int", nullable: false),
                    TotalConfirmedPhishing = table.Column<int>(type: "int", nullable: false),
                    TotalUrlsChecked = table.Column<int>(type: "int", nullable: false),
                    TotalUrlsBlocked = table.Column<int>(type: "int", nullable: false),
                    TotalSimulationsSent = table.Column<int>(type: "int", nullable: false),
                    TotalSimulationsCaught = table.Column<int>(type: "int", nullable: false),
                    CompanySecurityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMetrics", x => x.MetricId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Department = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThreatIndicators",
                columns: table => new
                {
                    IndicatorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreatIndicators", x => x.IndicatorId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Emails",
                columns: table => new
                {
                    EmailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecipientId = table.Column<int>(type: "int", nullable: false),
                    SenderAddress = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderDisplayName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyPreview = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    IsReported = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emails", x => x.EmailId);
                    table.ForeignKey(
                        name: "FK_Emails_Employees_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmployeeTrainings",
                columns: table => new
                {
                    TrainingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CurrentDifficulty = table.Column<int>(type: "int", nullable: false),
                    TotalSimulationsReceived = table.Column<int>(type: "int", nullable: false),
                    TotalCorrectlyReported = table.Column<int>(type: "int", nullable: false),
                    TotalClicked = table.Column<int>(type: "int", nullable: false),
                    TotalIgnored = table.Column<int>(type: "int", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    BestStreak = table.Column<int>(type: "int", nullable: false),
                    ScorePoints = table.Column<int>(type: "int", nullable: false),
                    LastSimulationAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DifficultyUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTrainings", x => x.TrainingId);
                    table.ForeignKey(
                        name: "FK_EmployeeTrainings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PhishingCampaigns",
                columns: table => new
                {
                    CampaignId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CampaignName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    TemplateSubject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateBody = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateSender = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhishingIndicators = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhishingCampaigns", x => x.CampaignId);
                    table.ForeignKey(
                        name: "FK_PhishingCampaigns_Employees_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ScoringRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Dimension = table.Column<int>(type: "int", nullable: false),
                    RuleName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RuleDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pattern = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScoreValue = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_ScoringRules_Employees_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmailScores",
                columns: table => new
                {
                    ScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmailId = table.Column<int>(type: "int", nullable: false),
                    Dimension = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    Details = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailScores", x => x.ScoreId);
                    table.ForeignKey(
                        name: "FK_EmailScores_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "EmailId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmailUrls",
                columns: table => new
                {
                    UrlId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmailId = table.Column<int>(type: "int", nullable: false),
                    OriginalUrl = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FinalUrl = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Domain = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSafe = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    SafeBrowsingResult = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DomainAgeDays = table.Column<int>(type: "int", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailUrls", x => x.UrlId);
                    table.ForeignKey(
                        name: "FK_EmailUrls_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "EmailId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PhishingReports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmailId = table.Column<int>(type: "int", nullable: false),
                    ReportedBy = table.Column<int>(type: "int", nullable: false),
                    ReportReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsConfirmedPhishing = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhishingReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_PhishingReports_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "EmailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhishingReports_Employees_ReportedBy",
                        column: x => x.ReportedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_PhishingReports_Employees_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SimulationEmails",
                columns: table => new
                {
                    SimulationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    TargetEmployeeId = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClickedLinkAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationEmails", x => x.SimulationId);
                    table.ForeignKey(
                        name: "FK_SimulationEmails_Employees_TargetEmployeeId",
                        column: x => x.TargetEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SimulationEmails_PhishingCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "PhishingCampaigns",
                        principalColumn: "CampaignId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ScoringRules",
                columns: new[] { "RuleId", "CreatedAt", "Dimension", "IsActive", "Pattern", "RuleDescription", "RuleName", "ScoreValue", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 24, 20, 52, 53, 249, DateTimeKind.Utc).AddTicks(9854), 0, true, null, "Domain found in threat intelligence feeds", "Known phishing domain", 90m, null },
                    { 2, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(408), 0, true, null, "Exact URL found in threat feeds", "Known phishing URL", 95m, null },
                    { 3, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(410), 0, true, null, "Sender email in threat database", "Known phishing sender", 85m, null },
                    { 4, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(411), 1, true, null, "Sender domain registered less than 30 days ago", "New domain (< 30 days)", 40m, null },
                    { 5, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(412), 1, true, null, "Sender domain registered less than 7 days ago", "Very new domain (< 7 days)", 70m, null },
                    { 6, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(413), 1, true, null, "Domain similar to a known legitimate domain", "Lookalike domain", 60m, null },
                    { 7, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(414), 2, true, "act now|immediate action|expires today|last chance|urgent", "Email contains artificial urgency", "Urgency language", 25m, null },
                    { 8, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(488), 2, true, "CEO|director|executive|management requires", "Email impersonates authority figure", "Authority manipulation", 30m, null },
                    { 9, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(489), 2, true, "account suspended|security breach|unauthorized access|locked out", "Email uses fear to prompt action", "Fear tactics", 35m, null },
                    { 10, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(490), 2, true, "you've won|congratulations|free gift|claim your prize", "Email promises unrealistic rewards", "Too-good-to-be-true", 45m, null },
                    { 11, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(491), 2, true, "verify your account|confirm your password|update your information|click here to log in", "Email requests sensitive information", "Credential request", 50m, null },
                    { 12, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(492), 3, true, null, "Email from internal sender outside business hours", "Off-hours email", 15m, null },
                    { 13, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(493), 3, true, null, "Sender has never emailed this recipient before", "First-time sender", 20m, null },
                    { 14, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(493), 4, true, null, "Email HTML structure doesn't match known sender templates", "Template mismatch", 35m, null },
                    { 15, new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(494), 4, true, null, "Email contains hidden or obfuscated content", "Hidden text/links", 55m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetrics_MetricDate",
                table: "DailyMetrics",
                column: "MetricDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Classification",
                table: "Emails",
                column: "Classification");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_OverallScore",
                table: "Emails",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_RecipientId_ReceivedAt",
                table: "Emails",
                columns: new[] { "RecipientId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailScores_EmailId_Dimension",
                table: "EmailScores",
                columns: new[] { "EmailId", "Dimension" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailUrls_Domain",
                table: "EmailUrls",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_EmailUrls_EmailId",
                table: "EmailUrls",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainings_EmployeeId",
                table: "EmployeeTrainings",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhishingCampaigns_CreatedBy",
                table: "PhishingCampaigns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PhishingReports_EmailId",
                table: "PhishingReports",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_PhishingReports_ReportedBy",
                table: "PhishingReports",
                column: "ReportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PhishingReports_ReviewedBy",
                table: "PhishingReports",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringRules_UpdatedBy",
                table: "ScoringRules",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationEmails_CampaignId",
                table: "SimulationEmails",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationEmails_TargetEmployeeId_Result",
                table: "SimulationEmails",
                columns: new[] { "TargetEmployeeId", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreatIndicators_Type_Value",
                table: "ThreatIndicators",
                columns: new[] { "Type", "Value" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyMetrics");

            migrationBuilder.DropTable(
                name: "EmailScores");

            migrationBuilder.DropTable(
                name: "EmailUrls");

            migrationBuilder.DropTable(
                name: "EmployeeTrainings");

            migrationBuilder.DropTable(
                name: "PhishingReports");

            migrationBuilder.DropTable(
                name: "ScoringRules");

            migrationBuilder.DropTable(
                name: "SimulationEmails");

            migrationBuilder.DropTable(
                name: "ThreatIndicators");

            migrationBuilder.DropTable(
                name: "Emails");

            migrationBuilder.DropTable(
                name: "PhishingCampaigns");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
