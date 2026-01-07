using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuotationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProvinceCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DistrictCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    WardCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQuotations_AppOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "AppOrganizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppQuotationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TestMethodName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQuotationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQuotationItems_AppIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "AppIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppQuotationItems_AppQuotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "AppQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppQuotationItems_AppTestMethods_TestMethodId",
                        column: x => x.TestMethodId,
                        principalTable: "AppTestMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationItems_IndicatorId",
                table: "AppQuotationItems",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationItems_QuotationId",
                table: "AppQuotationItems",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationItems_TestMethodId",
                table: "AppQuotationItems",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotations_CustomerName",
                table: "AppQuotations",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotations_OrganizationId",
                table: "AppQuotations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotations_QuotationDate",
                table: "AppQuotations",
                column: "QuotationDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotations_QuotationNumber",
                table: "AppQuotations",
                column: "QuotationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotations_Status",
                table: "AppQuotations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppQuotationItems");

            migrationBuilder.DropTable(
                name: "AppQuotations");
        }
    }
}
