using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardAndStandardIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppLaboratoryTaskIndicators_AssignedDate",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.CreateTable(
                name: "AppStandardIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ComparisonOperator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AllowedLimit = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStandardIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStandards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LimitDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppStandards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorRejectionHistories_RejectedDate",
                table: "AppIndicatorRejectionHistories",
                column: "RejectedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_IndicatorId",
                table: "AppStandardIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_StandardId",
                table: "AppStandardIndicators",
                column: "StandardId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_StandardId_IndicatorId_TestMethodId",
                table: "AppStandardIndicators",
                columns: new[] { "StandardId", "IndicatorId", "TestMethodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_TestMethodId",
                table: "AppStandardIndicators",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandards_Code",
                table: "AppStandards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStandards_IsActive",
                table: "AppStandards",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppStandardIndicators");

            migrationBuilder.DropTable(
                name: "AppStandards");

            migrationBuilder.DropIndex(
                name: "IX_AppIndicatorRejectionHistories_RejectedDate",
                table: "AppIndicatorRejectionHistories");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_AssignedDate",
                table: "AppLaboratoryTaskIndicators",
                column: "AssignedDate");
        }
    }
}
