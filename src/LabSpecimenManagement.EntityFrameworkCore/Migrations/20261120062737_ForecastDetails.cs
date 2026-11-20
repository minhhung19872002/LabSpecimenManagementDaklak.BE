using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class ForecastDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceForecastId",
                table: "AppServiceYearlyForecasts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppServiceYearlyForecastItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServiceYearlyForecastItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServiceYearlyForecastItems_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceYearlyForecastItems_AppServiceYearlyForecasts_For~",
                        column: x => x.ForecastId,
                        principalTable: "AppServiceYearlyForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceYearlyForecasts_SourceForecastId",
                table: "AppServiceYearlyForecasts",
                column: "SourceForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceYearlyForecastItems_ForecastId_Kind_ProductId",
                table: "AppServiceYearlyForecastItems",
                columns: new[] { "ForecastId", "Kind", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceYearlyForecastItems_ProductId",
                table: "AppServiceYearlyForecastItems",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppServiceYearlyForecasts_AppServiceYearlyForecasts_SourceF~",
                table: "AppServiceYearlyForecasts",
                column: "SourceForecastId",
                principalTable: "AppServiceYearlyForecasts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppServiceYearlyForecasts_AppServiceYearlyForecasts_SourceF~",
                table: "AppServiceYearlyForecasts");

            migrationBuilder.DropTable(
                name: "AppServiceYearlyForecastItems");

            migrationBuilder.DropIndex(
                name: "IX_AppServiceYearlyForecasts_SourceForecastId",
                table: "AppServiceYearlyForecasts");

            migrationBuilder.DropColumn(
                name: "SourceForecastId",
                table: "AppServiceYearlyForecasts");
        }
    }
}
