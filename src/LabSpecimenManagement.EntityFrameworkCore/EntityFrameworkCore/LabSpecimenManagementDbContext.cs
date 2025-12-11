using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class LabSpecimenManagementDbContext :
    AbpDbContext<LabSpecimenManagementDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    // Lab Specimen Management Entities
    public DbSet<TestRequest> TestRequests { get; set; }
    public DbSet<TestRequestAttachment> TestRequestAttachments { get; set; }
    public DbSet<Specimen> Specimens { get; set; }
    public DbSet<SpecimenHistory> SpecimenHistories { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<TestResult> TestResults { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public LabSpecimenManagementDbContext(DbContextOptions<LabSpecimenManagementDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        // TestRequest configuration
        builder.Entity<TestRequest>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestRequests", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.RequestNumber).IsRequired().HasMaxLength(128);
            b.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            b.Property(x => x.CustomerEmail).HasMaxLength(256);
            b.Property(x => x.CustomerPhone).HasMaxLength(50);
            b.Property(x => x.Subcontractor).HasMaxLength(256);

            b.HasIndex(x => x.RequestNumber);
            b.HasIndex(x => x.RequestDate);
            b.HasIndex(x => x.Status);
        });

        // TestRequestAttachment configuration
        builder.Entity<TestRequestAttachment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestRequestAttachments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(512);
            b.Property(x => x.FileType).HasMaxLength(50);

            b.HasOne(x => x.TestRequest)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired();
        });

        // Specimen configuration
        builder.Entity<Specimen>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Specimens", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(128);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Type).HasMaxLength(128);
            b.Property(x => x.StorageLocation).HasMaxLength(256);
            b.Property(x => x.Unit).HasMaxLength(50);

            b.HasIndex(x => x.Code);
            b.HasIndex(x => x.Status);

            b.HasOne(x => x.TestRequest)
                .WithMany(x => x.Specimens)
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired();
        });

        // SpecimenHistory configuration
        builder.Entity<SpecimenHistory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SpecimenHistories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Action).IsRequired().HasMaxLength(256);
            b.Property(x => x.Location).HasMaxLength(256);

            b.HasOne(x => x.Specimen)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.SpecimenId)
                .IsRequired();
        });

        // Test configuration
        builder.Entity<Test>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Tests", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(128);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Method).HasMaxLength(256);
            b.Property(x => x.Standard).HasMaxLength(256);

            b.HasIndex(x => x.Code);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.AssigneeId);

            b.HasOne(x => x.Specimen)
                .WithMany(x => x.Tests)
                .HasForeignKey(x => x.SpecimenId)
                .IsRequired();
        });

        // TestResult configuration
        builder.Entity<TestResult>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestResults", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Parameter).IsRequired().HasMaxLength(256);
            b.Property(x => x.Value).IsRequired().HasMaxLength(512);
            b.Property(x => x.Unit).HasMaxLength(50);

            b.HasIndex(x => x.ApprovalStatus);

            b.HasOne(x => x.Test)
                .WithMany(x => x.Results)
                .HasForeignKey(x => x.TestId)
                .IsRequired();
        });
    }
}
