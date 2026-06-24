using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptApproverSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Approver1Id",
                table: "AppGoodsReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Approver1Name",
                table: "AppGoodsReceipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Approver2Id",
                table: "AppGoodsReceipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Approver2Name",
                table: "AppGoodsReceipts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Approver1Id",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "Approver1Name",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "Approver2Id",
                table: "AppGoodsReceipts");

            migrationBuilder.DropColumn(
                name: "Approver2Name",
                table: "AppGoodsReceipts");
        }
    }
}
