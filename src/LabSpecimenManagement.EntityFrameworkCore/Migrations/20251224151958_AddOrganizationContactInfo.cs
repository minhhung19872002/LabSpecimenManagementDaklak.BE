using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationContactInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "AppOrganizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "AppOrganizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AppOrganizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProvinceCode",
                table: "AppOrganizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "AppOrganizations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WardCode",
                table: "AppOrganizations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "AppOrganizations");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "AppOrganizations");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AppOrganizations");

            migrationBuilder.DropColumn(
                name: "ProvinceCode",
                table: "AppOrganizations");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "AppOrganizations");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "AppOrganizations");
        }
    }
}
