using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityManagementEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppQualityManagementTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaskType = table.Column<int>(type: "integer", nullable: false),
                    TotalSampleCount = table.Column<int>(type: "integer", nullable: false),
                    TotalIndicatorCount = table.Column<int>(type: "integer", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RequestCompletionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResultSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChangeHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AppQualityManagementTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQualityManagementTasks_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppQualityManagementIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityManagementTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceLimit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQualityManagementIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQualityManagementIndicators_AppLaboratoryTaskIndicators_~",
                        column: x => x.LaboratoryTaskIndicatorId,
                        principalTable: "AppLaboratoryTaskIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppQualityManagementIndicators_AppQualityManagementTasks_Qu~",
                        column: x => x.QualityManagementTaskId,
                        principalTable: "AppQualityManagementTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppQualityIndicatorStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityManagementIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQualityIndicatorStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQualityIndicatorStatusHistories_AppQualityManagementIndi~",
                        column: x => x.QualityManagementIndicatorId,
                        principalTable: "AppQualityManagementIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityIndicatorStatusHistories_CreationTime",
                table: "AppQualityIndicatorStatusHistories",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityIndicatorStatusHistories_QualityManagementIndicat~",
                table: "AppQualityIndicatorStatusHistories",
                column: "QualityManagementIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_ApprovalStatus",
                table: "AppQualityManagementIndicators",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_LaboratoryTaskIndicatorId",
                table: "AppQualityManagementIndicators",
                column: "LaboratoryTaskIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_QualityManagementTaskId",
                table: "AppQualityManagementIndicators",
                column: "QualityManagementTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_StandardId",
                table: "AppQualityManagementIndicators",
                column: "StandardId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_ChangeHistoryId",
                table: "AppQualityManagementTasks",
                column: "ChangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_ContractNumber",
                table: "AppQualityManagementTasks",
                column: "ContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_RequestCompletionDate",
                table: "AppQualityManagementTasks",
                column: "RequestCompletionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_Status",
                table: "AppQualityManagementTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_TaskType",
                table: "AppQualityManagementTasks",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_TestRequestId",
                table: "AppQualityManagementTasks",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_TransferDate",
                table: "AppQualityManagementTasks",
                column: "TransferDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppQualityIndicatorStatusHistories");

            migrationBuilder.DropTable(
                name: "AppQualityManagementIndicators");

            migrationBuilder.DropTable(
                name: "AppQualityManagementTasks");
        }
    }
}
