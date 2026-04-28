using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialConsumptionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppMaterialConsumptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMaterialConsumptionItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialConsumptionItems_ProductId",
                table: "AppMaterialConsumptionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialConsumptionItems_TestMethodIndicatorId",
                table: "AppMaterialConsumptionItems",
                column: "TestMethodIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialConsumptionItems_TestMethodIndicatorId_ProductId",
                table: "AppMaterialConsumptionItems",
                columns: new[] { "TestMethodIndicatorId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMaterialConsumptionItems");
        }
    }
}
