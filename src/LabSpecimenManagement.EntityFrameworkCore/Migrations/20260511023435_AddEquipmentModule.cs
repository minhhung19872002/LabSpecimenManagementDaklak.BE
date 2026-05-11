using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEquipment",
                table: "AppGoodsReceiptItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "AppGoodsReceiptItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppEquipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Origin = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LotExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppEquipments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppEquipments_Code",
                table: "AppEquipments",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_AppEquipments_CreationTime",
                table: "AppEquipments",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppEquipments_GoodsReceiptId",
                table: "AppEquipments",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppEquipments_SerialNumber",
                table: "AppEquipments",
                column: "SerialNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppEquipments_Status",
                table: "AppEquipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppEquipments_SupplierId",
                table: "AppEquipments",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "IsEquipment",
                table: "AppGoodsReceiptItems");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "AppGoodsReceiptItems");
        }
    }
}
