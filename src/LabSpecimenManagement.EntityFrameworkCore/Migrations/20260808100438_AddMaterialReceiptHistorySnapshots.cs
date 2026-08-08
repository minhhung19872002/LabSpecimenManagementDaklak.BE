using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialReceiptHistorySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarcodeValues",
                table: "AppWarehouseImportReceiptItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockAfterBaseQuantity",
                table: "AppWarehouseImportReceiptItems",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockBeforeBaseQuantity",
                table: "AppWarehouseImportReceiptItems",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppInventoryBarcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginWarehouseImportReceiptItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastMaterialReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInventoryBarcodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppInventoryBarcodes_Code",
                table: "AppInventoryBarcodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppInventoryBarcodes_CurrentWarehouseId",
                table: "AppInventoryBarcodes",
                column: "CurrentWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppInventoryBarcodes_LastMaterialReceiptId",
                table: "AppInventoryBarcodes",
                column: "LastMaterialReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppInventoryBarcodes_OriginWarehouseImportReceiptItemId",
                table: "AppInventoryBarcodes",
                column: "OriginWarehouseImportReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppInventoryBarcodes_ProductId",
                table: "AppInventoryBarcodes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppInventoryBarcodes_SkuId",
                table: "AppInventoryBarcodes",
                column: "SkuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppInventoryBarcodes");

            migrationBuilder.DropColumn(
                name: "BarcodeValues",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "StockAfterBaseQuantity",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "StockBeforeBaseQuantity",
                table: "AppWarehouseImportReceiptItems");
        }
    }
}
