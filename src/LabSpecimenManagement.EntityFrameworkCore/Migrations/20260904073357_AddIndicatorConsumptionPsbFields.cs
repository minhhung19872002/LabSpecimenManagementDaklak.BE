using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicatorConsumptionPsbFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsumptionDate",
                table: "AppIndicatorConsumptions",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreparedSolutionBatchId",
                table: "AppIndicatorConsumptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicianName",
                table: "AppIndicatorConsumptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorConsumptions_PreparedSolutionBatchId",
                table: "AppIndicatorConsumptions",
                column: "PreparedSolutionBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppIndicatorConsumptions_PreparedSolutionBatchId",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "ConsumptionDate",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "PreparedSolutionBatchId",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "TechnicianName",
                table: "AppIndicatorConsumptions");
        }
    }
}
