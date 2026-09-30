using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTestRequestDeliveryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTestRequestDeliveryHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrintedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StatusAfter = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestDeliveryHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestDeliveryHistories_AppTestRequests_TestRequest~",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestDeliveryHistories_TestRequestId",
                table: "AppTestRequestDeliveryHistories",
                column: "TestRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTestRequestDeliveryHistories");
        }
    }
}
