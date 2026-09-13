using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Fines_Status_MemberId",
                table: "Fines",
                columns: new[] { "Status", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_CirculationLoans_CheckedOutAtUtc_Id",
                table: "CirculationLoans",
                columns: new[] { "CheckedOutAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CirculationLoans_Status_DueAtUtc",
                table: "CirculationLoans",
                columns: new[] { "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogBookCopies_Status_BookId",
                table: "CatalogBookCopies",
                columns: new[] { "Status", "BookId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc_Id",
                table: "AuditLogs",
                columns: new[] { "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fines_Status_MemberId",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_CirculationLoans_CheckedOutAtUtc_Id",
                table: "CirculationLoans");

            migrationBuilder.DropIndex(
                name: "IX_CirculationLoans_Status_DueAtUtc",
                table: "CirculationLoans");

            migrationBuilder.DropIndex(
                name: "IX_CatalogBookCopies_Status_BookId",
                table: "CatalogBookCopies");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CreatedAtUtc_Id",
                table: "AuditLogs");
        }
    }
}
