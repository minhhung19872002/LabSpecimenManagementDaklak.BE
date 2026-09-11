using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeCodeToIndicatorConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarcodeCode",
                table: "AppIndicatorConsumptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseImportReceiptItemId",
                table: "AppIndicatorConsumptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorConsumptions_WarehouseImportReceiptItemId",
                table: "AppIndicatorConsumptions",
                column: "WarehouseImportReceiptItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppIndicatorConsumptions_WarehouseImportReceiptItemId",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "BarcodeCode",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "WarehouseImportReceiptItemId",
                table: "AppIndicatorConsumptions");
        }
    }
}
