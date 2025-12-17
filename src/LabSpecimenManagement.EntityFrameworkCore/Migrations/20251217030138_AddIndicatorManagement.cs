using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicatorManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppIndicatorGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("PK_AppIndicatorGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IndicatorGroupId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_AppIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppReportTestResults",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestResultId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReportTestResults", x => new { x.ReportId, x.TestResultId });
                    table.ForeignKey(
                        name: "FK_AppReportTestResults_AppReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "AppReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppReportTestResults_AppTestResults_TestResultId",
                        column: x => x.TestResultId,
                        principalTable: "AppTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestIndicators", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorGroups_Name",
                table: "AppIndicatorGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_Code",
                table: "AppIndicators",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_IndicatorGroupId",
                table: "AppIndicators",
                column: "IndicatorGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_IsActive",
                table: "AppIndicators",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppReportTestResults_ReportId",
                table: "AppReportTestResults",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReportTestResults_TestResultId",
                table: "AppReportTestResults",
                column: "TestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestIndicators_IndicatorId",
                table: "AppTestRequestIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestIndicators_TestRequestId",
                table: "AppTestRequestIndicators",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestIndicators_TestRequestId_IndicatorId",
                table: "AppTestRequestIndicators",
                columns: new[] { "TestRequestId", "IndicatorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppIndicatorGroups");

            migrationBuilder.DropTable(
                name: "AppIndicators");

            migrationBuilder.DropTable(
                name: "AppReportTestResults");

            migrationBuilder.DropTable(
                name: "AppTestRequestIndicators");
        }
    }
}
