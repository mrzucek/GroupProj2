using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhishGuard.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 249, DateTimeKind.Utc).AddTicks(9854));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(408));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(410));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(411));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(412));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(413));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(414));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(488));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(489));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(490));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(491));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(492));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(493));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(493));

            migrationBuilder.UpdateData(
                table: "ScoringRules",
                keyColumn: "RuleId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 24, 20, 52, 53, 250, DateTimeKind.Utc).AddTicks(494));
        }
    }
}
