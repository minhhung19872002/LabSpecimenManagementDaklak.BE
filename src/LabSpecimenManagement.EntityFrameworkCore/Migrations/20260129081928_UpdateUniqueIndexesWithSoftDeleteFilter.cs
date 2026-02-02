using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUniqueIndexesWithSoftDeleteFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestResultValues_Code",
                table: "AppTestResultValues");

            migrationBuilder.DropIndex(
                name: "IX_AppTestMethods_Code",
                table: "AppTestMethods");

            migrationBuilder.DropIndex(
                name: "IX_AppTaskAssignments_TaskNumber",
                table: "AppTaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AppSubcontractors_Code",
                table: "AppSubcontractors");

            migrationBuilder.DropIndex(
                name: "IX_AppStorageLocations_Code",
                table: "AppStorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_AppStandards_Code",
                table: "AppStandards");

            migrationBuilder.DropIndex(
                name: "IX_AppSpecimenTypes_Code",
                table: "AppSpecimenTypes");

            migrationBuilder.DropIndex(
                name: "IX_AppReports_ReportNumber",
                table: "AppReports");

            migrationBuilder.DropIndex(
                name: "IX_AppNotes_Code",
                table: "AppNotes");

            migrationBuilder.DropIndex(
                name: "IX_AppNCRs_NCRNumber",
                table: "AppNCRs");

            migrationBuilder.DropIndex(
                name: "IX_AppLaboratories_Code",
                table: "AppLaboratories");

            migrationBuilder.DropIndex(
                name: "IX_AppIndicators_Code",
                table: "AppIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppIndicatorGroups_Name",
                table: "AppIndicatorGroups");

            migrationBuilder.DropIndex(
                name: "IX_AppDepartments_Code",
                table: "AppDepartments");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_Code",
                table: "AppCustomers");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValues_Code",
                table: "AppTestResultValues",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_Code",
                table: "AppTestMethods",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_TaskNumber",
                table: "AppTaskAssignments",
                column: "TaskNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractors_Code",
                table: "AppSubcontractors",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppStorageLocations_Code",
                table: "AppStorageLocations",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandards_Code",
                table: "AppStandards",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypes_Code",
                table: "AppSpecimenTypes",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_ReportNumber",
                table: "AppReports",
                column: "ReportNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotes_Code",
                table: "AppNotes",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_NCRNumber",
                table: "AppNCRs",
                column: "NCRNumber",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratories_Code",
                table: "AppLaboratories",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_Code",
                table: "AppIndicators",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorGroups_Name",
                table: "AppIndicatorGroups",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppDepartments_Code",
                table: "AppDepartments",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_Code",
                table: "AppCustomers",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestResultValues_Code",
                table: "AppTestResultValues");

            migrationBuilder.DropIndex(
                name: "IX_AppTestMethods_Code",
                table: "AppTestMethods");

            migrationBuilder.DropIndex(
                name: "IX_AppTaskAssignments_TaskNumber",
                table: "AppTaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AppSubcontractors_Code",
                table: "AppSubcontractors");

            migrationBuilder.DropIndex(
                name: "IX_AppStorageLocations_Code",
                table: "AppStorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_AppStandards_Code",
                table: "AppStandards");

            migrationBuilder.DropIndex(
                name: "IX_AppSpecimenTypes_Code",
                table: "AppSpecimenTypes");

            migrationBuilder.DropIndex(
                name: "IX_AppReports_ReportNumber",
                table: "AppReports");

            migrationBuilder.DropIndex(
                name: "IX_AppNotes_Code",
                table: "AppNotes");

            migrationBuilder.DropIndex(
                name: "IX_AppNCRs_NCRNumber",
                table: "AppNCRs");

            migrationBuilder.DropIndex(
                name: "IX_AppLaboratories_Code",
                table: "AppLaboratories");

            migrationBuilder.DropIndex(
                name: "IX_AppIndicators_Code",
                table: "AppIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppIndicatorGroups_Name",
                table: "AppIndicatorGroups");

            migrationBuilder.DropIndex(
                name: "IX_AppDepartments_Code",
                table: "AppDepartments");

            migrationBuilder.DropIndex(
                name: "IX_AppCustomers_Code",
                table: "AppCustomers");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResultValues_Code",
                table: "AppTestResultValues",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_Code",
                table: "AppTestMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_TaskNumber",
                table: "AppTaskAssignments",
                column: "TaskNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractors_Code",
                table: "AppSubcontractors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStorageLocations_Code",
                table: "AppStorageLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStandards_Code",
                table: "AppStandards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypes_Code",
                table: "AppSpecimenTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_ReportNumber",
                table: "AppReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNotes_Code",
                table: "AppNotes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_NCRNumber",
                table: "AppNCRs",
                column: "NCRNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratories_Code",
                table: "AppLaboratories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_Code",
                table: "AppIndicators",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorGroups_Name",
                table: "AppIndicatorGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppDepartments_Code",
                table: "AppDepartments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_Code",
                table: "AppCustomers",
                column: "Code",
                unique: true);
        }
    }
}
