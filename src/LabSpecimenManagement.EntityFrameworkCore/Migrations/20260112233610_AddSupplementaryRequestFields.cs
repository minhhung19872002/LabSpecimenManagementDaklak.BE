using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplementaryRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SupplementaryRequestDate",
                table: "AppLaboratoryTaskIndicators",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplementaryRequestReason",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplementaryRequestedById",
                table: "AppLaboratoryTaskIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplementaryRequestedByName",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplementaryRequestDate",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "SupplementaryRequestReason",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "SupplementaryRequestedById",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "SupplementaryRequestedByName",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
