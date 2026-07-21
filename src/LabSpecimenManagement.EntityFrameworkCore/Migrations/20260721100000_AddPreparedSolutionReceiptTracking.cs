using System;
using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260721100000_AddPreparedSolutionReceiptTracking")]
    public partial class AddPreparedSolutionReceiptTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WarehouseImportReceipt: add ReceiptType, make GoodsReceiptId/Code nullable
            migrationBuilder.AddColumn<int>(
                name: "ReceiptType",
                table: "AppWarehouseImportReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptId",
                table: "AppWarehouseImportReceipts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "GoodsReceiptCode",
                table: "AppWarehouseImportReceipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            // WarehouseImportReceiptItem: make GoodsReceiptItemId nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptItemId",
                table: "AppWarehouseImportReceiptItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // PreparedSolutionBatch: add PreparedSolutionReceiptId
            migrationBuilder.AddColumn<Guid>(
                name: "PreparedSolutionReceiptId",
                table: "AppPreparedSolutionBatches",
                type: "uuid",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreparedSolutionReceiptId",
                table: "AppPreparedSolutionBatches");

            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptItemId",
                table: "AppWarehouseImportReceiptItems",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GoodsReceiptCode",
                table: "AppWarehouseImportReceipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptId",
                table: "AppWarehouseImportReceipts",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ReceiptType",
                table: "AppWarehouseImportReceipts");
        }
    }
}
