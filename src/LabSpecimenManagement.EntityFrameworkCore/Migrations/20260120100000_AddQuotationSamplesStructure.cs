using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationSamplesStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old QuotationItems table
            migrationBuilder.DropTable(
                name: "AppQuotationItems");

            // Create QuotationSamples table
            migrationBuilder.CreateTable(
                name: "AppQuotationSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    SampleName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQuotationSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQuotationSamples_AppQuotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "AppQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create QuotationSampleIndicators table
            migrationBuilder.CreateTable(
                name: "AppQuotationSampleIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TestMethodName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQuotationSampleIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQuotationSampleIndicators_AppIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "AppIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppQuotationSampleIndicators_AppQuotationSamples_QuotationSampleId",
                        column: x => x.QuotationSampleId,
                        principalTable: "AppQuotationSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppQuotationSampleIndicators_AppTestMethods_TestMethodId",
                        column: x => x.TestMethodId,
                        principalTable: "AppTestMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for QuotationSamples
            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationSamples_QuotationId",
                table: "AppQuotationSamples",
                column: "QuotationId");

            // Create indexes for QuotationSampleIndicators
            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationSampleIndicators_IndicatorId",
                table: "AppQuotationSampleIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationSampleIndicators_QuotationSampleId",
                table: "AppQuotationSampleIndicators",
                column: "QuotationSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQuotationSampleIndicators_TestMethodId",
                table: "AppQuotationSampleIndicators",
                column: "TestMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new tables
            migrationBuilder.DropTable(
                name: "AppQuotationSampleIndicators");

            migrationBuilder.DropTable(
                name: "AppQuotationSamples");

            // Recreate old QuotationItems table
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
        }
    }
}
