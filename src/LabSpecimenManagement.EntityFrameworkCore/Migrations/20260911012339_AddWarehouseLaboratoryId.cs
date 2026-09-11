using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseLaboratoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LaboratoryId",
                table: "AppWarehouses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Backfill existing warehouses with the first active laboratory
            migrationBuilder.Sql(@"
                UPDATE ""AppWarehouses""
                SET ""LaboratoryId"" = (
                    SELECT ""Id"" FROM ""AppLaboratories""
                    WHERE ""IsActive"" = true AND ""IsDeleted"" = false
                    ORDER BY ""CreationTime""
                    LIMIT 1
                )
                WHERE ""LaboratoryId"" = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_AppWarehouses_LaboratoryId",
                table: "AppWarehouses",
                column: "LaboratoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppWarehouses_AppLaboratories_LaboratoryId",
                table: "AppWarehouses",
                column: "LaboratoryId",
                principalTable: "AppLaboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppWarehouses_AppLaboratories_LaboratoryId",
                table: "AppWarehouses");

            migrationBuilder.DropIndex(
                name: "IX_AppWarehouses_LaboratoryId",
                table: "AppWarehouses");

            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "AppWarehouses");
        }
    }
}
