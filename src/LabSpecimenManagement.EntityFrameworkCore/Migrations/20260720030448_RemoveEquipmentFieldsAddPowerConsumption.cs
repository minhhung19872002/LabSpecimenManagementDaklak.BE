using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEquipmentFieldsAddPowerConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormattedIdentificationCode",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "AppProducts");

            migrationBuilder.AddColumn<decimal>(
                name: "PowerConsumption",
                table: "AppProducts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerConsumption",
                table: "AppProducts");

            migrationBuilder.AddColumn<string>(
                name: "FormattedIdentificationCode",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
