using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialSystem.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsInTransactionsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastProcessedDate",
                table: "AppUnplannedExpensesAndProfits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProcessedDate",
                table: "AppPlannedExpensesAndProfits",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastProcessedDate",
                table: "AppUnplannedExpensesAndProfits");

            migrationBuilder.DropColumn(
                name: "LastProcessedDate",
                table: "AppPlannedExpensesAndProfits");
        }
    }
}
