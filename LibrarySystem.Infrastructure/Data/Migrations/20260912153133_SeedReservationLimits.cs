using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedReservationLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [CirculationPolicies]
                SET [MaximumActiveReservations] = CASE [MemberType]
                    WHEN 1 THEN 3
                    WHEN 2 THEN 5
                    WHEN 3 THEN 10
                    ELSE 3
                END
                WHERE [MaximumActiveReservations] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [CirculationPolicies] SET [MaximumActiveReservations] = 0;");
        }
    }
}
