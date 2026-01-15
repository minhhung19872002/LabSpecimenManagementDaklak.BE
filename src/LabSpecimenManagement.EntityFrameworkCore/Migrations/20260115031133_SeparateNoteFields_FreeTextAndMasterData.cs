using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class SeparateNoteFields_FreeTextAndMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new columns
            migrationBuilder.AddColumn<string>(
                name: "FreeTextNote",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasterDataNoteIds",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            // Step 2: Migrate existing Note data to FreeTextNote
            migrationBuilder.Sql(
                "UPDATE \"AppLaboratoryTaskIndicators\" SET \"FreeTextNote\" = \"Note\" WHERE \"Note\" IS NOT NULL");

            // Step 3: Drop old column
            migrationBuilder.DropColumn(
                name: "Note",
                table: "AppLaboratoryTaskIndicators");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add back old column
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "AppLaboratoryTaskIndicators",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Step 2: Migrate FreeTextNote back to Note (truncate if longer than 500)
            migrationBuilder.Sql(
                "UPDATE \"AppLaboratoryTaskIndicators\" SET \"Note\" = LEFT(\"FreeTextNote\", 500) WHERE \"FreeTextNote\" IS NOT NULL");

            // Step 3: Drop new columns
            migrationBuilder.DropColumn(
                name: "FreeTextNote",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "MasterDataNoteIds",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
