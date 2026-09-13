using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedFinePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [CirculationPolicies]
                SET [FineGracePeriodDays] = CASE WHEN [MemberType] = 2 THEN 2 ELSE 1 END,
                    [DailyOverdueFine] = CASE WHEN [MemberType] = 2 THEN 0.50 ELSE 1.00 END,
                    [MaximumOverdueFine] = 100.00,
                    [LostItemFine] = 500.00,
                    [DamagedItemFine] = 250.00
                WHERE [MaximumOverdueFine] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [CirculationPolicies]
                SET [FineGracePeriodDays] = 0,
                    [DailyOverdueFine] = 0,
                    [MaximumOverdueFine] = 0,
                    [LostItemFine] = 0,
                    [DamagedItemFine] = 0;
                """);
        }
    }
}
