using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jamaa.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAccountingPeriodUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep a single row per organisation + date range before creating the unique index.
            migrationBuilder.Sql(@"
DELETE FROM AccountingPeriods
WHERE rowid NOT IN (
    SELECT MIN(rowid)
    FROM AccountingPeriods
    GROUP BY OrganisationId, StartDate, EndDate
);");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_OrganisationId_StartDate_EndDate",
                table: "AccountingPeriods",
                columns: new[] { "OrganisationId", "StartDate", "EndDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountingPeriods_OrganisationId_StartDate_EndDate",
                table: "AccountingPeriods");
        }
    }
}

