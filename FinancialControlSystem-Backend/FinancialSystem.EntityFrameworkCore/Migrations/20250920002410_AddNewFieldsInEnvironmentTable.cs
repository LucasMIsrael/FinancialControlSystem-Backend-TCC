using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialSystem.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsInEnvironmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinancialControlLevel",
                table: "AppEnvironments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TotalBalance",
                table: "AppEnvironments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalGoalsAchieved",
                table: "AppEnvironments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinancialControlLevel",
                table: "AppEnvironments");

            migrationBuilder.DropColumn(
                name: "TotalBalance",
                table: "AppEnvironments");

            migrationBuilder.DropColumn(
                name: "TotalGoalsAchieved",
                table: "AppEnvironments");
        }
    }
}
