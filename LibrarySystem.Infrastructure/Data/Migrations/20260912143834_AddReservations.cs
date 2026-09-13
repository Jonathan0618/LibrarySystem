using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<long>(type: "bigint", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedBookCopyId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ReadyAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_CatalogBookCopies_AssignedBookCopyId",
                        column: x => x.AssignedBookCopyId,
                        principalTable: "CatalogBookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_CatalogBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "CatalogBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_LibraryMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "LibraryMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_AssignedBookCopyId",
                table: "Reservations",
                column: "AssignedBookCopyId",
                unique: true,
                filter: "[AssignedBookCopyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookId_Status_CreatedAtUtc",
                table: "Reservations",
                columns: new[] { "BookId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_MemberId_Status",
                table: "Reservations",
                columns: new[] { "MemberId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");
        }
    }
}
