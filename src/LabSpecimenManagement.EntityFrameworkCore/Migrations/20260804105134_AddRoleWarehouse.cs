using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppRoleWarehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoleWarehouses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleWarehouses_RoleId",
                table: "AppRoleWarehouses",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleWarehouses_RoleId_WarehouseId",
                table: "AppRoleWarehouses",
                columns: new[] { "RoleId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleWarehouses_WarehouseId",
                table: "AppRoleWarehouses",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppRoleWarehouses");
        }
    }
}
