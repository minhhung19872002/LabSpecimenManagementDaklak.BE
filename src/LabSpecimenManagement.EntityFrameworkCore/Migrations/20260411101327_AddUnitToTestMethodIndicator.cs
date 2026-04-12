using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitToTestMethodIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormattedUnit",
                table: "AppTestMethodIndicators",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "AppTestMethodIndicators",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Backfill: copy Unit/FormattedUnit from TestMethod to TestMethodIndicator
            migrationBuilder.Sql(@"
                UPDATE ""AppTestMethodIndicators"" tmi
                SET ""Unit"" = tm.""Unit"", ""FormattedUnit"" = tm.""FormattedUnit""
                FROM ""AppTestMethods"" tm
                WHERE tmi.""TestMethodId"" = tm.""Id"" AND tm.""Unit"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormattedUnit",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "AppTestMethodIndicators");
        }
    }
}
