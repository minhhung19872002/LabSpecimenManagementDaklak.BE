using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicatorConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppIndicatorConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecommendedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsDeducted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIndicatorConsumptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorConsumptions_EquipmentId",
                table: "AppIndicatorConsumptions",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorConsumptions_LaboratoryTaskIndicatorId",
                table: "AppIndicatorConsumptions",
                column: "LaboratoryTaskIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorConsumptions_ProductId",
                table: "AppIndicatorConsumptions",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppIndicatorConsumptions");
        }
    }
}
