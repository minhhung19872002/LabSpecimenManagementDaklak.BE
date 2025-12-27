using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTestRequestSampleAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTestRequestSampleAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestSampleAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleAttachments_AppTestRequestSamples_TestR~",
                        column: x => x.TestRequestSampleId,
                        principalTable: "AppTestRequestSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleAttachments_FileId",
                table: "AppTestRequestSampleAttachments",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleAttachments_TestRequestSampleId",
                table: "AppTestRequestSampleAttachments",
                column: "TestRequestSampleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTestRequestSampleAttachments");
        }
    }
}
