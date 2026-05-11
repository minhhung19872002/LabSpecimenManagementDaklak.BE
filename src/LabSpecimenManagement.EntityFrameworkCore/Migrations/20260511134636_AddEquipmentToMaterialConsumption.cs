using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentToMaterialConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppMaterialConsumptionItems_TestMethodIndicatorId_ProductId",
                table: "AppMaterialConsumptionItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "AppMaterialConsumptionItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "AppMaterialConsumptionItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "AppMaterialConsumptionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialConsumptionItems_EquipmentId",
                table: "AppMaterialConsumptionItems",
                column: "EquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppMaterialConsumptionItems_EquipmentId",
                table: "AppMaterialConsumptionItems");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "AppMaterialConsumptionItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "AppMaterialConsumptionItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "AppMaterialConsumptionItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppMaterialConsumptionItems_TestMethodIndicatorId_ProductId",
                table: "AppMaterialConsumptionItems",
                columns: new[] { "TestMethodIndicatorId", "ProductId" },
                unique: true);
        }
    }
}
