using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class ServiceDeclarePendingPriceSubcontractor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "AppTestMethodIndicators",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "SubcontractorId",
                table: "AppTestMethodIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_SubcontractorId",
                table: "AppTestMethodIndicators",
                column: "SubcontractorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_SubcontractorId",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropColumn(
                name: "SubcontractorId",
                table: "AppTestMethodIndicators");

            migrationBuilder.Sql("UPDATE \"AppTestMethodIndicators\" SET \"UnitPrice\" = 0 WHERE \"UnitPrice\" IS NULL;");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "AppTestMethodIndicators",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
