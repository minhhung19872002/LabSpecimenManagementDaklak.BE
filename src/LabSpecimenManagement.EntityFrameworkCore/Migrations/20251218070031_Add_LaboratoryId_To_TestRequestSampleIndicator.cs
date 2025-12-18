using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class Add_LaboratoryId_To_TestRequestSampleIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LaboratoryId",
                table: "AppTestRequestSampleIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_LaboratoryId",
                table: "AppTestRequestSampleIndicators",
                column: "LaboratoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTestRequestSampleIndicators_AppLaboratories_LaboratoryId",
                table: "AppTestRequestSampleIndicators",
                column: "LaboratoryId",
                principalTable: "AppLaboratories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTestRequestSampleIndicators_AppLaboratories_LaboratoryId",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppTestRequestSampleIndicators_LaboratoryId",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "AppTestRequestSampleIndicators");
        }
    }
}
