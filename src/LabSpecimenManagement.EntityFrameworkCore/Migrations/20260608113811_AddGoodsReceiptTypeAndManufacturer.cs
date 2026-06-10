using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptTypeAndManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SupplierName",
                table: "AppGoodsReceipts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "AppGoodsReceipts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "ReceiptType",
                table: "AppGoodsReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                table: "AppGoodsReceiptItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "AppGoodsReceiptItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "AppGoodsReceiptItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppManufacturers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppManufacturers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppGoodsReceipts_ReceiptType",
                table: "AppGoodsReceipts",
                column: "ReceiptType");

            migrationBuilder.CreateIndex(
                name: "IX_AppManufacturers_Code",
                table: "AppManufacturers",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppManufacturers_Name",
                table: "AppManufacturers",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppManufacturers");

            migrationBuilder.DropIndex(
                name: "IX_AppGoodsReceipts_ReceiptType",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ReceiptType",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "AppGoodsReceiptItems");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "AppGoodsReceiptItems");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "AppGoodsReceiptItems");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierName",
                table: "AppGoodsReceipts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "AppGoodsReceipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
