using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStockInventoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InventoryDate",
                table: "AppStockAdjustments",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryUserId",
                table: "AppStockAdjustments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InventoryUserName",
                table: "AppStockAdjustments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActualUnitId",
                table: "AppStockAdjustmentItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualUnitName",
                table: "AppStockAdjustmentItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AppStockAdjustmentItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "AppStockAdjustmentItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseName",
                table: "AppStockAdjustmentItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppProductWarehouseInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastInventoryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductWarehouseInventories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppProductWarehouseInventories_ProductId",
                table: "AppProductWarehouseInventories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductWarehouseInventories_ProductId_WarehouseId",
                table: "AppProductWarehouseInventories",
                columns: new[] { "ProductId", "WarehouseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppProductWarehouseInventories");

            migrationBuilder.DropColumn(
                name: "InventoryDate",
                table: "AppStockAdjustments");

            migrationBuilder.DropColumn(
                name: "InventoryUserId",
                table: "AppStockAdjustments");

            migrationBuilder.DropColumn(
                name: "InventoryUserName",
                table: "AppStockAdjustments");

            migrationBuilder.DropColumn(
                name: "ActualUnitId",
                table: "AppStockAdjustmentItems");

            migrationBuilder.DropColumn(
                name: "ActualUnitName",
                table: "AppStockAdjustmentItems");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AppStockAdjustmentItems");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "AppStockAdjustmentItems");

            migrationBuilder.DropColumn(
                name: "WarehouseName",
                table: "AppStockAdjustmentItems");
        }
    }
}
