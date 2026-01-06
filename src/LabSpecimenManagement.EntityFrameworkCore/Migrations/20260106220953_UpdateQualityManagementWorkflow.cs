using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQualityManagementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResultSentDate",
                table: "AppQualityManagementTasks",
                newName: "HandoverDate");

            migrationBuilder.AddColumn<Guid>(
                name: "ReportPublishedByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportPublishedByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportPublishedDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HandoverByUserId",
                table: "AppQualityManagementTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverByUserName",
                table: "AppQualityManagementTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverNote",
                table: "AppQualityManagementTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrinted",
                table: "AppQualityManagementIndicators",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintedDate",
                table: "AppQualityManagementIndicators",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportPublishedByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ReportPublishedByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ReportPublishedDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "HandoverByUserId",
                table: "AppQualityManagementTasks");

            migrationBuilder.DropColumn(
                name: "HandoverByUserName",
                table: "AppQualityManagementTasks");

            migrationBuilder.DropColumn(
                name: "HandoverNote",
                table: "AppQualityManagementTasks");

            migrationBuilder.DropColumn(
                name: "IsPrinted",
                table: "AppQualityManagementIndicators");

            migrationBuilder.DropColumn(
                name: "PrintedDate",
                table: "AppQualityManagementIndicators");

            migrationBuilder.RenameColumn(
                name: "HandoverDate",
                table: "AppQualityManagementTasks",
                newName: "ResultSentDate");
        }
    }
}
