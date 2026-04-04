using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jamaa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardLayoutToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DashboardLayout",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DashboardLayout",
                table: "Users");
        }
    }
}
