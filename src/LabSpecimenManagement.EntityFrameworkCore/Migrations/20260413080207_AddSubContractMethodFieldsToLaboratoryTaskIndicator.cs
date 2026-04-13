using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSubContractMethodFieldsToLaboratoryTaskIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SubContractCost",
                table: "AppLaboratoryTaskIndicators",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubContractMethodName",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubContractNumber",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubContractCost",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "SubContractMethodName",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "SubContractNumber",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
