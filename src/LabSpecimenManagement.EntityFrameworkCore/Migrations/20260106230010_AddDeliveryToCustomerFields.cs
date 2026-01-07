using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryToCustomerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveredToCustomerByUserId",
                table: "AppTestRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveredToCustomerByUserName",
                table: "AppTestRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredToCustomerDate",
                table: "AppTestRequests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                table: "AppTestRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredToCustomerByUserId",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "DeliveredToCustomerByUserName",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "DeliveredToCustomerDate",
                table: "AppTestRequests");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                table: "AppTestRequests");
        }
    }
}
