using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add AssignedDate column to AppLaboratoryTaskIndicators
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "AppLaboratoryTaskIndicators",
                type: "timestamp without time zone",
                nullable: true);

            // Create AppIndicatorRejectionHistories table
            migrationBuilder.CreateTable(
                name: "AppIndicatorRejectionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RejectedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RejectedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIndicatorRejectionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppIndicatorRejectionHistories_AppLaboratoryTaskIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "AppLaboratoryTaskIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create index on IndicatorId for rejection histories
            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorRejectionHistories_IndicatorId",
                table: "AppIndicatorRejectionHistories",
                column: "IndicatorId");

            // Create index on AssignedDate
            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_AssignedDate",
                table: "AppLaboratoryTaskIndicators",
                column: "AssignedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop AppIndicatorRejectionHistories table
            migrationBuilder.DropTable(
                name: "AppIndicatorRejectionHistories");

            // Drop AssignedDate index
            migrationBuilder.DropIndex(
                name: "IX_AppLaboratoryTaskIndicators_AssignedDate",
                table: "AppLaboratoryTaskIndicators");

            // Drop AssignedDate column
            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
