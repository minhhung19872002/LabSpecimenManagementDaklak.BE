using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseIdToConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "AppTestBatchConsumptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "AppIndicatorConsumptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatchConsumptions_WarehouseId",
                table: "AppTestBatchConsumptions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorConsumptions_WarehouseId",
                table: "AppIndicatorConsumptions",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestBatchConsumptions_WarehouseId",
                table: "AppTestBatchConsumptions");

            migrationBuilder.DropIndex(
                name: "IX_AppIndicatorConsumptions_WarehouseId",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "AppTestBatchConsumptions");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "AppIndicatorConsumptions");
        }
    }
}
