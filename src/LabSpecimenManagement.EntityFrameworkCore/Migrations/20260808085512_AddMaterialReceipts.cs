using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppMaterialReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceiverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SyntheticWarehouseReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AppMaterialReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppMaterialReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivingWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivingWarehouseName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMaterialReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMaterialReceiptItems_AppMaterialReceipts_MaterialReceipt~",
                        column: x => x.MaterialReceiptId,
                        principalTable: "AppMaterialReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialReceiptItems_MaterialReceiptId",
                table: "AppMaterialReceiptItems",
                column: "MaterialReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialReceiptItems_ProductId",
                table: "AppMaterialReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialReceiptItems_ReceivingWarehouseId",
                table: "AppMaterialReceiptItems",
                column: "ReceivingWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialReceipts_Code",
                table: "AppMaterialReceipts",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialReceipts_ReceiptDate",
                table: "AppMaterialReceipts",
                column: "ReceiptDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialReceipts_Status",
                table: "AppMaterialReceipts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMaterialReceiptItems");

            migrationBuilder.DropTable(
                name: "AppMaterialReceipts");
        }
    }
}
