using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class ServiceGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "AppServiceGroupCodeSeq");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceGroupId",
                table: "AppTestMethodIndicators",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppServiceGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'NDV' || app_pad_code(nextval('\"AppServiceGroupCodeSeq\"'))"),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServiceGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_ServiceGroupId",
                table: "AppTestMethodIndicators",
                column: "ServiceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceGroups_Code",
                table: "AppServiceGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceGroups_Name",
                table: "AppServiceGroups",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTestMethodIndicators_AppServiceGroups_ServiceGroupId",
                table: "AppTestMethodIndicators",
                column: "ServiceGroupId",
                principalTable: "AppServiceGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTestMethodIndicators_AppServiceGroups_ServiceGroupId",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropTable(
                name: "AppServiceGroups");

            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_ServiceGroupId",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropColumn(
                name: "ServiceGroupId",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropSequence(
                name: "AppServiceGroupCodeSeq");
        }
    }
}
