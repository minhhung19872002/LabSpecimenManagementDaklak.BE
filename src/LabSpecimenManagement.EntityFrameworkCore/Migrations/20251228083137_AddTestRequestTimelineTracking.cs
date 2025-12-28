using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTestRequestTimelineTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InProgressByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InProgressByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InProgressDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingApprovalByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingApprovalDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResultSentByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultSentByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultSentDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SampleSentByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleSentByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "CompletedByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "InProgressByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "InProgressByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "InProgressDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "PendingApprovalByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "PendingApprovalByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "PendingApprovalDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "PendingByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "PendingByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "PendingDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ResultSentByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ResultSentByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ResultSentDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SampleSentByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SampleSentByUserName",
                table: "AppTestRequests");
        }
    }
}
