using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicatorNameChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppIndicatorNameChangeHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldName = table.Column<string>(type: "text", nullable: true),
                    OldFormattedName = table.Column<string>(type: "text", nullable: true),
                    NewName = table.Column<string>(type: "text", nullable: true),
                    NewFormattedName = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIndicatorNameChangeHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorNameChangeHistories_LineId",
                table: "AppIndicatorNameChangeHistories",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorNameChangeHistories_Source_DocumentId",
                table: "AppIndicatorNameChangeHistories",
                columns: new[] { "Source", "DocumentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppIndicatorNameChangeHistories");
        }
    }
}
