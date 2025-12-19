using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoryTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLaboratoryTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalSampleCount = table.Column<int>(type: "integer", nullable: false),
                    TechnicalManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RequestCompletionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppLaboratoryTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTasks_AppLaboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "AppLaboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTasks_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppLaboratoryTaskIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSampleIndicatorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLaboratoryTaskIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskIndicators_AppLaboratoryTasks_LaboratoryTaskId",
                        column: x => x.LaboratoryTaskId,
                        principalTable: "AppLaboratoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskIndicators_AppTestRequestSampleIndicators_TestRequestSampleIndicatorId",
                        column: x => x.TestRequestSampleIndicatorId,
                        principalTable: "AppTestRequestSampleIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_LaboratoryId",
                table: "AppLaboratoryTasks",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_RequestCompletionDate",
                table: "AppLaboratoryTasks",
                column: "RequestCompletionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_Status",
                table: "AppLaboratoryTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_TechnicalManagerId",
                table: "AppLaboratoryTasks",
                column: "TechnicalManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_TestRequestId",
                table: "AppLaboratoryTasks",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_TransferDate",
                table: "AppLaboratoryTasks",
                column: "TransferDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_LaboratoryTaskId",
                table: "AppLaboratoryTaskIndicators",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_TestRequestSampleIndicatorId",
                table: "AppLaboratoryTaskIndicators",
                column: "TestRequestSampleIndicatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropTable(
                name: "AppLaboratoryTasks");
        }
    }
}
