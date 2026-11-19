using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class ServiceBatchNormCodeEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "AppServiceBatchNorms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Backfill mã DM-{năm}-{0001} cho định mức đã có (kể cả đã xóa mềm) theo thứ tự tạo
            migrationBuilder.Sql(@"
UPDATE ""AppServiceBatchNorms"" h
SET ""Code"" = x.code
FROM (
    SELECT ""Id"",
           'DM-' || EXTRACT(YEAR FROM ""CreationTime"")::int || '-' ||
           LPAD((ROW_NUMBER() OVER (PARTITION BY EXTRACT(YEAR FROM ""CreationTime"") ORDER BY ""CreationTime"", ""Id""))::text, 4, '0') AS code
    FROM ""AppServiceBatchNorms""
) x
WHERE h.""Id"" = x.""Id"";");

            migrationBuilder.CreateTable(
                name: "AppServiceBatchNormEquipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceBatchNormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacity = table.Column<decimal>(type: "numeric", nullable: false),
                    CapacityUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_AppServiceBatchNormEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServiceBatchNormEquipments_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceBatchNormEquipments_AppServiceBatchNorms_ServiceB~",
                        column: x => x.ServiceBatchNormId,
                        principalTable: "AppServiceBatchNorms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceBatchNorms_Code",
                table: "AppServiceBatchNorms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceBatchNormEquipments_ProductId",
                table: "AppServiceBatchNormEquipments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceBatchNormEquipments_ServiceBatchNormId_ProductId",
                table: "AppServiceBatchNormEquipments",
                columns: new[] { "ServiceBatchNormId", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppServiceBatchNormEquipments");

            migrationBuilder.DropIndex(
                name: "IX_AppServiceBatchNorms_Code",
                table: "AppServiceBatchNorms");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "AppServiceBatchNorms");
        }
    }
}
