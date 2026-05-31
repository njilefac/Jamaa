using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jamaa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThousandSeparatorToAccountingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThousandSeparator",
                table: "AccountingSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: ",");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThousandSeparator",
                table: "AccountingSettings");
        }
    }
}
