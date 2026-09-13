using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAcquisitionVendors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcquisitionRecords_AcquisitionVendors_VendorId",
                table: "AcquisitionRecords");

            migrationBuilder.DropTable(
                name: "AcquisitionVendors");

            migrationBuilder.DropIndex(
                name: "IX_AcquisitionRecords_VendorId",
                table: "AcquisitionRecords");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "AcquisitionRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "AcquisitionRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcquisitionVendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionVendors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionRecords_VendorId",
                table: "AcquisitionRecords",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionVendors_Name",
                table: "AcquisitionVendors",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionRecords_AcquisitionVendors_VendorId",
                table: "AcquisitionRecords",
                column: "VendorId",
                principalTable: "AcquisitionVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
