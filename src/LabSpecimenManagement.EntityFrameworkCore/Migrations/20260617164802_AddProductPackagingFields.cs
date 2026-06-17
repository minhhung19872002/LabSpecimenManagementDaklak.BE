using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPackagingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackagingQuantity",
                table: "AppProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackagingUnitId",
                table: "AppProducts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolumePerUnit",
                table: "AppProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VolumeUnitId",
                table: "AppProducts",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackagingQuantity",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "PackagingUnitId",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "VolumePerUnit",
                table: "AppProducts");

            migrationBuilder.DropColumn(
                name: "VolumeUnitId",
                table: "AppProducts");
        }
    }
}
