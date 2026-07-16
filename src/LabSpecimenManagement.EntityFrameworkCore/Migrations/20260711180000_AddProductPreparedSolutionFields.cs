using System;
using LabSpecimenManagement.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    [DbContext(typeof(LabSpecimenManagementDbContext))]
    [Migration("20260711180000_AddProductPreparedSolutionFields")]
    public partial class AddProductPreparedSolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsagePurpose",
                table: "AppProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelfLifeDays",
                table: "AppProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseProductId",
                table: "AppProducts",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UsagePurpose", table: "AppProducts");
            migrationBuilder.DropColumn(name: "ShelfLifeDays", table: "AppProducts");
            migrationBuilder.DropColumn(name: "BaseProductId", table: "AppProducts");
        }
    }
}
