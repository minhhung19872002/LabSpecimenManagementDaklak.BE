using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecimenHistoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Temperature column
            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "AppSpecimenHistories",
                type: "numeric",
                nullable: true);

            // Add Humidity column
            migrationBuilder.AddColumn<decimal>(
                name: "Humidity",
                table: "AppSpecimenHistories",
                type: "numeric",
                nullable: true);

            // Add Timestamp column
            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "AppSpecimenHistories",
                type: "timestamp without time zone",
                nullable: true);

            // Make Notes nullable (if not already)
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "AppSpecimenHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "AppSpecimenHistories");

            migrationBuilder.DropColumn(
                name: "Humidity",
                table: "AppSpecimenHistories");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "AppSpecimenHistories");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "AppSpecimenHistories",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
