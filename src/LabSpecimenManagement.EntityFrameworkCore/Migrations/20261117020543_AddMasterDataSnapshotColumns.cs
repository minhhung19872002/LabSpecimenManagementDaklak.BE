using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataSnapshotColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecimenTypeName",
                table: "AppTestRequestSamples",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndicatorCode",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndicatorFormattedName",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndicatorGroupName",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndicatorName",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LaboratoryName",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecimenTypeName",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestMethodCode",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestMethodFormattedName",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestMethodFormattedUnit",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestMethodName",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestMethodUnit",
                table: "AppTestRequestSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndicatorFormattedName",
                table: "AppQuotationSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestMethodFormattedName",
                table: "AppQuotationSampleIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LaboratoryName",
                table: "AppLaboratoryTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MethodCode",
                table: "AppLaboratoryTaskIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MethodFormattedName",
                table: "AppLaboratoryTaskIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MethodName",
                table: "AppLaboratoryTaskIndicators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StandardName",
                table: "AppLaboratoryTaskIndicators",
                type: "text",
                nullable: true);
            // Backfill snapshot từ master hiện tại (kể cả bản ghi đã xóa mềm) cho dữ liệu cũ
            migrationBuilder.Sql(@"
UPDATE ""AppTestRequestSampleIndicators"" si SET
    ""IndicatorCode"" = i.""Code"",
    ""IndicatorName"" = i.""Name"",
    ""IndicatorFormattedName"" = i.""FormattedName"",
    ""IndicatorGroupName"" = g.""Name""
FROM ""AppIndicators"" i
LEFT JOIN ""AppIndicatorGroups"" g ON g.""Id"" = i.""IndicatorGroupId""
WHERE i.""Id"" = si.""IndicatorId"";

UPDATE ""AppTestRequestSampleIndicators"" si SET
    ""TestMethodCode"" = m.""Code"",
    ""TestMethodName"" = m.""Name"",
    ""TestMethodFormattedName"" = m.""FormattedName"",
    ""TestMethodUnit"" = m.""Unit"",
    ""TestMethodFormattedUnit"" = m.""FormattedUnit""
FROM ""AppTestMethods"" m
WHERE m.""Id"" = si.""TestMethodId"";

UPDATE ""AppTestRequestSampleIndicators"" si SET
    ""LaboratoryName"" = d.""Name""
FROM ""AppLaboratories"" l
JOIN ""AppDepartments"" d ON d.""Id"" = l.""DepartmentId""
WHERE l.""Id"" = si.""LaboratoryId"";

UPDATE ""AppTestRequestSampleIndicators"" si SET
    ""SpecimenTypeName"" = st.""Name""
FROM ""AppSpecimenTypes"" st
WHERE st.""Id"" = si.""SpecimenTypeId"";

UPDATE ""AppTestRequestSamples"" s SET
    ""SpecimenTypeName"" = st.""Name""
FROM ""AppSpecimenTypes"" st
WHERE st.""Id"" = s.""SpecimenTypeId"";

UPDATE ""AppLaboratoryTasks"" t SET
    ""LaboratoryName"" = d.""Name""
FROM ""AppLaboratories"" l
JOIN ""AppDepartments"" d ON d.""Id"" = l.""DepartmentId""
WHERE l.""Id"" = t.""LaboratoryId"";

UPDATE ""AppLaboratoryTaskIndicators"" li SET
    ""MethodCode"" = m.""Code"",
    ""MethodName"" = m.""Name"",
    ""MethodFormattedName"" = m.""FormattedName""
FROM ""AppTestMethods"" m
WHERE m.""Id"" = li.""MethodId"";

UPDATE ""AppLaboratoryTaskIndicators"" li SET
    ""StandardName"" = s.""Name""
FROM ""AppStandards"" s
WHERE s.""Id"" = li.""StandardId"";
UPDATE ""AppQuotationSampleIndicators"" qi SET
    ""IndicatorFormattedName"" = i.""FormattedName""
FROM ""AppIndicators"" i
WHERE i.""Id"" = qi.""IndicatorId"";

UPDATE ""AppQuotationSampleIndicators"" qi SET
    ""TestMethodFormattedName"" = m.""FormattedName""
FROM ""AppTestMethods"" m
WHERE m.""Id"" = qi.""TestMethodId"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecimenTypeName",
                table: "AppTestRequestSamples");

            migrationBuilder.DropColumn(
                name: "IndicatorCode",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "IndicatorFormattedName",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "IndicatorGroupName",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "IndicatorName",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "LaboratoryName",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "SpecimenTypeName",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TestMethodCode",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TestMethodFormattedName",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TestMethodFormattedUnit",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TestMethodName",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TestMethodUnit",
                table: "AppTestRequestSampleIndicators");

            migrationBuilder.DropColumn(
                name: "IndicatorFormattedName",
                table: "AppQuotationSampleIndicators");

            migrationBuilder.DropColumn(
                name: "TestMethodFormattedName",
                table: "AppQuotationSampleIndicators");

            migrationBuilder.DropColumn(
                name: "LaboratoryName",
                table: "AppLaboratoryTasks");

            migrationBuilder.DropColumn(
                name: "MethodCode",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "MethodFormattedName",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "MethodName",
                table: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropColumn(
                name: "StandardName",
                table: "AppLaboratoryTaskIndicators");
        }
    }
}
