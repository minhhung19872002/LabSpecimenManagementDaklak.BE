using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentFieldsToWarehouseImportReceiptItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetCode",
                table: "AppWarehouseImportReceiptItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepreciationPeriod",
                table: "AppWarehouseImportReceiptItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquipmentIndex",
                table: "AppWarehouseImportReceiptItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EquipmentReceiveStatus",
                table: "AppWarehouseImportReceiptItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsEquipment",
                table: "AppWarehouseImportReceiptItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AppWarehouseImportReceiptItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                table: "AppWarehouseImportReceiptItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerName",
                table: "AppWarehouseImportReceiptItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "AppWarehouseImportReceiptItems",
                type: "numeric(18,0)",
                precision: 18,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "AppWarehouseImportReceiptItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetCode",
                table: "AppEquipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepreciationPeriod",
                table: "AppEquipments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AppEquipments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
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
                name: "DepreciationPeriod",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AppEquipments");
        }
    }
}
