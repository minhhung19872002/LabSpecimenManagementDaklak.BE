using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRichTextFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormattedName",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormattedStorageConditions",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentificationCode",
                table: "AppProducts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormattedName",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "FormattedStorageConditions",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "IdentificationCode",
                table: "AppProducts");
        }
    }
}
