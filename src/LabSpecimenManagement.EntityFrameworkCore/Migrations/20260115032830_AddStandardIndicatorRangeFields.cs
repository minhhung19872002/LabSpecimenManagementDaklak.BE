using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardIndicatorRangeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LowerLimit",
                table: "AppStandardIndicators",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LowerOperator",
                table: "AppStandardIndicators",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpperLimit",
                table: "AppStandardIndicators",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpperOperator",
                table: "AppStandardIndicators",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LowerLimit",
                table: "AppStandardIndicators");

            migrationBuilder.DropColumn(
                name: "LowerOperator",
                table: "AppStandardIndicators");

            migrationBuilder.DropColumn(
                name: "UpperLimit",
                table: "AppStandardIndicators");

            migrationBuilder.DropColumn(
                name: "UpperOperator",
                table: "AppStandardIndicators");
        }
    }
}
