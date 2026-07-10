using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260711160000_AddProductSupplyFields")]
    public partial class AddProductSupplyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PackagingSpec",
                table: "AppProducts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedPackagingSpec",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialComposition",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedMaterialComposition",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReusable",
                table: "AppProducts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxReuseCount",
                table: "AppProducts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormattedPackagingSpec",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "MaterialComposition",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "FormattedMaterialComposition",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "IsReusable",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "MaxReuseCount",
                table: "AppProducts");

            migrationBuilder.AlterColumn<string>(
                name: "PackagingSpec",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
