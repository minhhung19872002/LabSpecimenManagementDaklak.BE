using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260714080000_AddSupplementaryTaskCompletion")]
    public partial class AddSupplementaryTaskCompletion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletionNote",
                table: "AppSupplementaryTasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionImageUrls",
                table: "AppSupplementaryTasks",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CompletionNote", table: "AppSupplementaryTasks");
            migrationBuilder.DropColumn(name: "CompletionImageUrls", table: "AppSupplementaryTasks");
        }
    }
}
