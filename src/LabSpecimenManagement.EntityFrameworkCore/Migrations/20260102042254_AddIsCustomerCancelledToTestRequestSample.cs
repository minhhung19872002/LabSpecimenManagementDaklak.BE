using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCustomerCancelledToTestRequestSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerCancelledDate",
                table: "AppTestRequestSamples",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomerCancelled",
                table: "AppTestRequestSamples",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerCancelledDate",
                table: "AppTestRequestSamples");

            migrationBuilder.DropColumn(
                name: "IsCustomerCancelled",
                table: "AppTestRequestSamples");
        }
    }
}
