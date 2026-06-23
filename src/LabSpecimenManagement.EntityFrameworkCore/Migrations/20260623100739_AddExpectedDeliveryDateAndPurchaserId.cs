using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddExpectedDeliveryDateAndPurchaserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "AppGoodsReceipts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaserId",
                table: "AppGoodsReceipts",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "PurchaserId",
                table: "AppGoodsReceipts");
        }
    }
}
