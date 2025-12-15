using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingMasterDataAndOtherEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "AppTests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "AppSpecimens",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "StorageLocation",
                table: "AppSpecimens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "AppSpecimens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Images",
                table: "AppSpecimens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Deviation",
                table: "AppSpecimens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "AppCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AppCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppNCRs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NCRNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReportedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCause = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreventiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppNCRs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SpecimenIds = table.Column<string>(type: "text", nullable: true),
                    TestIds = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PreparedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    QRCode = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PdfPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsEmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSpecimenTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultStorageCondition = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MaxStorageDays = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppSpecimenTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStorageLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Temperature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Humidity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    CurrentCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppStorageLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSubcontractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Capabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EvaluationScore = table.Column<decimal>(type: "numeric", nullable: true),
                    ContractStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppSubcontractors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTaskAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RelatedTestRequestIds = table.Column<string>(type: "text", nullable: true),
                    RelatedSpecimenIds = table.Column<string>(type: "text", nullable: true),
                    RelatedTestIds = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppTaskAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTestMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Standard = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstimatedDurationHours = table.Column<int>(type: "integer", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppTestMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSubcontractorCertifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IssuedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SubcontractorId1 = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_AppSubcontractorCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSubcontractorCertifications_AppSubcontractors_Subcontrac~",
                        column: x => x.SubcontractorId,
                        principalTable: "AppSubcontractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppSubcontractorCertifications_AppSubcontractors_Subcontra~1",
                        column: x => x.SubcontractorId1,
                        principalTable: "AppSubcontractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_Code",
                table: "AppCustomers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppDepartments_Code",
                table: "AppDepartments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_AssignedToId",
                table: "AppNCRs",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_NCRNumber",
                table: "AppNCRs",
                column: "NCRNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_RelatedEntityId",
                table: "AppNCRs",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_Severity",
                table: "AppNCRs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_Status",
                table: "AppNCRs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_ReportNumber",
                table: "AppReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_Status",
                table: "AppReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_TestRequestId",
                table: "AppReports",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypes_Code",
                table: "AppSpecimenTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStorageLocations_Code",
                table: "AppStorageLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractorCertifications_ExpiryDate",
                table: "AppSubcontractorCertifications",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractorCertifications_SubcontractorId",
                table: "AppSubcontractorCertifications",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractorCertifications_SubcontractorId1",
                table: "AppSubcontractorCertifications",
                column: "SubcontractorId1");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractors_Code",
                table: "AppSubcontractors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractors_IsActive",
                table: "AppSubcontractors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_AssignedById",
                table: "AppTaskAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_AssignedToId",
                table: "AppTaskAssignments",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_DueDate",
                table: "AppTaskAssignments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_Status",
                table: "AppTaskAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_TaskNumber",
                table: "AppTaskAssignments",
                column: "TaskNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_Code",
                table: "AppTestMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_DepartmentId",
                table: "AppTestMethods",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCustomers");

            migrationBuilder.DropTable(
                name: "AppDepartments");

            migrationBuilder.DropTable(
                name: "AppNCRs");

            migrationBuilder.DropTable(
                name: "AppReports");

            migrationBuilder.DropTable(
                name: "AppSpecimenTypes");

            migrationBuilder.DropTable(
                name: "AppStorageLocations");

            migrationBuilder.DropTable(
                name: "AppSubcontractorCertifications");

            migrationBuilder.DropTable(
                name: "AppTaskAssignments");

            migrationBuilder.DropTable(
                name: "AppTestMethods");

            migrationBuilder.DropTable(
                name: "AppSubcontractors");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "AppTests",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "AppSpecimens",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StorageLocation",
                table: "AppSpecimens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "AppSpecimens",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Images",
                table: "AppSpecimens",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Deviation",
                table: "AppSpecimens",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
