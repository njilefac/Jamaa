using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jamaa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingPeriodBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingPeriodBalances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    FiscalYearId = table.Column<string>(type: "TEXT", nullable: false),
                    AccountingPeriodId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<string>(type: "TEXT", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriodBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingPeriodBalances_AccountingPeriods_AccountingPeriodId",
                        column: x => x.AccountingPeriodId,
                        principalTable: "AccountingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountingPeriodBalances_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountingPeriodBalances_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriodBalances_AccountId",
                table: "AccountingPeriodBalances",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriodBalances_AccountingPeriodId",
                table: "AccountingPeriodBalances",
                column: "AccountingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriodBalances_FiscalYearId",
                table: "AccountingPeriodBalances",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriodBalances_OrganisationId_AccountId_FiscalYearId_AccountingPeriodId",
                table: "AccountingPeriodBalances",
                columns: new[] { "OrganisationId", "AccountId", "FiscalYearId", "AccountingPeriodId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingPeriodBalances");
        }
    }
}
