using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoryTaskIndicatorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to LaboratoryTaskIndicators table
            migrationBuilder.AddColumn<Guid>(
                name: "MethodId",
                table: "AppLaboratoryTaskIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicianId",
                table: "AppLaboratoryTaskIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntryDate",
                table: "AppLaboratoryTaskIndicators",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnteredById",
                table: "AppLaboratoryTaskIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "AppLaboratoryTaskIndicators",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDate",
                table: "AppLaboratoryTaskIndicators",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "AppLaboratoryTaskIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // Create indexes for new columns
            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_MethodId",
                table: "AppLaboratoryTaskIndicators",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_TechnicianId",
                table: "AppLaboratoryTaskIndicators",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_ApprovalStatus",
                table: "AppLaboratoryTaskIndicators",
                column: "ApprovalStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_AppLaboratoryTaskIndicators_MethodId",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppLaboratoryTaskIndicators_TechnicianId",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppLaboratoryTaskIndicators_ApprovalStatus",
                table: "AppLaboratoryTaskIndicators");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "MethodId",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "EntryDate",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "EnteredById",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
