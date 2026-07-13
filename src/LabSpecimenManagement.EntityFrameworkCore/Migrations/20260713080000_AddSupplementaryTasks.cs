using System;
using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260713080000_AddSupplementaryTasks")]
    public partial class AddSupplementaryTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSupplementaryTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ImageUrls = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSupplementaryTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSupplementaryTaskLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ChangedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSupplementaryTaskLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSupplementaryTasks_AssignedToId",
                table: "AppSupplementaryTasks",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSupplementaryTasks_DueDate",
                table: "AppSupplementaryTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppSupplementaryTasks_Status",
                table: "AppSupplementaryTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppSupplementaryTaskLogs_TaskId",
                table: "AppSupplementaryTaskLogs",
                column: "TaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_AppSupplementaryTaskLogs_TaskId", table: "AppSupplementaryTaskLogs");
            migrationBuilder.DropIndex(name: "IX_AppSupplementaryTasks_Status", table: "AppSupplementaryTasks");
            migrationBuilder.DropIndex(name: "IX_AppSupplementaryTasks_DueDate", table: "AppSupplementaryTasks");
            migrationBuilder.DropIndex(name: "IX_AppSupplementaryTasks_AssignedToId", table: "AppSupplementaryTasks");
            migrationBuilder.DropTable(name: "AppSupplementaryTaskLogs");
            migrationBuilder.DropTable(name: "AppSupplementaryTasks");
        }
    }
}
