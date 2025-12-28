using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoryTaskSupporter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLaboratoryTaskSupporters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLaboratoryTaskSupporters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskSupporters_AppLaboratoryTasks_LaboratoryTa~",
                        column: x => x.LaboratoryTaskId,
                        principalTable: "AppLaboratoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSupporters_LaboratoryTaskId",
                table: "AppLaboratoryTaskSupporters",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSupporters_UserId",
                table: "AppLaboratoryTaskSupporters",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLaboratoryTaskSupporters");
        }
    }
}
