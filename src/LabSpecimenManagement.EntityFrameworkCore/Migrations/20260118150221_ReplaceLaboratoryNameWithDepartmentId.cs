using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceLaboratoryNameWithDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppLaboratories_Name",
                table: "AppLaboratories");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AppLaboratories");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "AppLaboratories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratories_DepartmentId",
                table: "AppLaboratories",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppLaboratories_DepartmentId",
                table: "AppLaboratories");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AppLaboratories");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AppLaboratories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratories_Name",
                table: "AppLaboratories",
                column: "Name",
                unique: true);
        }
    }
}
