using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecimenTypeIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSpecimenTypeIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecimenTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSpecimenTypeIndicators", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypeIndicators_IndicatorId",
                table: "AppSpecimenTypeIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypeIndicators_SpecimenTypeId",
                table: "AppSpecimenTypeIndicators",
                column: "SpecimenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypeIndicators_SpecimenTypeId_IndicatorId",
                table: "AppSpecimenTypeIndicators",
                columns: new[] { "SpecimenTypeId", "IndicatorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSpecimenTypeIndicators");
        }
    }
}
