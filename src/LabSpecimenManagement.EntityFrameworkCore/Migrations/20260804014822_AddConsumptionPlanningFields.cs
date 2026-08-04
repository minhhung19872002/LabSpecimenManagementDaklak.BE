using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumptionPlanningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvgConsumptionPerSample",
                table: "AppMaterialConsumptionItems",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedSamplesPerYear",
                table: "AppMaterialConsumptionItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgConsumptionPerSample",
                table: "AppMaterialConsumptionItems");

            migrationBuilder.DropColumn(
                name: "EstimatedSamplesPerYear",
                table: "AppMaterialConsumptionItems");
        }
    }
}
