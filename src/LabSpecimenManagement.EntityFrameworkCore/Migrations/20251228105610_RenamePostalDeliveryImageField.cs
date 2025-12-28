using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class RenamePostalDeliveryImageField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostalDeliveryImageFileId",
                table: "AppTestRequests",
                newName: "PostalDeliveryImageFileIds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostalDeliveryImageFileIds",
                table: "AppTestRequests",
                newName: "PostalDeliveryImageFileId");
        }
    }
}
