using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecimenTypeIdToTestMethodIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SpecimenTypeId column
            migrationBuilder.AddColumn<Guid>(
                name: "SpecimenTypeId",
                table: "AppTestMethodIndicators",
                type: "uuid",
                nullable: true);

            // Drop old unique index (TestMethodId, IndicatorId)
            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId",
                table: "AppTestMethodIndicators");

            // Create index on SpecimenTypeId
            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_SpecimenTypeId",
                table: "AppTestMethodIndicators",
                column: "SpecimenTypeId");

            // Create new unique index (TestMethodId, IndicatorId, SpecimenTypeId)
            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId_SpecimenTypeId",
                table: "AppTestMethodIndicators",
                columns: new[] { "TestMethodId", "IndicatorId", "SpecimenTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new unique index
            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId_SpecimenTypeId",
                table: "AppTestMethodIndicators");

            // Drop SpecimenTypeId index
            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_SpecimenTypeId",
                table: "AppTestMethodIndicators");

            // Recreate old unique index
            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId",
                table: "AppTestMethodIndicators",
                columns: new[] { "TestMethodId", "IndicatorId" },
                unique: true);

            // Drop SpecimenTypeId column
            migrationBuilder.DropColumn(
                name: "SpecimenTypeId",
                table: "AppTestMethodIndicators");
        }
    }
}
