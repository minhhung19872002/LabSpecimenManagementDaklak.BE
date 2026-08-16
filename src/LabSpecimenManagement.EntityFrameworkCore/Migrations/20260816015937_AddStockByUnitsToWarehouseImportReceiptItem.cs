using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStockByUnitsToWarehouseImportReceiptItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StockAfterByUnitsJson",
                table: "AppWarehouseImportReceiptItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockBeforeByUnitsJson",
                table: "AppWarehouseImportReceiptItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockAfterByUnitsJson",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "StockBeforeByUnitsJson",
                table: "AppWarehouseImportReceiptItems");
        }
    }
}
