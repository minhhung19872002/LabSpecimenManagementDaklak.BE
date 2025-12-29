using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalData",
                table: "AppNotifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadlineDate",
                table: "AppNotifications",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemCount",
                table: "AppNotifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "AppNotifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalData",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "DeadlineDate",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "ItemCount",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "AppNotifications");
        }
    }
}
