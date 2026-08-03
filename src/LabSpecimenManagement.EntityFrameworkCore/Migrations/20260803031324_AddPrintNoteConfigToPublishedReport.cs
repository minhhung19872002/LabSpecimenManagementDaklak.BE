using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintNoteConfigToPublishedReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FreeTextNote",
                table: "AppPublishedReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedMasterNoteIds",
                table: "AppPublishedReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VicePresidentId",
                table: "AppPublishedReports",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeTextNote",
                table: "AppPublishedReports");

            migrationBuilder.DropColumn(
                name: "SelectedMasterNoteIds",
                table: "AppPublishedReports");

            migrationBuilder.DropColumn(
                name: "VicePresidentId",
                table: "AppPublishedReports");
        }
    }
}
