using System;
using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260716091000_AddWarehouseImportItemSkuAndWarehouse")]
    public partial class AddWarehouseImportItemSkuAndWarehouse : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SkuId",
                table: "AppWarehouseImportReceiptItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StorageWarehouseId",
                table: "AppWarehouseImportReceiptItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageWarehouseName",
                table: "AppWarehouseImportReceiptItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkuId",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "StorageWarehouseId",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "StorageWarehouseName",
                table: "AppWarehouseImportReceiptItems");
        }
    }
}
