using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabSpecimenManagement.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbpAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationName = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImpersonatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImpersonatorUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ImpersonatorTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImpersonatorTenantName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExecutionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExecutionDuration = table.Column<int>(type: "integer", nullable: false),
                    ClientIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ClientName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BrowserInfo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Url = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Exceptions = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpBackgroundJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    JobArgs = table.Column<string>(type: "character varying(1048576)", maxLength: 1048576, nullable: false),
                    TryCount = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NextTryTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastTryTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsAbandoned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Priority = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)15),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpBackgroundJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpClaimTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    IsStatic = table.Column<bool>(type: "boolean", nullable: false),
                    Regex = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RegexDescription = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ValueType = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpClaimTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpFeatureGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpFeatureGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ParentName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DefaultValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsVisibleToClients = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailableToHost = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedProviders = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ValueType = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpFeatureValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpFeatureValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpLinkUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpLinkUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpOrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(95)", maxLength: 95, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AbpOrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpOrganizationUnits_AbpOrganizationUnits_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpPermissionGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpPermissionGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpPermissionGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpPermissionGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ParentName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MultiTenancySide = table.Column<byte>(type: "smallint", nullable: false),
                    Providers = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StateCheckers = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsStatic = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpSecurityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicationName = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    Identity = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    Action = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TenantName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ClientIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BrowserInfo = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpSecurityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Device = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeviceInfo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IpAddresses = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SignedIn = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastAccessed = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpSettingDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DefaultValue = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsVisibleToClients = table.Column<bool>(type: "boolean", nullable: false),
                    Providers = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsInherited = table.Column<bool>(type: "boolean", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpSettingDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpTenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AbpTenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserDelegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserDelegations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Surname = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SecurityStamp = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PhoneNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ShouldChangePasswordOnNextLogin = table.Column<bool>(type: "boolean", nullable: false),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false),
                    LastPasswordChangeTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_AbpUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AppCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppIndicatorGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AppIndicatorGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IndicatorGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppLaboratories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TechnicalManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppLaboratories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppNCRs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NCRNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReportedById = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCause = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreventiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ClosedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppNCRs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SenderName = table.Column<string>(type: "text", nullable: true),
                    ItemCount = table.Column<int>(type: "integer", nullable: true),
                    AdditionalData = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProvinceCode = table.Column<string>(type: "text", nullable: false),
                    WardCode = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    TaxCode = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppOrganizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SpecimenIds = table.Column<string>(type: "text", nullable: true),
                    TestIds = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PreparedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    QRCode = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PdfPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsEmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSpecimenTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultStorageCondition = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MaxStorageDays = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppSpecimenTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStandardIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ComparisonOperator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AllowedLimit = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppStandardIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStandards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LimitDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppStandards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppStorageLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Temperature = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Humidity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    CurrentCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppStorageLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSubcontractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Capabilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EvaluationScore = table.Column<decimal>(type: "numeric", nullable: true),
                    ContractStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ContractEndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppSubcontractors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTaskAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RelatedTestRequestIds = table.Column<string>(type: "text", nullable: true),
                    RelatedSpecimenIds = table.Column<string>(type: "text", nullable: true),
                    RelatedTestIds = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_AppTaskAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTestMethodIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestMethodIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTestMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Unit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_AppTestMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContractDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SentToAccounting = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentReceived = table.Column<bool>(type: "boolean", nullable: false),
                    SentToDepartment = table.Column<bool>(type: "boolean", nullable: false),
                    ResultReturned = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProvinceCode = table.Column<string>(type: "text", nullable: true),
                    Province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DistrictCode = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WardCode = table.Column<string>(type: "text", nullable: true),
                    Ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TotalSampleCount = table.Column<int>(type: "integer", nullable: false),
                    MethodWestHighlandInstitute = table.Column<bool>(type: "boolean", nullable: false),
                    MethodCustomer = table.Column<bool>(type: "boolean", nullable: false),
                    MethodCustomerDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResultLanguage = table.Column<int>(type: "integer", nullable: false),
                    ResultCopyCount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SeparatePassedResults = table.Column<bool>(type: "boolean", nullable: false),
                    ConformityDeclaration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OtherRequirements = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubContract = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SamplingRequirement = table.Column<int>(type: "integer", nullable: false),
                    DeliveryDirectPickup = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryPostal = table.Column<bool>(type: "boolean", nullable: false),
                    ReturnStoredSample = table.Column<bool>(type: "boolean", nullable: false),
                    SampleTypeService = table.Column<bool>(type: "boolean", nullable: false),
                    SampleTypeContract = table.Column<bool>(type: "boolean", nullable: false),
                    SampleTypeTask = table.Column<bool>(type: "boolean", nullable: false),
                    SampleTypeSubContract = table.Column<bool>(type: "boolean", nullable: false),
                    ResultDeliveryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SampleStorage = table.Column<int>(type: "integer", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCostInWords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReceiptNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupervisorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DirectorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeRequest = table.Column<string>(type: "text", nullable: true),
                    AgreeTerms = table.Column<bool>(type: "boolean", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResultRejectionReason = table.Column<string>(type: "text", nullable: true),
                    ExpectedCompletionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Subcontractor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SampleSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PendingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PendingByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PendingByUserName = table.Column<string>(type: "text", nullable: true),
                    SampleSentByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SampleSentByUserName = table.Column<string>(type: "text", nullable: true),
                    InProgressDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    InProgressByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InProgressByUserName = table.Column<string>(type: "text", nullable: true),
                    PendingApprovalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PendingApprovalByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PendingApprovalByUserName = table.Column<string>(type: "text", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByUserName = table.Column<string>(type: "text", nullable: true),
                    ResultSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResultSentByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultSentByUserName = table.Column<string>(type: "text", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedByUserName = table.Column<string>(type: "text", nullable: true),
                    PostalDeliveryImageFileIds = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_AppTestRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientSecret = table.Column<string>(type: "text", nullable: true),
                    ClientType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConsentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    DisplayNames = table.Column<string>(type: "text", nullable: true),
                    JsonWebKeySet = table.Column<string>(type: "text", nullable: true),
                    Permissions = table.Column<string>(type: "text", nullable: true),
                    PostLogoutRedirectUris = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    RedirectUris = table.Column<string>(type: "text", nullable: true),
                    Requirements = table.Column<string>(type: "text", nullable: true),
                    Settings = table.Column<string>(type: "text", nullable: true),
                    ClientUri = table.Column<string>(type: "text", nullable: true),
                    LogoUri = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_OpenIddictApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Descriptions = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    DisplayNames = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    Resources = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_OpenIddictScopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpAuditLogActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuditLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MethodName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Parameters = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExecutionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExecutionDuration = table.Column<int>(type: "integer", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpAuditLogActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpAuditLogActions_AbpAuditLogs_AuditLogId",
                        column: x => x.AuditLogId,
                        principalTable: "AbpAuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpEntityChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ChangeType = table.Column<byte>(type: "smallint", nullable: false),
                    EntityTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EntityTypeFullName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEntityChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpEntityChanges_AbpAuditLogs_AuditLogId",
                        column: x => x.AuditLogId,
                        principalTable: "AbpAuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpOrganizationUnitRoles",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpOrganizationUnitRoles", x => new { x.OrganizationUnitId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AbpOrganizationUnitRoles_AbpOrganizationUnits_OrganizationU~",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbpOrganizationUnitRoles_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpRoleClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClaimValue = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpRoleClaims_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpTenantConnectionStrings",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpTenantConnectionStrings", x => new { x.TenantId, x.Name });
                    table.ForeignKey(
                        name: "FK_AbpTenantConnectionStrings_AbpTenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "AbpTenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClaimValue = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUserClaims_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserLogins",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(196)", maxLength: 196, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserLogins", x => new { x.UserId, x.LoginProvider });
                    table.ForeignKey(
                        name: "FK_AbpUserLogins_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserOrganizationUnits",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserOrganizationUnits", x => new { x.OrganizationUnitId, x.UserId });
                    table.ForeignKey(
                        name: "FK_AbpUserOrganizationUnits_AbpOrganizationUnits_OrganizationU~",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbpUserOrganizationUnits_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AbpUserRoles_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbpUserRoles_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AbpUserTokens_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSubcontractorCertifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IssuedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SubcontractorId1 = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_AppSubcontractorCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSubcontractorCertifications_AppSubcontractors_Subcontrac~",
                        column: x => x.SubcontractorId,
                        principalTable: "AppSubcontractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppSubcontractorCertifications_AppSubcontractors_Subcontra~1",
                        column: x => x.SubcontractorId1,
                        principalTable: "AppSubcontractors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppLaboratoryTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalSampleCount = table.Column<int>(type: "integer", nullable: false),
                    TechnicalManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RequestCompletionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultRejectionReason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_AppLaboratoryTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTasks_AppLaboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "AppLaboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTasks_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppQualityManagementTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaskType = table.Column<int>(type: "integer", nullable: false),
                    TotalSampleCount = table.Column<int>(type: "integer", nullable: false),
                    TotalIndicatorCount = table.Column<int>(type: "integer", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RequestCompletionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ResultSentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChangeHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedQualityManagerId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_AppQualityManagementTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQualityManagementTasks_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSpecimens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorageLocation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Images = table.Column<string>(type: "text", nullable: true),
                    Deviation = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_AppSpecimens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSpecimens_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestAttachments_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestChangeHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestChangeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestChangeHistories_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    SampleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SampleName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SamplingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SampleQuantity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusText = table.Column<string>(type: "text", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StorageLocation = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TotalIndicatorCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SpecimenTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    NeedDisposal = table.Column<bool>(type: "boolean", nullable: false),
                    NeedDisposalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
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
                    table.PrimaryKey("PK_AppTestRequestSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSamples_AppTestRequests_TestRequestId",
                        column: x => x.TestRequestId,
                        principalTable: "AppTestRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    Scopes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictAuthorizations_OpenIddictApplications_Application~",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpEntityPropertyChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityChangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OriginalValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PropertyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PropertyTypeFullName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEntityPropertyChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpEntityPropertyChanges_AbpEntityChanges_EntityChangeId",
                        column: x => x.EntityChangeId,
                        principalTable: "AbpEntityChanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppLaboratoryTaskSupporters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLaboratoryTaskSupporters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskSupporters_AppLaboratoryTasks_LaboratoryTa~",
                        column: x => x.LaboratoryTaskId,
                        principalTable: "AppLaboratoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSampleTestingImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ImageType = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSampleTestingImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSampleTestingImages_AppLaboratoryTasks_LaboratoryTaskId",
                        column: x => x.LaboratoryTaskId,
                        principalTable: "AppLaboratoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSpecimenHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecimenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Temperature = table.Column<decimal>(type: "numeric", nullable: true),
                    Humidity = table.Column<decimal>(type: "numeric", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSpecimenHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSpecimenHistories_AppSpecimens_SpecimenId",
                        column: x => x.SpecimenId,
                        principalTable: "AppSpecimens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecimenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Method = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Standard = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignmentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EstimatedDuration = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_AppTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTests_AppSpecimens_SpecimenId",
                        column: x => x.SpecimenId,
                        principalTable: "AppSpecimens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestSampleAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestSampleAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleAttachments_AppTestRequestSamples_TestR~",
                        column: x => x.TestRequestSampleId,
                        principalTable: "AppTestRequestSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTestRequestSampleIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    TestMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TransferDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RequestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SentToDepartment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTestRequestSampleIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleIndicators_AppIndicators_IndicatorId",
                        column: x => x.IndicatorId,
                        principalTable: "AppIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleIndicators_AppLaboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "AppLaboratories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleIndicators_AppTestMethods_TestMethodId",
                        column: x => x.TestMethodId,
                        principalTable: "AppTestMethods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppTestRequestSampleIndicators_AppTestRequestSamples_TestRe~",
                        column: x => x.TestRequestSampleId,
                        principalTable: "AppTestRequestSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    RedemptionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "OpenIddictAuthorizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppTestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Parameter = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LowerLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    UpperLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    IsPass = table.Column<bool>(type: "boolean", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    EnteredById = table.Column<Guid>(type: "uuid", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "text", nullable: true),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovalComment = table.Column<string>(type: "text", nullable: true),
                    ChartData = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_AppTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppTestResults_AppTests_TestId",
                        column: x => x.TestId,
                        principalTable: "AppTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppLaboratoryTaskIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestRequestSampleIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Result = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubmitDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EnteredById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSentToQuality = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLaboratoryTaskIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskIndicators_AppLaboratoryTasks_LaboratoryTa~",
                        column: x => x.LaboratoryTaskId,
                        principalTable: "AppLaboratoryTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppLaboratoryTaskIndicators_AppTestRequestSampleIndicators_~",
                        column: x => x.TestRequestSampleIndicatorId,
                        principalTable: "AppTestRequestSampleIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppReportTestResults",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestResultId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReportTestResults", x => new { x.ReportId, x.TestResultId });
                    table.ForeignKey(
                        name: "FK_AppReportTestResults_AppReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "AppReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppReportTestResults_AppTestResults_TestResultId",
                        column: x => x.TestResultId,
                        principalTable: "AppTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppIndicatorRejectionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RejectedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RejectedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIndicatorRejectionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppIndicatorRejectionHistories_AppLaboratoryTaskIndicators_~",
                        column: x => x.IndicatorId,
                        principalTable: "AppLaboratoryTaskIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppQualityManagementIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityManagementTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaboratoryTaskIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceLimit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQualityManagementIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQualityManagementIndicators_AppLaboratoryTaskIndicators_~",
                        column: x => x.LaboratoryTaskIndicatorId,
                        principalTable: "AppLaboratoryTaskIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppQualityManagementIndicators_AppQualityManagementTasks_Qu~",
                        column: x => x.QualityManagementTaskId,
                        principalTable: "AppQualityManagementTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppQualityIndicatorStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityManagementIndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppQualityIndicatorStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppQualityIndicatorStatusHistories_AppQualityManagementIndi~",
                        column: x => x.QualityManagementIndicatorId,
                        principalTable: "AppQualityManagementIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbpAuditLogActions_AuditLogId",
                table: "AbpAuditLogActions",
                column: "AuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpAuditLogActions_TenantId_ServiceName_MethodName_Executio~",
                table: "AbpAuditLogActions",
                columns: new[] { "TenantId", "ServiceName", "MethodName", "ExecutionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpAuditLogs_TenantId_ExecutionTime",
                table: "AbpAuditLogs",
                columns: new[] { "TenantId", "ExecutionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpAuditLogs_TenantId_UserId_ExecutionTime",
                table: "AbpAuditLogs",
                columns: new[] { "TenantId", "UserId", "ExecutionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpBackgroundJobs_IsAbandoned_NextTryTime",
                table: "AbpBackgroundJobs",
                columns: new[] { "IsAbandoned", "NextTryTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityChanges_AuditLogId",
                table: "AbpEntityChanges",
                column: "AuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityChanges_TenantId_EntityTypeFullName_EntityId",
                table: "AbpEntityChanges",
                columns: new[] { "TenantId", "EntityTypeFullName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityPropertyChanges_EntityChangeId",
                table: "AbpEntityPropertyChanges",
                column: "EntityChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpFeatureGroups_Name",
                table: "AbpFeatureGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpFeatures_GroupName",
                table: "AbpFeatures",
                column: "GroupName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpFeatures_Name",
                table: "AbpFeatures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpFeatureValues_Name_ProviderName_ProviderKey",
                table: "AbpFeatureValues",
                columns: new[] { "Name", "ProviderName", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpLinkUsers_SourceUserId_SourceTenantId_TargetUserId_Targe~",
                table: "AbpLinkUsers",
                columns: new[] { "SourceUserId", "SourceTenantId", "TargetUserId", "TargetTenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpOrganizationUnitRoles_RoleId_OrganizationUnitId",
                table: "AbpOrganizationUnitRoles",
                columns: new[] { "RoleId", "OrganizationUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpOrganizationUnits_Code",
                table: "AbpOrganizationUnits",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_AbpOrganizationUnits_ParentId",
                table: "AbpOrganizationUnits",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpPermissionGrants_TenantId_Name_ProviderName_ProviderKey",
                table: "AbpPermissionGrants",
                columns: new[] { "TenantId", "Name", "ProviderName", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpPermissionGroups_Name",
                table: "AbpPermissionGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpPermissions_GroupName",
                table: "AbpPermissions",
                column: "GroupName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpPermissions_Name",
                table: "AbpPermissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoleClaims_RoleId",
                table: "AbpRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoles_NormalizedName",
                table: "AbpRoles",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpSecurityLogs_TenantId_Action",
                table: "AbpSecurityLogs",
                columns: new[] { "TenantId", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpSecurityLogs_TenantId_ApplicationName",
                table: "AbpSecurityLogs",
                columns: new[] { "TenantId", "ApplicationName" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpSecurityLogs_TenantId_Identity",
                table: "AbpSecurityLogs",
                columns: new[] { "TenantId", "Identity" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpSecurityLogs_TenantId_UserId",
                table: "AbpSecurityLogs",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpSessions_Device",
                table: "AbpSessions",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_AbpSessions_SessionId",
                table: "AbpSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpSessions_TenantId_UserId",
                table: "AbpSessions",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpSettingDefinitions_Name",
                table: "AbpSettingDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpSettings_Name_ProviderName_ProviderKey",
                table: "AbpSettings",
                columns: new[] { "Name", "ProviderName", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_Name",
                table: "AbpTenants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_NormalizedName",
                table: "AbpTenants",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserClaims_UserId",
                table: "AbpUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLogins_LoginProvider_ProviderKey",
                table: "AbpUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserOrganizationUnits_UserId_OrganizationUnitId",
                table: "AbpUserOrganizationUnits",
                columns: new[] { "UserId", "OrganizationUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserRoles_RoleId_UserId",
                table: "AbpUserRoles",
                columns: new[] { "RoleId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_Email",
                table: "AbpUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_NormalizedEmail",
                table: "AbpUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_NormalizedUserName",
                table: "AbpUsers",
                column: "NormalizedUserName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_UserName",
                table: "AbpUsers",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomers_Code",
                table: "AppCustomers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppDepartments_Code",
                table: "AppDepartments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorGroups_Name",
                table: "AppIndicatorGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorRejectionHistories_IndicatorId",
                table: "AppIndicatorRejectionHistories",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicatorRejectionHistories_RejectedDate",
                table: "AppIndicatorRejectionHistories",
                column: "RejectedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_Code",
                table: "AppIndicators",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_IndicatorGroupId",
                table: "AppIndicators",
                column: "IndicatorGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIndicators_IsActive",
                table: "AppIndicators",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratories_Code",
                table: "AppLaboratories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratories_IsActive",
                table: "AppLaboratories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratories_Name",
                table: "AppLaboratories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_ApprovalStatus",
                table: "AppLaboratoryTaskIndicators",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_LaboratoryTaskId",
                table: "AppLaboratoryTaskIndicators",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_MethodId",
                table: "AppLaboratoryTaskIndicators",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_TechnicianId",
                table: "AppLaboratoryTaskIndicators",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskIndicators_TestRequestSampleIndicatorId",
                table: "AppLaboratoryTaskIndicators",
                column: "TestRequestSampleIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_LaboratoryId",
                table: "AppLaboratoryTasks",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_RequestCompletionDate",
                table: "AppLaboratoryTasks",
                column: "RequestCompletionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_Status",
                table: "AppLaboratoryTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_TechnicalManagerId",
                table: "AppLaboratoryTasks",
                column: "TechnicalManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_TestRequestId",
                table: "AppLaboratoryTasks",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTasks_TransferDate",
                table: "AppLaboratoryTasks",
                column: "TransferDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSupporters_LaboratoryTaskId",
                table: "AppLaboratoryTaskSupporters",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSupporters_LaboratoryTaskId_UserId",
                table: "AppLaboratoryTaskSupporters",
                columns: new[] { "LaboratoryTaskId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppLaboratoryTaskSupporters_UserId",
                table: "AppLaboratoryTaskSupporters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_AssignedToId",
                table: "AppNCRs",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_NCRNumber",
                table: "AppNCRs",
                column: "NCRNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_RelatedEntityId",
                table: "AppNCRs",
                column: "RelatedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_Severity",
                table: "AppNCRs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AppNCRs_Status",
                table: "AppNCRs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_CreationTime",
                table: "AppNotifications",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_UserId",
                table: "AppNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppNotifications_UserId_IsRead",
                table: "AppNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOrganizations_IsActive",
                table: "AppOrganizations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrganizations_Name",
                table: "AppOrganizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityIndicatorStatusHistories_CreationTime",
                table: "AppQualityIndicatorStatusHistories",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityIndicatorStatusHistories_QualityManagementIndicat~",
                table: "AppQualityIndicatorStatusHistories",
                column: "QualityManagementIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_ApprovalStatus",
                table: "AppQualityManagementIndicators",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_LaboratoryTaskIndicatorId",
                table: "AppQualityManagementIndicators",
                column: "LaboratoryTaskIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_QualityManagementTaskId",
                table: "AppQualityManagementIndicators",
                column: "QualityManagementTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementIndicators_StandardId",
                table: "AppQualityManagementIndicators",
                column: "StandardId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_ChangeHistoryId",
                table: "AppQualityManagementTasks",
                column: "ChangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_ContractNumber",
                table: "AppQualityManagementTasks",
                column: "ContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_RequestCompletionDate",
                table: "AppQualityManagementTasks",
                column: "RequestCompletionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_Status",
                table: "AppQualityManagementTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_TaskType",
                table: "AppQualityManagementTasks",
                column: "TaskType");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_TestRequestId",
                table: "AppQualityManagementTasks",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppQualityManagementTasks_TransferDate",
                table: "AppQualityManagementTasks",
                column: "TransferDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_ReportNumber",
                table: "AppReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_Status",
                table: "AppReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppReports_TestRequestId",
                table: "AppReports",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReportTestResults_ReportId",
                table: "AppReportTestResults",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AppReportTestResults_TestResultId",
                table: "AppReportTestResults",
                column: "TestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSampleTestingImages_LaboratoryTaskId",
                table: "AppSampleTestingImages",
                column: "LaboratoryTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSampleTestingImages_LaboratoryTaskId_SampleId_ImageType",
                table: "AppSampleTestingImages",
                columns: new[] { "LaboratoryTaskId", "SampleId", "ImageType" });

            migrationBuilder.CreateIndex(
                name: "IX_AppSampleTestingImages_SampleId",
                table: "AppSampleTestingImages",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenHistories_SpecimenId",
                table: "AppSpecimenHistories",
                column: "SpecimenId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimens_Code",
                table: "AppSpecimens",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimens_Status",
                table: "AppSpecimens",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimens_TestRequestId",
                table: "AppSpecimens",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSpecimenTypes_Code",
                table: "AppSpecimenTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_IndicatorId",
                table: "AppStandardIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_StandardId",
                table: "AppStandardIndicators",
                column: "StandardId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_StandardId_IndicatorId_TestMethodId",
                table: "AppStandardIndicators",
                columns: new[] { "StandardId", "IndicatorId", "TestMethodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStandardIndicators_TestMethodId",
                table: "AppStandardIndicators",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppStandards_Code",
                table: "AppStandards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppStandards_IsActive",
                table: "AppStandards",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppStorageLocations_Code",
                table: "AppStorageLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractorCertifications_ExpiryDate",
                table: "AppSubcontractorCertifications",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractorCertifications_SubcontractorId",
                table: "AppSubcontractorCertifications",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractorCertifications_SubcontractorId1",
                table: "AppSubcontractorCertifications",
                column: "SubcontractorId1");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractors_Code",
                table: "AppSubcontractors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSubcontractors_IsActive",
                table: "AppSubcontractors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_AssignedById",
                table: "AppTaskAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_AssignedToId",
                table: "AppTaskAssignments",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_DueDate",
                table: "AppTaskAssignments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_Status",
                table: "AppTaskAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppTaskAssignments_TaskNumber",
                table: "AppTaskAssignments",
                column: "TaskNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_IndicatorId",
                table: "AppTestMethodIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId",
                table: "AppTestMethodIndicators",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethodIndicators_TestMethodId_IndicatorId",
                table: "AppTestMethodIndicators",
                columns: new[] { "TestMethodId", "IndicatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_Code",
                table: "AppTestMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestMethods_IsActive",
                table: "AppTestMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestAttachments_TestRequestId",
                table: "AppTestRequestAttachments",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestChangeHistories_ApprovalStatus",
                table: "AppTestRequestChangeHistories",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestChangeHistories_CreationTime",
                table: "AppTestRequestChangeHistories",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestChangeHistories_TestRequestId",
                table: "AppTestRequestChangeHistories",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestIndicators_IndicatorId",
                table: "AppTestRequestIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestIndicators_TestRequestId",
                table: "AppTestRequestIndicators",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestIndicators_TestRequestId_IndicatorId",
                table: "AppTestRequestIndicators",
                columns: new[] { "TestRequestId", "IndicatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequests_ContractNumber",
                table: "AppTestRequests",
                column: "ContractNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequests_RequestDate",
                table: "AppTestRequests",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequests_RequestNumber",
                table: "AppTestRequests",
                column: "RequestNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequests_Status",
                table: "AppTestRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleAttachments_FileId",
                table: "AppTestRequestSampleAttachments",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleAttachments_TestRequestSampleId",
                table: "AppTestRequestSampleAttachments",
                column: "TestRequestSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_IndicatorId",
                table: "AppTestRequestSampleIndicators",
                column: "IndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_LaboratoryId",
                table: "AppTestRequestSampleIndicators",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_TestMethodId",
                table: "AppTestRequestSampleIndicators",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSampleIndicators_TestRequestSampleId",
                table: "AppTestRequestSampleIndicators",
                column: "TestRequestSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSamples_SampleCode",
                table: "AppTestRequestSamples",
                column: "SampleCode");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestRequestSamples_TestRequestId",
                table: "AppTestRequestSamples",
                column: "TestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResults_ApprovalStatus",
                table: "AppTestResults",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppTestResults_TestId",
                table: "AppTestResults",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTests_AssigneeId",
                table: "AppTests",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTests_Code",
                table: "AppTests",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_AppTests_SpecimenId",
                table: "AppTests",
                column: "SpecimenId");

            migrationBuilder.CreateIndex(
                name: "IX_AppTests_Status",
                table: "AppTests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictApplications_ClientId",
                table: "OpenIddictApplications",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type",
                table: "OpenIddictAuthorizations",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictScopes_Name",
                table: "OpenIddictScopes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type",
                table: "OpenIddictTokens",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_AuthorizationId",
                table: "OpenIddictTokens",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ReferenceId",
                table: "OpenIddictTokens",
                column: "ReferenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbpAuditLogActions");

            migrationBuilder.DropTable(
                name: "AbpBackgroundJobs");

            migrationBuilder.DropTable(
                name: "AbpClaimTypes");

            migrationBuilder.DropTable(
                name: "AbpEntityPropertyChanges");

            migrationBuilder.DropTable(
                name: "AbpFeatureGroups");

            migrationBuilder.DropTable(
                name: "AbpFeatures");

            migrationBuilder.DropTable(
                name: "AbpFeatureValues");

            migrationBuilder.DropTable(
                name: "AbpLinkUsers");

            migrationBuilder.DropTable(
                name: "AbpOrganizationUnitRoles");

            migrationBuilder.DropTable(
                name: "AbpPermissionGrants");

            migrationBuilder.DropTable(
                name: "AbpPermissionGroups");

            migrationBuilder.DropTable(
                name: "AbpPermissions");

            migrationBuilder.DropTable(
                name: "AbpRoleClaims");

            migrationBuilder.DropTable(
                name: "AbpSecurityLogs");

            migrationBuilder.DropTable(
                name: "AbpSessions");

            migrationBuilder.DropTable(
                name: "AbpSettingDefinitions");

            migrationBuilder.DropTable(
                name: "AbpSettings");

            migrationBuilder.DropTable(
                name: "AbpTenantConnectionStrings");

            migrationBuilder.DropTable(
                name: "AbpUserClaims");

            migrationBuilder.DropTable(
                name: "AbpUserDelegations");

            migrationBuilder.DropTable(
                name: "AbpUserLogins");

            migrationBuilder.DropTable(
                name: "AbpUserOrganizationUnits");

            migrationBuilder.DropTable(
                name: "AbpUserRoles");

            migrationBuilder.DropTable(
                name: "AbpUserTokens");

            migrationBuilder.DropTable(
                name: "AppCustomers");

            migrationBuilder.DropTable(
                name: "AppDepartments");

            migrationBuilder.DropTable(
                name: "AppIndicatorGroups");

            migrationBuilder.DropTable(
                name: "AppIndicatorRejectionHistories");

            migrationBuilder.DropTable(
                name: "AppLaboratoryTaskSupporters");

            migrationBuilder.DropTable(
                name: "AppNCRs");

            migrationBuilder.DropTable(
                name: "AppNotifications");

            migrationBuilder.DropTable(
                name: "AppOrganizations");

            migrationBuilder.DropTable(
                name: "AppQualityIndicatorStatusHistories");

            migrationBuilder.DropTable(
                name: "AppReportTestResults");

            migrationBuilder.DropTable(
                name: "AppSampleTestingImages");

            migrationBuilder.DropTable(
                name: "AppSpecimenHistories");

            migrationBuilder.DropTable(
                name: "AppSpecimenTypes");

            migrationBuilder.DropTable(
                name: "AppStandardIndicators");

            migrationBuilder.DropTable(
                name: "AppStandards");

            migrationBuilder.DropTable(
                name: "AppStorageLocations");

            migrationBuilder.DropTable(
                name: "AppSubcontractorCertifications");

            migrationBuilder.DropTable(
                name: "AppTaskAssignments");

            migrationBuilder.DropTable(
                name: "AppTestMethodIndicators");

            migrationBuilder.DropTable(
                name: "AppTestRequestAttachments");

            migrationBuilder.DropTable(
                name: "AppTestRequestChangeHistories");

            migrationBuilder.DropTable(
                name: "AppTestRequestIndicators");

            migrationBuilder.DropTable(
                name: "AppTestRequestSampleAttachments");

            migrationBuilder.DropTable(
                name: "OpenIddictScopes");

            migrationBuilder.DropTable(
                name: "OpenIddictTokens");

            migrationBuilder.DropTable(
                name: "AbpEntityChanges");

            migrationBuilder.DropTable(
                name: "AbpTenants");

            migrationBuilder.DropTable(
                name: "AbpOrganizationUnits");

            migrationBuilder.DropTable(
                name: "AbpRoles");

            migrationBuilder.DropTable(
                name: "AbpUsers");

            migrationBuilder.DropTable(
                name: "AppQualityManagementIndicators");

            migrationBuilder.DropTable(
                name: "AppReports");

            migrationBuilder.DropTable(
                name: "AppTestResults");

            migrationBuilder.DropTable(
                name: "AppSubcontractors");

            migrationBuilder.DropTable(
                name: "OpenIddictAuthorizations");

            migrationBuilder.DropTable(
                name: "AbpAuditLogs");

            migrationBuilder.DropTable(
                name: "AppLaboratoryTaskIndicators");

            migrationBuilder.DropTable(
                name: "AppQualityManagementTasks");

            migrationBuilder.DropTable(
                name: "AppTests");

            migrationBuilder.DropTable(
                name: "OpenIddictApplications");

            migrationBuilder.DropTable(
                name: "AppLaboratoryTasks");

            migrationBuilder.DropTable(
                name: "AppTestRequestSampleIndicators");

            migrationBuilder.DropTable(
                name: "AppSpecimens");

            migrationBuilder.DropTable(
                name: "AppIndicators");

            migrationBuilder.DropTable(
                name: "AppLaboratories");

            migrationBuilder.DropTable(
                name: "AppTestMethods");

            migrationBuilder.DropTable(
                name: "AppTestRequestSamples");

            migrationBuilder.DropTable(
                name: "AppTestRequests");
        }
    }
}
