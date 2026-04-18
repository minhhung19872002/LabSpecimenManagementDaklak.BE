using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class MakeTestMethodUnitNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestRequests_ContractNumber",
                table: "AppTestRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "AppTestMethods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequests_ContractNumber",
                table: "AppTestRequests",
                column: "ContractNumber",
                unique: true,
                filter: "\"ContractNumber\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestRequests_ContractNumber",
                table: "AppTestRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "AppTestMethods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequests_ContractNumber",
                table: "AppTestRequests",
                column: "ContractNumber");
        }
    }
}
