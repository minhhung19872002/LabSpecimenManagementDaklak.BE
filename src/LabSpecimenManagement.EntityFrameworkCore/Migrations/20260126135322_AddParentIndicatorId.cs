using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddParentIndicatorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentIndicatorId",
                table: "AppIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_ParentIndicatorId",
                table: "AppIndicators",
                column: "ParentIndicatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppIndicators_ParentIndicatorId",
                table: "AppIndicators");

            migrationBuilder.DropColumn(
                name: "ParentIndicatorId",
                table: "AppIndicators");
        }
    }
}
