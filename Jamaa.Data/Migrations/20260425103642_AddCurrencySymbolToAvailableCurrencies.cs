using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jamaa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencySymbolToAvailableCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencySymbol",
                table: "AccountingAvailableCurrencies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencySymbol",
                table: "AccountingAvailableCurrencies");
        }
    }
}
