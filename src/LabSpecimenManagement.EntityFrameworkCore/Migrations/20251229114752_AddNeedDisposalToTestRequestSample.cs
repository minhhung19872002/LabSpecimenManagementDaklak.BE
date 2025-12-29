using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedDisposalToTestRequestSample : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedDisposal",
                table: "AppTestRequestSamples",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NeedDisposalDate",
                table: "AppTestRequestSamples",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedDisposal",
                table: "AppTestRequestSamples");

            migrationBuilder.DropColumn(
                name: "NeedDisposalDate",
                table: "AppTestRequestSamples");
        }
    }
}
