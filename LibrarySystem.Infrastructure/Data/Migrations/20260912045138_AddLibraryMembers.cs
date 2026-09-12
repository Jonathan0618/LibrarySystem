using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddLibraryMembers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LibraryMembers",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                MemberNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                MemberType = table.Column<int>(type: "int", nullable: false),
                Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LibraryMembers", x => x.Id);
                table.ForeignKey(
                    name: "FK_LibraryMembers_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LibraryMembers_MemberNumber",
            table: "LibraryMembers",
            column: "MemberNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LibraryMembers_MemberType_IsActive",
            table: "LibraryMembers",
            columns: new[] { "MemberType", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_LibraryMembers_UserId",
            table: "LibraryMembers",
            column: "UserId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LibraryMembers");
    }
}
