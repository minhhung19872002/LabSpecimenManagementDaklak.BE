using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVoucherCodeFromReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppReceipts_VoucherCode",
                table: "AppReceipts");

            migrationBuilder.DropColumn(
                name: "VoucherCode",
                table: "AppReceipts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherCode",
                table: "AppReceipts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppReceipts_VoucherCode",
                table: "AppReceipts",
                column: "VoucherCode");
        }
    }
}
