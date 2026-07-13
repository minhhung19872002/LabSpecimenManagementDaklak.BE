using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260712130000_AddPreparedSolutionConcentration")]
    public partial class AddPreparedSolutionConcentration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Concentration",
                table: "AppPreparedSolutionBatches",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Concentration",
                table: "AppPreparedSolutionBatches");
        }
    }
}
