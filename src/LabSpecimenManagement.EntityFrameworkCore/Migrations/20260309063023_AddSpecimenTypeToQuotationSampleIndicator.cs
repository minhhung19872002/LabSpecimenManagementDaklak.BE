using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecimenTypeToQuotationSampleIndicator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SpecimenTypeId",
                table: "AppQuotationSampleIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecimenTypeName",
                table: "AppQuotationSampleIndicators",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationSampleIndicators_SpecimenTypeId",
                table: "AppQuotationSampleIndicators",
                column: "SpecimenTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppQuotationSampleIndicators_AppSpecimenTypes_SpecimenTypeId",
                table: "AppQuotationSampleIndicators",
                column: "SpecimenTypeId",
                principalTable: "AppSpecimenTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppQuotationSampleIndicators_AppSpecimenTypes_SpecimenTypeId",
                table: "AppQuotationSampleIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppQuotationSampleIndicators_SpecimenTypeId",
                table: "AppQuotationSampleIndicators");

            migrationBuilder.DropColumn(
                name: "SpecimenTypeId",
                table: "AppQuotationSampleIndicators");

            migrationBuilder.DropColumn(
                name: "SpecimenTypeName",
                table: "AppQuotationSampleIndicators");
        }
    }
}
