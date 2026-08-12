using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryBarcodeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AppInventoryBarcodes",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateIndex(
                name: "IX_AppInventoryBarcodes_Status",
                table: "AppInventoryBarcodes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppInventoryBarcodes_Status",
                table: "AppInventoryBarcodes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppInventoryBarcodes");
        }
    }
}
