using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddTestBatchTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTestBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecimenTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConsumptionConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConsumptionConfirmedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_AppTestBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTestBatchConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecommendedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsDeducted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestBatchConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestBatchConsumptions_AppTestBatches_TestBatchId",
                        column: x => x.TestBatchId,
                        principalTable: "AppTestBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SampleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    IsExternallyCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestBatchItems_AppTestBatches_TestBatchId",
                        column: x => x.TestBatchId,
                        principalTable: "AppTestBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatchConsumptions_EquipmentId",
                table: "AppTestBatchConsumptions",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatchConsumptions_ProductId",
                table: "AppTestBatchConsumptions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatchConsumptions_TestBatchId",
                table: "AppTestBatchConsumptions",
                column: "TestBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatches_Code",
                table: "AppTestBatches",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatches_CreationTime",
                table: "AppTestBatches",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatches_IndicatorId",
                table: "AppTestBatches",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatches_MethodId",
                table: "AppTestBatches",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatches_SpecimenTypeId",
                table: "AppTestBatches",
                column: "SpecimenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatches_Status",
                table: "AppTestBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatchItems_LaboratoryTaskId",
                table: "AppTestBatchItems",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatchItems_LaboratoryTaskIndicatorId",
                table: "AppTestBatchItems",
                column: "LaboratoryTaskIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestBatchItems_TestBatchId",
                table: "AppTestBatchItems",
                column: "TestBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTestBatchConsumptions");

            migrationBuilder.DropTable(
                name: "AppTestBatchItems");

            migrationBuilder.DropTable(
                name: "AppTestBatches");
        }
    }
}
