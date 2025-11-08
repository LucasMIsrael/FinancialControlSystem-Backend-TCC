using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialSystem.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoveEnvironmentField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AppEnvironments");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
