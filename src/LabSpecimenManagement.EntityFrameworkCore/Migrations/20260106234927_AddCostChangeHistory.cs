using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCostChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCostChangeHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    OldTotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    NewTotalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    DifferenceAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AffectedIndicatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AffectedIndicatorName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IndicatorUnitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppCostChangeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCostChangeHistories_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCostChangeHistories_CreationTime",
                table: "AppCostChangeHistories",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppCostChangeHistories_TestRequestId",
                table: "AppCostChangeHistories",
                column: "TestRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCostChangeHistories");
        }
    }
}
