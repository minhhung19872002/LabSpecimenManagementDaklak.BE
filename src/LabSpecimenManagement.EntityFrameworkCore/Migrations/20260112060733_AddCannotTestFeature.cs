using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCannotTestFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCannotTest",
                table: "AppIndicatorRejectionHistories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReportedByRole",
                table: "AppIndicatorRejectionHistories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppRejectionHistoryImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RejectionHistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRejectionHistoryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRejectionHistoryImages_AppIndicatorRejectionHistories_Re~",
                        column: x => x.RejectionHistoryId,
                        principalTable: "AppIndicatorRejectionHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorRejectionHistories_IsCannotTest",
                table: "AppIndicatorRejectionHistories",
                column: "IsCannotTest");

            migrationBuilder.CreateIndex(
                name: "IX_AppRejectionHistoryImages_RejectionHistoryId",
                table: "AppRejectionHistoryImages",
                column: "RejectionHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppRejectionHistoryImages");

            migrationBuilder.DropIndex(
                name: "IX_AppIndicatorRejectionHistories_IsCannotTest",
                table: "AppIndicatorRejectionHistories");

            migrationBuilder.DropColumn(
                name: "IsCannotTest",
                table: "AppIndicatorRejectionHistories");

            migrationBuilder.DropColumn(
                name: "ReportedByRole",
                table: "AppIndicatorRejectionHistories");
        }
    }
}
