using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jamaa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingSettings",
                columns: table => new
                {
                    OrganisationId = table.Column<string>(type: "TEXT", nullable: false),
                    BaseCurrency = table.Column<string>(type: "TEXT", nullable: false),
                    DateFormat = table.Column<string>(type: "TEXT", nullable: false),
                    DecimalPrecision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSettings", x => x.OrganisationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingSettings");
        }
    }
}
