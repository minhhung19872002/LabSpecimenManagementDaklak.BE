using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseImportReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppWarehouseImportReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActualImportDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    WarehouseKeeperId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseKeeperName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryDocumentFileId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DeliveryDocumentFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryDocumentFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryDocumentFileSize = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppWarehouseImportReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppWarehouseImportReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseImportReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualReceivedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LotExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    QualityDocumentFileId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QualityDocumentFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QualityDocumentFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QualityDocumentFileSize = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppWarehouseImportReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppWarehouseImportReceiptItems_AppWarehouseImportReceipts_W~",
                        column: x => x.WarehouseImportReceiptId,
                        principalTable: "AppWarehouseImportReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceiptItems_GoodsReceiptItemId",
                table: "AppWarehouseImportReceiptItems",
                column: "GoodsReceiptItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceiptItems_ProductId",
                table: "AppWarehouseImportReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceiptItems_WarehouseImportReceiptId",
                table: "AppWarehouseImportReceiptItems",
                column: "WarehouseImportReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceipts_Code",
                table: "AppWarehouseImportReceipts",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceipts_GoodsReceiptId",
                table: "AppWarehouseImportReceipts",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceipts_Status",
                table: "AppWarehouseImportReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceipts_WarehouseId",
                table: "AppWarehouseImportReceipts",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropTable(
                name: "AppWarehouseImportReceipts");
        }
    }
}
