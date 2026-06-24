using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixApprovalProcessUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppApprovalProcesses_ProcessType",
                table: "AppApprovalProcesses");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalProcesses_ProcessType",
                table: "AppApprovalProcesses",
                column: "ProcessType",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppApprovalProcesses_ProcessType",
                table: "AppApprovalProcesses");

            migrationBuilder.CreateIndex(
                name: "IX_AppApprovalProcesses_ProcessType",
                table: "AppApprovalProcesses",
                column: "ProcessType",
                unique: true);
        }
    }
}
