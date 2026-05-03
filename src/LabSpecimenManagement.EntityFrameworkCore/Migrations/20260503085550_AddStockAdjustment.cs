using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStockAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppStockAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppStockAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStockAdjustmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AdjustmentQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStockAdjustmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStockAdjustmentItems_AppStockAdjustments_StockAdjustment~",
                        column: x => x.StockAdjustmentId,
                        principalTable: "AppStockAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustmentItems_ProductId",
                table: "AppStockAdjustmentItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustmentItems_StockAdjustmentId",
                table: "AppStockAdjustmentItems",
                column: "StockAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustments_Code",
                table: "AppStockAdjustments",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustments_CreationTime",
                table: "AppStockAdjustments",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppStockAdjustments_Status",
                table: "AppStockAdjustments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppStockAdjustmentItems");

            migrationBuilder.DropTable(
                name: "AppStockAdjustments");
        }
    }
}
