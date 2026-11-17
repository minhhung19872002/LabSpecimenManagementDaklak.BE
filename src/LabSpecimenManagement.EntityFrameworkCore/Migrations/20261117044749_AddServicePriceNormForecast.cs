using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePriceNormForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId_SpecimenTy~",
                table: "AppTestMethodIndicators");

            migrationBuilder.CreateSequence(
                name: "AppBatchNormCodeSeq");

            migrationBuilder.CreateSequence(
                name: "AppServiceCodeSeq");

            // Mã tự sinh DV0001 / DM0001 — quá 9999 thì không cắt số (lpad sẽ truncate)
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION app_pad_code(n bigint) RETURNS text
LANGUAGE sql IMMUTABLE AS $$
    SELECT CASE WHEN n < 10000 THEN lpad(n::text, 4, '0') ELSE n::text END
$$;");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppTestMethodIndicators",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ServiceCode",
                table: "AppTestMethodIndicators",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValueSql: "'DV' || app_pad_code(nextval('\"AppServiceCodeSeq\"'))");

            migrationBuilder.AddColumn<int>(
                name: "TurnaroundDays",
                table: "AppTestMethodIndicators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MethodBatchNormId",
                table: "AppIndicatorConsumptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NormBatchQuantity",
                table: "AppIndicatorConsumptions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NormEstimatedSamples",
                table: "AppIndicatorConsumptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NormForecastYear",
                table: "AppIndicatorConsumptions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppMethodBatchNorms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'DM' || app_pad_code(nextval('\"AppBatchNormCodeSeq\"'))"),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchQuantity = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_AppMethodBatchNorms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppServicePriceVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TurnaroundDays = table.Column<int>(type: "integer", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_AppServicePriceVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServicePriceVersions_AppTestMethodIndicators_TestMethodI~",
                        column: x => x.TestMethodIndicatorId,
                        principalTable: "AppTestMethodIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppServiceYearlyForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    EstimatedSamples = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_AppServiceYearlyForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServiceYearlyForecasts_AppTestMethodIndicators_TestMetho~",
                        column: x => x.TestMethodIndicatorId,
                        principalTable: "AppTestMethodIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_ServiceCode",
                table: "AppTestMethodIndicators",
                column: "ServiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId_SpecimenTy~",
                table: "AppTestMethodIndicators",
                columns: new[] { "TestMethodId", "IndicatorId", "SpecimenTypeId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodBatchNorms_Code",
                table: "AppMethodBatchNorms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodBatchNorms_TestMethodId",
                table: "AppMethodBatchNorms",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMethodBatchNorms_TestMethodId_ProductId",
                table: "AppMethodBatchNorms",
                columns: new[] { "TestMethodId", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"ProductId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppServicePriceVersions_TestMethodIndicatorId",
                table: "AppServicePriceVersions",
                column: "TestMethodIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServicePriceVersions_TestMethodIndicatorId_EffectiveDate",
                table: "AppServicePriceVersions",
                columns: new[] { "TestMethodIndicatorId", "EffectiveDate" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceYearlyForecasts_TestMethodIndicatorId_Year",
                table: "AppServiceYearlyForecasts",
                columns: new[] { "TestMethodIndicatorId", "Year" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceYearlyForecasts_Year",
                table: "AppServiceYearlyForecasts",
                column: "Year");

            // Mỗi dịch vụ hiện có → 1 phiên bản giá gốc (EffectiveDate NULL) = đơn giá hiện tại
            migrationBuilder.Sql(@"
INSERT INTO ""AppServicePriceVersions""
    (""Id"", ""TestMethodIndicatorId"", ""UnitPrice"", ""TurnaroundDays"", ""EffectiveDate"", ""Note"", ""CreationTime"", ""IsDeleted"")
SELECT gen_random_uuid(), tmi.""Id"", COALESCE(tmi.""UnitPrice"", 0), NULL, NULL, 'Giá gốc', now(), false
FROM ""AppTestMethodIndicators"" tmi;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMethodBatchNorms");

            migrationBuilder.DropTable(
                name: "AppServicePriceVersions");

            migrationBuilder.DropTable(
                name: "AppServiceYearlyForecasts");

            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_ServiceCode",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId_SpecimenTy~",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropColumn(
                name: "ServiceCode",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropColumn(
                name: "TurnaroundDays",
                table: "AppTestMethodIndicators");

            migrationBuilder.DropColumn(
                name: "MethodBatchNormId",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "NormBatchQuantity",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "NormEstimatedSamples",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropColumn(
                name: "NormForecastYear",
                table: "AppIndicatorConsumptions");

            migrationBuilder.DropSequence(
                name: "AppBatchNormCodeSeq");

            migrationBuilder.DropSequence(
                name: "AppServiceCodeSeq");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app_pad_code(bigint);");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId_SpecimenTy~",
                table: "AppTestMethodIndicators",
                columns: new[] { "TestMethodId", "IndicatorId", "SpecimenTypeId" },
                unique: true);
        }
    }
}
