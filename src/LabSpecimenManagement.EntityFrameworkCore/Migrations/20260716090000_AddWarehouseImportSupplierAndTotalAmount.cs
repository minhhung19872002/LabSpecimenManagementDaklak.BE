using System;
using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260716090000_AddWarehouseImportSupplierAndTotalAmount")]
    public partial class AddWarehouseImportSupplierAndTotalAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "AppWarehouseImportReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "AppWarehouseImportReceipts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "AppWarehouseImportReceipts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "AppWarehouseImportReceipts");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "AppWarehouseImportReceipts");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "AppWarehouseImportReceipts");
        }
    }
}
