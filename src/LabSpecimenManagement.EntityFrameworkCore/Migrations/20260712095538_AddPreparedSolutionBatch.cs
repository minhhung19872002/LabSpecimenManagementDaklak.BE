using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPreparedSolutionBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppPreparedSolutionBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolutionProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolutionProductName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IdentificationCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LotCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PreparationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TotalVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    VolumeUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    VolumeUnitName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    StorageWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageWarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceWarehouseName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StorageConditions = table.Column<string>(type: "text", nullable: true),
                    PreparationMethod = table.Column<string>(type: "text", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProcessingImages = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    PreparedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreparedByName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_AppPreparedSolutionBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPreparedSolutionIngredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PreparedSolutionBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductType = table.Column<int>(type: "integer", nullable: false),
                    ProductTypeName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UnitName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    QuantityUsed = table.Column<decimal>(type: "numeric", nullable: false),
                    WarehouseQuantitySnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    IsEquipment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPreparedSolutionIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPreparedSolutionIngredients_AppPreparedSolutionBatches_P~",
                        column: x => x.PreparedSolutionBatchId,
                        principalTable: "AppPreparedSolutionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppPreparedSolutionBatches_ExpiryDate",
                table: "AppPreparedSolutionBatches",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppPreparedSolutionBatches_IsProcessed",
                table: "AppPreparedSolutionBatches",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_AppPreparedSolutionBatches_PreparationDate",
                table: "AppPreparedSolutionBatches",
                column: "PreparationDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppPreparedSolutionIngredients_PreparedSolutionBatchId",
                table: "AppPreparedSolutionIngredients",
                column: "PreparedSolutionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPreparedSolutionIngredients_ProductId",
                table: "AppPreparedSolutionIngredients",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPreparedSolutionIngredients");

            migrationBuilder.DropTable(
                name: "AppPreparedSolutionBatches");
        }
    }
}
