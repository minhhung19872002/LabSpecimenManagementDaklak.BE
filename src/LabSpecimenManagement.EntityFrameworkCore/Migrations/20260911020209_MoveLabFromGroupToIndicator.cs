using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class MoveLabFromGroupToIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add LaboratoryId column to Indicators (nullable first for data migration)
            migrationBuilder.AddColumn<Guid>(
                name: "LaboratoryId",
                table: "AppIndicators",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // 2. Copy LaboratoryId from IndicatorGroup to Indicator
            migrationBuilder.Sql(@"
                UPDATE ""AppIndicators"" i
                SET ""LaboratoryId"" = g.""LaboratoryId""
                FROM ""AppIndicatorGroups"" g
                WHERE i.""IndicatorGroupId"" = g.""Id""
                  AND g.""LaboratoryId"" IS NOT NULL;
            ");

            // 3. Create index on new column
            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_LaboratoryId",
                table: "AppIndicators",
                column: "LaboratoryId");

            // 4. Drop LaboratoryId from IndicatorGroups
            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "AppIndicatorGroups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Re-add LaboratoryId to IndicatorGroups
            migrationBuilder.AddColumn<Guid>(
                name: "LaboratoryId",
                table: "AppIndicatorGroups",
                type: "uuid",
                nullable: true);

            // 2. Copy LaboratoryId back from first indicator in each group
            migrationBuilder.Sql(@"
                UPDATE ""AppIndicatorGroups"" g
                SET ""LaboratoryId"" = sub.""LaboratoryId""
                FROM (
                    SELECT DISTINCT ON (""IndicatorGroupId"") ""IndicatorGroupId"", ""LaboratoryId""
                    FROM ""AppIndicators""
                    WHERE ""LaboratoryId"" != '00000000-0000-0000-0000-000000000000'
                    ORDER BY ""IndicatorGroupId"", ""CreationTime""
                ) sub
                WHERE g.""Id"" = sub.""IndicatorGroupId"";
            ");

            // 3. Drop index and column from Indicators
            migrationBuilder.DropIndex(
                name: "IX_AppIndicators_LaboratoryId",
                table: "AppIndicators");

            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "AppIndicators");
        }
    }
}
