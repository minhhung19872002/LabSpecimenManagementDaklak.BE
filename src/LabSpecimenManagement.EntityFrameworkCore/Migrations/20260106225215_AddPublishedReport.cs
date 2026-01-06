using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppPublishedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityManagementTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReportType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PrintNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IndicatorIds = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_AppPublishedReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPublishedReports_AppQualityManagementTasks_QualityManage~",
                        column: x => x.QualityManagementTaskId,
                        principalTable: "AppQualityManagementTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppPublishedReports_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppPublishedReports_CreationTime",
                table: "AppPublishedReports",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppPublishedReports_QualityManagementTaskId",
                table: "AppPublishedReports",
                column: "QualityManagementTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPublishedReports_TestRequestId",
                table: "AppPublishedReports",
                column: "TestRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPublishedReports");
        }
    }
}
