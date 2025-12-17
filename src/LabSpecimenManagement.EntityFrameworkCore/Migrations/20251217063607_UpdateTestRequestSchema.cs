using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTestRequestSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerInfo",
                table: "AppTestRequests");

            migrationBuilder.RenameColumn(
                name: "CustomerPhone",
                table: "AppTestRequests",
                newName: "TaxCode");

            migrationBuilder.RenameColumn(
                name: "CustomerEmail",
                table: "AppTestRequests",
                newName: "SubContract");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "AppTestRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConformityDeclaration",
                table: "AppTestRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "AppTestRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNumber",
                table: "AppTestRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryDirectPickup",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryPostal",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DirectorId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "AppTestRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MethodCustomer",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MethodCustomerDetail",
                table: "AppTestRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MethodWestHighlandInstitute",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherRequirements",
                table: "AppTestRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentReceived",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "AppTestRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "AppTestRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "AppTestRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultCopyCount",
                table: "AppTestRequests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultDeliveryDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultLanguage",
                table: "AppTestRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ResultReturned",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReturnStoredSample",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SampleStorage",
                table: "AppTestRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SampleTypeContract",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SampleTypeService",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SampleTypeSubContract",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SampleTypeTask",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SamplingRequirement",
                table: "AppTestRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SentToAccounting",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SentToDepartment",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SeparatePassedResults",
                table: "AppTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SupervisorId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "AppTestRequests",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TotalCostInWords",
                table: "AppTestRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSampleCount",
                table: "AppTestRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "AppTestRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppTestRequestSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    SampleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SampleName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SamplingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SampleQuantity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StorageLocation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TotalIndicatorCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_AppTestRequestSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSamples_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestSampleIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestSampleIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleIndicators_AppIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "AppIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleIndicators_AppTestRequestSamples_TestRe~",
                        column: x => x.TestRequestSampleId,
                        principalTable: "AppTestRequestSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequests_ContractNumber",
                table: "AppTestRequests",
                column: "ContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_IndicatorId",
                table: "AppTestRequestSampleIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_TestRequestSampleId",
                table: "AppTestRequestSampleIndicators",
                column: "TestRequestSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSamples_SampleCode",
                table: "AppTestRequestSamples",
                column: "SampleCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSamples_TestRequestId",
                table: "AppTestRequestSamples",
                column: "TestRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTestRequestSampleIndicators");

            migrationBuilder.DropTable(
                name: "AppTestRequestSamples");

            migrationBuilder.DropIndex(
                name: "IX_AppTestRequests_ContractNumber",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ConformityDeclaration",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ContractDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ContractNumber",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "DeliveryDirectPickup",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "DeliveryPostal",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "DirectorId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "District",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "MethodCustomer",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "MethodCustomerDetail",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "MethodWestHighlandInstitute",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "OtherRequirements",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "PaymentReceived",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ResultCopyCount",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ResultDeliveryDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ResultLanguage",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ResultReturned",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "ReturnStoredSample",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SampleStorage",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SampleTypeContract",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SampleTypeService",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SampleTypeSubContract",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SampleTypeTask",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SamplingRequirement",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SentToAccounting",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SentToDepartment",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SeparatePassedResults",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "TotalCostInWords",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "TotalSampleCount",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "AppTestRequests");

            migrationBuilder.RenameColumn(
                name: "TaxCode",
                table: "AppTestRequests",
                newName: "CustomerPhone");

            migrationBuilder.RenameColumn(
                name: "SubContract",
                table: "AppTestRequests",
                newName: "CustomerEmail");

            migrationBuilder.AddColumn<string>(
                name: "CustomerInfo",
                table: "AppTestRequests",
                type: "text",
                nullable: true);
        }
    }
}
