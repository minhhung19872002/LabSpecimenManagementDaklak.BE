using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260714090000_AddProductEquipmentFields")]
    public partial class AddProductEquipmentFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormattedIdentificationCode",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentModel",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManufactureYear",
                table: "AppProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainTechnicalSpecs",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstallationLocation",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FormattedIdentificationCode", table: "AppProducts");
            migrationBuilder.DropColumn(name: "EquipmentModel", table: "AppProducts");
            migrationBuilder.DropColumn(name: "SerialNumber", table: "AppProducts");
            migrationBuilder.DropColumn(name: "ManufactureYear", table: "AppProducts");
            migrationBuilder.DropColumn(name: "MainTechnicalSpecs", table: "AppProducts");
            migrationBuilder.DropColumn(name: "InstallationLocation", table: "AppProducts");
        }
    }
}
