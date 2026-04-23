using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhishGuard.Migrations
{
    /// <inheritdoc />
    public partial class FixClerkUserIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_ClerkUserId",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ClerkUserId",
                table: "Employees",
                column: "ClerkUserId",
                unique: true,
                filter: "`ClerkUserId` IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_ClerkUserId",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ClerkUserId",
                table: "Employees",
                column: "ClerkUserId",
                unique: true);
        }
    }
}
