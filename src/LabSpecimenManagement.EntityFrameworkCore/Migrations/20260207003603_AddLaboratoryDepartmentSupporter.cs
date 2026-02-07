using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoryDepartmentSupporter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLaboratoryDepartmentSupporters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLaboratoryDepartmentSupporters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryDepartmentSupporters_ExpirationDate",
                table: "AppLaboratoryDepartmentSupporters",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryDepartmentSupporters_LaboratoryId",
                table: "AppLaboratoryDepartmentSupporters",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryDepartmentSupporters_LaboratoryId_UserId",
                table: "AppLaboratoryDepartmentSupporters",
                columns: new[] { "LaboratoryId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryDepartmentSupporters_UserId",
                table: "AppLaboratoryDepartmentSupporters",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLaboratoryDepartmentSupporters");
        }
    }
}
