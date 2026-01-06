using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowRedesign_MethodChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerConfirmationDate",
                table: "AppLaboratoryTaskIndicators",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerConfirmedById",
                table: "AppLaboratoryTaskIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerConfirmedByName",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TestingRejectedById",
                table: "AppLaboratoryTaskIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestingRejectedByName",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TestingRejectionDate",
                table: "AppLaboratoryTaskIndicators",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestingRejectionReason",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppMethodChangeHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldMethodName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NewMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewMethodName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.PrimaryKey("PK_AppMethodChangeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMethodChangeHistories_AppLaboratoryTaskIndicators_Indica~",
                        column: x => x.IndicatorId,
                        principalTable: "AppLaboratoryTaskIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodChangeHistories_IndicatorId",
                table: "AppMethodChangeHistories",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodChangeHistories_IsApproved",
                table: "AppMethodChangeHistories",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodChangeHistories_RequestedDate",
                table: "AppMethodChangeHistories",
                column: "RequestedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMethodChangeHistories");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmationDate",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmedById",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmedByName",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "TestingRejectedById",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "TestingRejectedByName",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "TestingRejectionDate",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "TestingRejectionReason",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
