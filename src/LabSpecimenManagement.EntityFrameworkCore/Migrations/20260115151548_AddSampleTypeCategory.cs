using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleTypeCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SampleTypeCategoryId",
                table: "AppSpecimenTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppSampleTypeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSampleTypeCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypes_SampleTypeCategoryId",
                table: "AppSpecimenTypes",
                column: "SampleTypeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSampleTypeCategories_IsActive",
                table: "AppSampleTypeCategories",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_AppSpecimenTypes_AppSampleTypeCategories_SampleTypeCategory~",
                table: "AppSpecimenTypes",
                column: "SampleTypeCategoryId",
                principalTable: "AppSampleTypeCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppSpecimenTypes_AppSampleTypeCategories_SampleTypeCategory~",
                table: "AppSpecimenTypes");

            migrationBuilder.DropTable(
                name: "AppSampleTypeCategories");

            migrationBuilder.DropIndex(
                name: "IX_AppSpecimenTypes_SampleTypeCategoryId",
                table: "AppSpecimenTypes");

            migrationBuilder.DropColumn(
                name: "SampleTypeCategoryId",
                table: "AppSpecimenTypes");
        }
    }
}
