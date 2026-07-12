using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporaryAccessGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTemporaryAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeType = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmployeeFullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EmployeeEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmployeePhone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SelectedModules = table.Column<string>(type: "text", nullable: false),
                    GrantedPermissions = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AppTemporaryAccessGrants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTemporaryAccessGrants_ExpiryDate",
                table: "AppTemporaryAccessGrants",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppTemporaryAccessGrants_Status",
                table: "AppTemporaryAccessGrants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppTemporaryAccessGrants_UserId",
                table: "AppTemporaryAccessGrants",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTemporaryAccessGrants");
        }
    }
}
