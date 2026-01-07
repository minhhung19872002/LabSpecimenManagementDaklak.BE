using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoryTaskSampleReceived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLaboratoryTaskSampleReceiveds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLaboratoryTaskSampleReceiveds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskSampleReceiveds_AppLaboratoryTasks_Laborat~",
                        column: x => x.LaboratoryTaskId,
                        principalTable: "AppLaboratoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskSampleReceiveds_AppTestRequestSamples_Samp~",
                        column: x => x.SampleId,
                        principalTable: "AppTestRequestSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSampleReceiveds_LaboratoryTaskId",
                table: "AppLaboratoryTaskSampleReceiveds",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSampleReceiveds_LaboratoryTaskId_SampleId",
                table: "AppLaboratoryTaskSampleReceiveds",
                columns: new[] { "LaboratoryTaskId", "SampleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSampleReceiveds_SampleId",
                table: "AppLaboratoryTaskSampleReceiveds",
                column: "SampleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLaboratoryTaskSampleReceiveds");
        }
    }
}
