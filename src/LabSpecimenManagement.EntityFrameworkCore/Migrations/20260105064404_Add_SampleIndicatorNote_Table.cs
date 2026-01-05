using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class Add_SampleIndicatorNote_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SampleIndicatorNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSampleIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteContent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SelectedNoteIds = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleIndicatorNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleIndicatorNotes_AppTestRequestSampleIndicators_TestReq~",
                        column: x => x.TestRequestSampleIndicatorId,
                        principalTable: "AppTestRequestSampleIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleIndicatorNotes_TestRequestSampleIndicatorId",
                table: "SampleIndicatorNotes",
                column: "TestRequestSampleIndicatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SampleIndicatorNotes");
        }
    }
}
