using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogReferenceArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CatalogShelfLocations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CatalogPublishers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CatalogCategories",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CatalogAuthors",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CatalogShelfLocations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CatalogPublishers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CatalogCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CatalogAuthors");
        }
    }
}
