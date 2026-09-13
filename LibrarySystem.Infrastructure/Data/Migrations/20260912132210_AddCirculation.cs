using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCirculation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CirculationLoans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CheckoutOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    BookCopyId = table.Column<long>(type: "bigint", nullable: false),
                    CheckedOutAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RenewalCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CirculationLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CirculationLoans_CatalogBookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "CatalogBookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CirculationLoans_LibraryMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "LibraryMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CirculationPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberType = table.Column<int>(type: "int", nullable: false),
                    LoanPeriodDays = table.Column<int>(type: "int", nullable: false),
                    MaximumActiveLoans = table.Column<int>(type: "int", nullable: false),
                    MaximumRenewals = table.Column<int>(type: "int", nullable: false),
                    AllowRenewalWhenOverdue = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CirculationPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CirculationLoanHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CirculationLoanHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CirculationLoanHistory_CirculationLoans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "CirculationLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CirculationLoanHistory_LoanId_OccurredAtUtc",
                table: "CirculationLoanHistory",
                columns: new[] { "LoanId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CirculationLoans_BookCopyId_Status",
                table: "CirculationLoans",
                columns: new[] { "BookCopyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CirculationLoans_CheckoutOperationId_BookCopyId",
                table: "CirculationLoans",
                columns: new[] { "CheckoutOperationId", "BookCopyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CirculationLoans_DueAtUtc_Status",
                table: "CirculationLoans",
                columns: new[] { "DueAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CirculationLoans_MemberId_Status",
                table: "CirculationLoans",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CirculationPolicies_MemberType",
                table: "CirculationPolicies",
                column: "MemberType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CirculationLoanHistory");

            migrationBuilder.DropTable(
                name: "CirculationPolicies");

            migrationBuilder.DropTable(
                name: "CirculationLoans");
        }
    }
}
