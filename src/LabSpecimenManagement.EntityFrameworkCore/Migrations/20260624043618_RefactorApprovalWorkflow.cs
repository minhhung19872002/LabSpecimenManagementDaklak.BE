using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class RefactorApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppApprovalLogs");

            migrationBuilder.DropColumn(
                name: "Approver1Id",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "Approver1Name",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "Approver2Id",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "Approver2Name",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "CurrentApprovalLevel",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "Approver1Id",
                table: "AppApprovalProcesses");

            migrationBuilder.DropColumn(
                name: "Approver1Name",
                table: "AppApprovalProcesses");

            migrationBuilder.DropColumn(
                name: "Approver2Id",
                table: "AppApprovalProcesses");

            migrationBuilder.DropColumn(
                name: "Approver2Name",
                table: "AppApprovalProcesses");

            migrationBuilder.CreateTable(
                name: "AppApprovalProcessSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppApprovalProcessSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppApprovalProcessSteps_AppApprovalProcesses_ApprovalProces~",
                        column: x => x.ApprovalProcessId,
                        principalTable: "AppApprovalProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppApprovalSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessType = table.Column<int>(type: "integer", nullable: false),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    CurrentStepOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppApprovalSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppApprovalSnapshotSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppApprovalSnapshotSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppApprovalSnapshotSteps_AppApprovalSnapshots_ApprovalSnaps~",
                        column: x => x.ApprovalSnapshotId,
                        principalTable: "AppApprovalSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalProcessSteps_ApprovalProcessId",
                table: "AppApprovalProcessSteps",
                column: "ApprovalProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalSnapshots_ReferenceId",
                table: "AppApprovalSnapshots",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalSnapshots_ReferenceType",
                table: "AppApprovalSnapshots",
                column: "ReferenceType");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalSnapshotSteps_ApprovalSnapshotId",
                table: "AppApprovalSnapshotSteps",
                column: "ApprovalSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppApprovalProcessSteps");

            migrationBuilder.DropTable(
                name: "AppApprovalSnapshotSteps");

            migrationBuilder.DropTable(
                name: "AppApprovalSnapshots");

            migrationBuilder.AddColumn<Guid>(
                name: "Approver1Id",
                table: "AppGoodsReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Approver1Name",
                table: "AppGoodsReceipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Approver2Id",
                table: "AppGoodsReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Approver2Name",
                table: "AppGoodsReceipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentApprovalLevel",
                table: "AppGoodsReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Approver1Id",
                table: "AppApprovalProcesses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Approver1Name",
                table: "AppApprovalProcesses",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Approver2Id",
                table: "AppApprovalProcesses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Approver2Name",
                table: "AppApprovalProcesses",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppApprovalLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NextApproverName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppApprovalLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalLogs_ReferenceId",
                table: "AppApprovalLogs",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalLogs_ReferenceType",
                table: "AppApprovalLogs",
                column: "ReferenceType");
        }
    }
}
