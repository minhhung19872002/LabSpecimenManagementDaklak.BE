using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedSampleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSharedSample",
                table: "AppTestRequestSamples",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSharedSampleTask",
                table: "AppLaboratoryTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWaitingForPriorLab",
                table: "AppLaboratoryTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PriorLabTaskId",
                table: "AppLaboratoryTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleReceivedDate",
                table: "AppLaboratoryTasks",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleReceivedNote",
                table: "AppLaboratoryTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleSentDate",
                table: "AppLaboratoryTasks",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SampleSentToNextLab",
                table: "AppLaboratoryTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSharedSample",
                table: "AppTestRequestSamples");

            migrationBuilder.DropColumn(
                name: "IsSharedSampleTask",
                table: "AppLaboratoryTasks");

            migrationBuilder.DropColumn(
                name: "IsWaitingForPriorLab",
                table: "AppLaboratoryTasks");

            migrationBuilder.DropColumn(
                name: "PriorLabTaskId",
                table: "AppLaboratoryTasks");

            migrationBuilder.DropColumn(
                name: "SampleReceivedDate",
                table: "AppLaboratoryTasks");

            migrationBuilder.DropColumn(
                name: "SampleReceivedNote",
                table: "AppLaboratoryTasks");

            migrationBuilder.DropColumn(
                name: "SampleSentDate",
                table: "AppLaboratoryTasks");

            migrationBuilder.DropColumn(
                name: "SampleSentToNextLab",
                table: "AppLaboratoryTasks");
        }
    }
}
