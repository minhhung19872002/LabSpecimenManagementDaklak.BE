using System;
using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260719063000_AddFifoLotTracking")]
    public partial class AddFifoLotTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedBaseQuantity",
                table: "AppWarehouseImportReceiptItems",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsumedBaseQuantity",
                table: "AppWarehouseImportReceiptItems",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "AppStockMovementLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "AppStockMovementLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseImportReceiptItemId",
                table: "AppStockMovementLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStockMovementLogs_WarehouseId",
                table: "AppStockMovementLogs",
                column: "WarehouseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppStockMovementLogs_WarehouseId",
                table: "AppStockMovementLogs");

            migrationBuilder.DropColumn(
                name: "ReceivedBaseQuantity",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "ConsumedBaseQuantity",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "AppStockMovementLogs");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "AppStockMovementLogs");

            migrationBuilder.DropColumn(
                name: "WarehouseImportReceiptItemId",
                table: "AppStockMovementLogs");
        }
    }
}
