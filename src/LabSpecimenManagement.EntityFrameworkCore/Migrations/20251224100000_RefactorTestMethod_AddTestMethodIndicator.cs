using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTestMethod_AddTestMethodIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Unit column to AppTestMethods
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "AppTestMethods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Drop old columns from AppTestMethods
            migrationBuilder.DropColumn(
                name: "Description",
                table: "AppTestMethods");

            migrationBuilder.DropColumn(
                name: "Standard",
                table: "AppTestMethods");

            migrationBuilder.DropIndex(
                name: "IX_AppTestMethods_DepartmentId",
                table: "AppTestMethods");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AppTestMethods");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationHours",
                table: "AppTestMethods");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "AppTestMethods");

            // Add index on IsActive
            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_IsActive",
                table: "AppTestMethods",
                column: "IsActive");

            // Create AppTestMethodIndicators table (junction table for many-to-many)
            migrationBuilder.CreateTable(
                name: "AppTestMethodIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestMethodIndicators", x => x.Id);
                });

            // Create unique index on TestMethodId + IndicatorId
            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId",
                table: "AppTestMethodIndicators",
                columns: new[] { "TestMethodId", "IndicatorId" },
                unique: true);

            // Create index on IndicatorId
            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_IndicatorId",
                table: "AppTestMethodIndicators",
                column: "IndicatorId");

            // Create index on TestMethodId
            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId",
                table: "AppTestMethodIndicators",
                column: "TestMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop AppTestMethodIndicators table
            migrationBuilder.DropTable(
                name: "AppTestMethodIndicators");

            // Drop IsActive index
            migrationBuilder.DropIndex(
                name: "IX_AppTestMethods_IsActive",
                table: "AppTestMethods");

            // Restore old columns to AppTestMethods
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AppTestMethods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Standard",
                table: "AppTestMethods",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "AppTestMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_DepartmentId",
                table: "AppTestMethods",
                column: "DepartmentId");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationHours",
                table: "AppTestMethods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "AppTestMethods",
                type: "numeric",
                nullable: true);

            // Drop Unit column
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "AppTestMethods");
        }
    }
}
