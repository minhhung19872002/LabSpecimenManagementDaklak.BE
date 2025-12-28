using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleStorageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SampleSentDate to TestRequests
            migrationBuilder.AddColumn<DateTime>(
                name: "SampleSentDate",
                table: "AppTestRequests",
                type: "timestamp with time zone",
                nullable: true);

            // Add IsCancelled to TestRequestSamples
            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "AppTestRequestSamples",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Add CancelledDate to TestRequestSamples
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledDate",
                table: "AppTestRequestSamples",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SampleSentDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "AppTestRequestSamples");

            migrationBuilder.DropColumn(
                name: "CancelledDate",
                table: "AppTestRequestSamples");
        }
    }
}
