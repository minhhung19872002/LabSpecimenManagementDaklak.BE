using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseImportItemPricingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalLineAmount",
                table: "AppWarehouseImportReceiptItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "AppWarehouseImportReceiptItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VatPercent",
                table: "AppWarehouseImportReceiptItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalLineAmount",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "AppWarehouseImportReceiptItems");

            migrationBuilder.DropColumn(
                name: "VatPercent",
                table: "AppWarehouseImportReceiptItems");
        }
    }
}
