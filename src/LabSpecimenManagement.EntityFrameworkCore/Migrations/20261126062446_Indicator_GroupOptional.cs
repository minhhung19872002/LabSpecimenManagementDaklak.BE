using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class Indicator_GroupOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "IndicatorGroupId",
                table: "AppIndicators",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Dữ liệu cũ dùng Guid.Empty làm "không nhóm" → chuẩn hóa về NULL
            migrationBuilder.Sql("UPDATE \"AppIndicators\" SET \"IndicatorGroupId\" = NULL WHERE \"IndicatorGroupId\" = '00000000-0000-0000-0000-000000000000';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"AppIndicators\" SET \"IndicatorGroupId\" = '00000000-0000-0000-0000-000000000000' WHERE \"IndicatorGroupId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "IndicatorGroupId",
                table: "AppIndicators",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
