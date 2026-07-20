using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentCostFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """DROP INDEX IF EXISTS "IX_AppGoodsReceipts_ReceiptType";""");
            migrationBuilder.Sql(
                """ALTER TABLE "AppGoodsReceipts" DROP COLUMN IF EXISTS "ReceiptType";""");

            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "AssetCode" character varying(100);""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "DepreciationPeriod" integer;""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "EquipmentIndex" integer NOT NULL DEFAULT 0;""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "EquipmentReceiveStatus" integer NOT NULL DEFAULT 0;""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "IsEquipment" boolean NOT NULL DEFAULT false;""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "Location" character varying(256);""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "ManufacturerId" uuid;""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "ManufacturerName" character varying(500);""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "OriginalPrice" numeric(18,0);""");
            migrationBuilder.Sql("""ALTER TABLE "AppWarehouseImportReceiptItems" ADD COLUMN IF NOT EXISTS "SerialNumber" character varying(200);""");

            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "AssetCode" character varying(100);""");
            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "AverageSamplesPerYear" integer;""");
            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "CalibrationCostPerYear" numeric(18,2);""");
            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "DepreciationPeriod" integer;""");
            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "Location" character varying(256);""");
            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "MaintenanceCostRatio" numeric(18,2);""");
            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "OperatingHoursPerYear" numeric(18,2);""");
            migrationBuilder.Sql("""ALTER TABLE "AppEquipments" ADD COLUMN IF NOT EXISTS "PowerConsumption" numeric(18,2);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetCode",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "DepreciationPeriod",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "EquipmentIndex",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "EquipmentReceiveStatus",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "IsEquipment",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "ManufacturerName",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "AssetCode",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "AverageSamplesPerYear",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "CalibrationCostPerYear",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "DepreciationPeriod",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "MaintenanceCostRatio",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "OperatingHoursPerYear",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "PowerConsumption",
                table: "AppEquipments");

            migrationBuilder.AddColumn<int>(
                name: "ReceiptType",
                table: "AppGoodsReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AppGoodsReceipts_ReceiptType",
                table: "AppGoodsReceipts",
                column: "ReceiptType");
        }
    }
}
