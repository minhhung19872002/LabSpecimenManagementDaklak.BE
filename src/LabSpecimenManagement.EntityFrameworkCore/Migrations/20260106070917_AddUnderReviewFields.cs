using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddUnderReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnderReviewByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnderReviewByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnderReviewDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnderReviewByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "UnderReviewByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "UnderReviewDate",
                table: "AppTestRequests");
        }
    }
}
