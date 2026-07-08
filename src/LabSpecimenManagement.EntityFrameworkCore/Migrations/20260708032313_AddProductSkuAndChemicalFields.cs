using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSkuAndChemicalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxStockLevel",
                table: "AppProducts");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStockLevel",
                table: "AppProducts",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChemicalFormula",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppProductSkus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackagingQuantity = table.Column<int>(type: "integer", nullable: true),
                    PackagingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    VolumePerUnit = table.Column<int>(type: "integer", nullable: true),
                    VolumeUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductSkus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppProductSkus_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppProductSkus_ProductId",
                table: "AppProductSkus",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductSkus_ProductId_ManufacturerId",
                table: "AppProductSkus",
                columns: new[] { "ProductId", "ManufacturerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppProductSkus");

            migrationBuilder.DropColumn(
                name: "ChemicalFormula",
                table: "AppProducts");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStockLevel",
                table: "AppProducts",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxStockLevel",
                table: "AppProducts",
                type: "numeric",
                nullable: true);
        }
    }
}
