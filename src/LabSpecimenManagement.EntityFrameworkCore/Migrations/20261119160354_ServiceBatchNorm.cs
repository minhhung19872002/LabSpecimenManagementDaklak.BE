using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class ServiceBatchNorm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppMethodBatchNorms_TestMethodId_ProductId",
                table: "AppMethodBatchNorms");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceBatchNormId",
                table: "AppMethodBatchNorms",
                type: "uuid",
                nullable: true);

            // Định mức chuyển từ theo phương pháp sang theo dịch vụ (nền mẫu × chỉ tiêu × PP):
            // bỏ dữ liệu cũ (BA chốt), xóa mềm vì IndicatorConsumption còn tham chiếu MethodBatchNormId
            migrationBuilder.Sql(@"
UPDATE ""AppMethodBatchNorms""
SET ""IsDeleted"" = true, ""DeletionTime"" = now()
WHERE ""IsDeleted"" = false;");

            migrationBuilder.CreateTable(
                name: "AppServiceBatchNorms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_AppServiceBatchNorms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServiceBatchNorms_AppTestMethodIndicators_TestMethodIndi~",
                        column: x => x.TestMethodIndicatorId,
                        principalTable: "AppTestMethodIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodBatchNorms_ServiceBatchNormId",
                table: "AppMethodBatchNorms",
                column: "ServiceBatchNormId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodBatchNorms_ServiceBatchNormId_ProductId",
                table: "AppMethodBatchNorms",
                columns: new[] { "ServiceBatchNormId", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"ProductId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceBatchNorms_TestMethodIndicatorId",
                table: "AppServiceBatchNorms",
                column: "TestMethodIndicatorId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_AppMethodBatchNorms_AppServiceBatchNorms_ServiceBatchNormId",
                table: "AppMethodBatchNorms",
                column: "ServiceBatchNormId",
                principalTable: "AppServiceBatchNorms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppMethodBatchNorms_AppServiceBatchNorms_ServiceBatchNormId",
                table: "AppMethodBatchNorms");

            migrationBuilder.DropTable(
                name: "AppServiceBatchNorms");

            migrationBuilder.DropIndex(
                name: "IX_AppMethodBatchNorms_ServiceBatchNormId",
                table: "AppMethodBatchNorms");

            migrationBuilder.DropIndex(
                name: "IX_AppMethodBatchNorms_ServiceBatchNormId_ProductId",
                table: "AppMethodBatchNorms");

            migrationBuilder.DropColumn(
                name: "ServiceBatchNormId",
                table: "AppMethodBatchNorms");

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodBatchNorms_TestMethodId_ProductId",
                table: "AppMethodBatchNorms",
                columns: new[] { "TestMethodId", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"ProductId\" IS NOT NULL");
        }
    }
}
