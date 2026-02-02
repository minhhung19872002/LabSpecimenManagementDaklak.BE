using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecimenTypeIdToStandardIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SpecimenTypeId column
            migrationBuilder.AddColumn<Guid>(
                name: "SpecimenTypeId",
                table: "AppStandardIndicators",
                type: "uuid",
                nullable: true);

            // Drop old unique index (StandardId, IndicatorId, TestMethodId)
            migrationBuilder.DropIndex(
                name: "IX_AppStandardIndicators_StandardId_IndicatorId_TestMethodId",
                table: "AppStandardIndicators");

            // Create index on SpecimenTypeId
            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_SpecimenTypeId",
                table: "AppStandardIndicators",
                column: "SpecimenTypeId");

            // Create new unique index (StandardId, IndicatorId, TestMethodId, SpecimenTypeId)
            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_StandardId_IndicatorId_TestMethodId_SpecimenTypeId",
                table: "AppStandardIndicators",
                columns: new[] { "StandardId", "IndicatorId", "TestMethodId", "SpecimenTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new unique index
            migrationBuilder.DropIndex(
                name: "IX_AppStandardIndicators_StandardId_IndicatorId_TestMethodId_SpecimenTypeId",
                table: "AppStandardIndicators");

            // Drop SpecimenTypeId index
            migrationBuilder.DropIndex(
                name: "IX_AppStandardIndicators_SpecimenTypeId",
                table: "AppStandardIndicators");

            // Recreate old unique index
            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_StandardId_IndicatorId_TestMethodId",
                table: "AppStandardIndicators",
                columns: new[] { "StandardId", "IndicatorId", "TestMethodId" },
                unique: true);

            // Drop SpecimenTypeId column
            migrationBuilder.DropColumn(
                name: "SpecimenTypeId",
                table: "AppStandardIndicators");
        }
    }
}
