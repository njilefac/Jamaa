using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jamaa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingAvailableCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingAvailableCurrencies",
                columns: table => new
                {
                    OrganisationId = table.Column<string>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingAvailableCurrencies", x => new { x.OrganisationId, x.CurrencyCode });
                    table.ForeignKey(
                        name: "FK_AccountingAvailableCurrencies_AccountingSettings_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "AccountingSettings",
                        principalColumn: "OrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingAvailableCurrencies");
        }
    }
}
