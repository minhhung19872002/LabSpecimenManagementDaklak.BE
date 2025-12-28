using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmitDateAndSampleTestingImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SubmitDate to LaboratoryTaskIndicators
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmitDate",
                table: "AppLaboratoryTaskIndicators",
                type: "timestamp without time zone",
                nullable: true);

            // Create SampleTestingImages table
            migrationBuilder.CreateTable(
                name: "AppSampleTestingImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ImageType = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSampleTestingImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSampleTestingImages_AppLaboratoryTasks_LaboratoryTaskId",
                        column: x => x.LaboratoryTaskId,
                        principalTable: "AppLaboratoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSampleTestingImages_LaboratoryTaskId",
                table: "AppSampleTestingImages",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSampleTestingImages_SampleId",
                table: "AppSampleTestingImages",
                column: "SampleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSampleTestingImages");

            migrationBuilder.DropColumn(
                name: "SubmitDate",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
