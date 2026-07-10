using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260710160000_AddProductStandardFields")]
    public partial class AddProductStandardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StandardCertificationType",
                table: "AppProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedChemicalFormula",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertifiedValue",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedCertifiedValue",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementUncertainty",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedMeasurementUncertainty",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StandardLink",
                table: "AppProducts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalysisGrade",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandardCertificationType",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "FormattedChemicalFormula",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "CertifiedValue",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "FormattedCertifiedValue",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "MeasurementUncertainty",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "FormattedMeasurementUncertainty",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "StandardLink",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "AnalysisGrade",
                table: "AppProducts");
        }
    }
}
