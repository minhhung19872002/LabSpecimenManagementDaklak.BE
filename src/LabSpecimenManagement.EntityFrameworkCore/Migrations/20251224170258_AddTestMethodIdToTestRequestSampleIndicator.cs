using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTestMethodIdToTestRequestSampleIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TestMethodId",
                table: "AppTestRequestSampleIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_TestMethodId",
                table: "AppTestRequestSampleIndicators",
                column: "TestMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTestRequestSampleIndicators_AppTestMethods_TestMethodId",
                table: "AppTestRequestSampleIndicators",
                column: "TestMethodId",
                principalTable: "AppTestMethods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTestRequestSampleIndicators_AppTestMethods_TestMethodId",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppTestRequestSampleIndicators_TestMethodId",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TestMethodId",
                table: "AppTestRequestSampleIndicators");
        }
    }
}
