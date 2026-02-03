using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddChatTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppChatChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Configuration = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppChatChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppChatContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppChatContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppChatContacts_AppChatChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "AppChatChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppChatContacts_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppChatConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastMessagePreview = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UnreadCount = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_AppChatConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppChatConversations_AppChatChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "AppChatChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppChatConversations_AppChatContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "AppChatContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderType = table.Column<int>(type: "integer", nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Attachments = table.Column<string>(type: "jsonb", nullable: true),
                    ExternalMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppChatMessages_AppChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AppChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatChannels_IsActive",
                table: "AppChatChannels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatChannels_Type",
                table: "AppChatChannels",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatContacts_ChannelId",
                table: "AppChatContacts",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatContacts_ChannelId_ExternalId",
                table: "AppChatContacts",
                columns: new[] { "ChannelId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppChatContacts_CustomerId",
                table: "AppChatContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatConversations_AssignedAgentId",
                table: "AppChatConversations",
                column: "AssignedAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatConversations_AssignedAgentId_Status",
                table: "AppChatConversations",
                columns: new[] { "AssignedAgentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatConversations_ChannelId",
                table: "AppChatConversations",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatConversations_ChannelId_Status",
                table: "AppChatConversations",
                columns: new[] { "ChannelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatConversations_ContactId",
                table: "AppChatConversations",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatConversations_LastMessageAt",
                table: "AppChatConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatConversations_Status",
                table: "AppChatConversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_ConversationId",
                table: "AppChatMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_ConversationId_CreationTime",
                table: "AppChatMessages",
                columns: new[] { "ConversationId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_ConversationId_IsDeleted",
                table: "AppChatMessages",
                columns: new[] { "ConversationId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_ExternalMessageId",
                table: "AppChatMessages",
                column: "ExternalMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppChatMessages");

            migrationBuilder.DropTable(
                name: "AppChatConversations");

            migrationBuilder.DropTable(
                name: "AppChatContacts");

            migrationBuilder.DropTable(
                name: "AppChatChannels");
        }
    }
}
