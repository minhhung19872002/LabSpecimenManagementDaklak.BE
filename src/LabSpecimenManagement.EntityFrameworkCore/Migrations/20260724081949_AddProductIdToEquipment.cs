using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdToEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""AppWarehouseImportReceipts""
                    ALTER COLUMN ""GoodsReceiptId"" DROP NOT NULL;
                ALTER TABLE ""AppWarehouseImportReceipts""
                    ALTER COLUMN ""GoodsReceiptCode"" DROP NOT NULL;
                ALTER TABLE ""AppWarehouseImportReceipts""
                    ADD COLUMN IF NOT EXISTS ""ReceiptType"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""AppWarehouseImportReceiptItems""
                    ALTER COLUMN ""GoodsReceiptItemId"" DROP NOT NULL;
                ALTER TABLE ""AppPreparedSolutionBatches""
                    ADD COLUMN IF NOT EXISTS ""PreparedSolutionReceiptId"" uuid;
                ALTER TABLE ""AppEquipments""
                    ADD COLUMN IF NOT EXISTS ""ProductId"" uuid;
                CREATE INDEX IF NOT EXISTS ""IX_AppEquipments_ProductId"" ON ""AppEquipments"" (""ProductId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppEquipments_ProductId",
                table: "AppEquipments");

            migrationBuilder.DropColumn(
                name: "ReceiptType",
                table: "AppWarehouseImportReceipts");

            migrationBuilder.DropColumn(
                name: "PreparedSolutionReceiptId",
                table: "AppPreparedSolutionBatches");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "AppEquipments");

            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptId",
                table: "AppWarehouseImportReceipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
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
                name: "GoodsReceiptItemId",
                table: "AppWarehouseImportReceiptItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "PreparedSolutionReceiptId",
                table: "AppPreparedSolutionBatches");
        }
    }
}
