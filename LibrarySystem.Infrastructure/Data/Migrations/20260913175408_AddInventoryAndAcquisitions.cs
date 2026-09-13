using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAndAcquisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcquisitionBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcquisitionVendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionVendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopyWithdrawals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookCopyId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WithdrawnAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopyWithdrawals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopyWithdrawals_CatalogBookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "CatalogBookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventorySessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcquisitionRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BookId = table.Column<long>(type: "bigint", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    DonationSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    QuantityOrdered = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcquisitionRecords_AcquisitionVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "AcquisitionVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcquisitionRecords_CatalogBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "CatalogBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryExpectedCopies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventorySessionId = table.Column<long>(type: "bigint", nullable: false),
                    BookCopyId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryExpectedCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryExpectedCopies_CatalogBookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "CatalogBookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryExpectedCopies_InventorySessions_InventorySessionId",
                        column: x => x.InventorySessionId,
                        principalTable: "InventorySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryScans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventorySessionId = table.Column<long>(type: "bigint", nullable: false),
                    BookCopyId = table.Column<long>(type: "bigint", nullable: false),
                    FoundAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    FoundByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryScans_CatalogBookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "CatalogBookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryScans_InventorySessions_InventorySessionId",
                        column: x => x.InventorySessionId,
                        principalTable: "InventorySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcquisitionReceipts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcquisitionRecordId = table.Column<long>(type: "bigint", nullable: false),
                    BookCopyId = table.Column<long>(type: "bigint", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                    ReceivedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcquisitionReceipts_AcquisitionRecords_AcquisitionRecordId",
                        column: x => x.AcquisitionRecordId,
                        principalTable: "AcquisitionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcquisitionReceipts_CatalogBookCopies_BookCopyId",
                        column: x => x.BookCopyId,
                        principalTable: "CatalogBookCopies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionBudgets_Year",
                table: "AcquisitionBudgets",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionReceipts_AcquisitionRecordId",
                table: "AcquisitionReceipts",
                column: "AcquisitionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionReceipts_BookCopyId",
                table: "AcquisitionReceipts",
                column: "BookCopyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRecords_BookId",
                table: "AcquisitionRecords",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRecords_OrderNumber",
                table: "AcquisitionRecords",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRecords_VendorId",
                table: "AcquisitionRecords",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionVendors_Name",
                table: "AcquisitionVendors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CopyWithdrawals_BookCopyId",
                table: "CopyWithdrawals",
                column: "BookCopyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryExpectedCopies_BookCopyId",
                table: "InventoryExpectedCopies",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryExpectedCopies_InventorySessionId_BookCopyId",
                table: "InventoryExpectedCopies",
                columns: new[] { "InventorySessionId", "BookCopyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryScans_BookCopyId",
                table: "InventoryScans",
                column: "BookCopyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryScans_InventorySessionId_BookCopyId",
                table: "InventoryScans",
                columns: new[] { "InventorySessionId", "BookCopyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcquisitionBudgets");

            migrationBuilder.DropTable(
                name: "AcquisitionReceipts");

            migrationBuilder.DropTable(
                name: "CopyWithdrawals");

            migrationBuilder.DropTable(
                name: "InventoryExpectedCopies");

            migrationBuilder.DropTable(
                name: "InventoryScans");

            migrationBuilder.DropTable(
                name: "AcquisitionRecords");

            migrationBuilder.DropTable(
                name: "InventorySessions");

            migrationBuilder.DropTable(
                name: "AcquisitionVendors");
        }
    }
}
