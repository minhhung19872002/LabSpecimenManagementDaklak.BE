using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "AppWarehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentWarehouseId",
                table: "AppWarehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouses_ParentWarehouseId",
                table: "AppWarehouses",
                column: "ParentWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppWarehouses_AppWarehouses_ParentWarehouseId",
                table: "AppWarehouses",
                column: "ParentWarehouseId",
                principalTable: "AppWarehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppWarehouses_AppWarehouses_ParentWarehouseId",
                table: "AppWarehouses");

            migrationBuilder.DropIndex(
                name: "IX_AppWarehouses_ParentWarehouseId",
                table: "AppWarehouses");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "AppWarehouses");

            migrationBuilder.DropColumn(
                name: "ParentWarehouseId",
                table: "AppWarehouses");
        }
    }
}
