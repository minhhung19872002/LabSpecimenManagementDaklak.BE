using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStatusToTestRequestSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "AppTestRequestSamples",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusText",
                table: "AppTestRequestSamples",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "AppTestRequestSampleIndicators",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SentToDepartment",
                table: "AppTestRequestSampleIndicators",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransferDate",
                table: "AppTestRequestSampleIndicators",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "AppTestRequestSamples");

            migrationBuilder.DropColumn(
                name: "StatusText",
                table: "AppTestRequestSamples");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "SentToDepartment",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TransferDate",
                table: "AppTestRequestSampleIndicators");
        }
    }
}
