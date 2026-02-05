using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTestResultValueIndicatorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestResultValues_IndicatorId",
                table: "AppTestResultValues");

            migrationBuilder.DropColumn(
                name: "IndicatorId",
                table: "AppTestResultValues");

            migrationBuilder.CreateTable(
                name: "AppTestResultValueIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestResultValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecimenTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestResultValueIndicators", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValueIndicators_IndicatorId",
                table: "AppTestResultValueIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValueIndicators_SpecimenTypeId",
                table: "AppTestResultValueIndicators",
                column: "SpecimenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValueIndicators_TestResultValueId",
                table: "AppTestResultValueIndicators",
                column: "TestResultValueId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValueIndicators_TestResultValueId_IndicatorId_~",
                table: "AppTestResultValueIndicators",
                columns: new[] { "TestResultValueId", "IndicatorId", "SpecimenTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTestResultValueIndicators");

            migrationBuilder.AddColumn<Guid>(
                name: "IndicatorId",
                table: "AppTestResultValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValues_IndicatorId",
                table: "AppTestResultValues",
                column: "IndicatorId");
        }
    }
}
