using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialReceiptWarehouseImportLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialReceiptId",
                table: "AppWarehouseImportReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AppWarehouseImportReceipts" AS warehouse_receipt
                SET "MaterialReceiptId" = material_receipt."Id"
                FROM "AppMaterialReceipts" AS material_receipt
                WHERE material_receipt."SyntheticWarehouseReceiptId" = warehouse_receipt."Id"
                  AND warehouse_receipt."MaterialReceiptId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouseImportReceipts_MaterialReceiptId",
                table: "AppWarehouseImportReceipts",
                column: "MaterialReceiptId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppWarehouseImportReceipts_AppMaterialReceipts_MaterialRece~",
                table: "AppWarehouseImportReceipts",
                column: "MaterialReceiptId",
                principalTable: "AppMaterialReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppWarehouseImportReceipts_AppMaterialReceipts_MaterialRece~",
                table: "AppWarehouseImportReceipts");

            migrationBuilder.DropIndex(
                name: "IX_AppWarehouseImportReceipts_MaterialReceiptId",
                table: "AppWarehouseImportReceipts");

            migrationBuilder.DropColumn(
                name: "MaterialReceiptId",
                table: "AppWarehouseImportReceipts");
        }
    }
}
