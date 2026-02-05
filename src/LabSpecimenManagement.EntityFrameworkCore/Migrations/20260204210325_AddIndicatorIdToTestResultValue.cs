using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicatorIdToTestResultValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IndicatorId",
                table: "AppTestResultValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValues_IndicatorId",
                table: "AppTestResultValues",
                column: "IndicatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestResultValues_IndicatorId",
                table: "AppTestResultValues");

            migrationBuilder.DropColumn(
                name: "IndicatorId",
                table: "AppTestResultValues");
        }
    }
}
