using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppProductAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductType = table.Column<int>(type: "integer", nullable: false),
                    BaseUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackagingSpec = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CASNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Origin = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Concentration = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StorageConditions = table.Column<string>(type: "text", nullable: true),
                    Categories = table.Column<string>(type: "text", nullable: true),
                    MinStockLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxStockLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    AvailableQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AppProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppProductUnitConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvertedQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ConvertedUnitId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductUnitConversions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttachments_FileId",
                table: "AppProductAttachments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductAttachments_ProductId",
                table: "AppProductAttachments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_BaseUnitId",
                table: "AppProducts",
                column: "BaseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_Code",
                table: "AppProducts",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_IsActive",
                table: "AppProducts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_Name",
                table: "AppProducts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AppProducts_ProductType",
                table: "AppProducts",
                column: "ProductType");

            migrationBuilder.CreateIndex(
                name: "IX_AppProductUnitConversions_ProductId",
                table: "AppProductUnitConversions",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppProductAttachments");

            migrationBuilder.DropTable(
                name: "AppProducts");

            migrationBuilder.DropTable(
                name: "AppProductUnitConversions");
        }
    }
}
