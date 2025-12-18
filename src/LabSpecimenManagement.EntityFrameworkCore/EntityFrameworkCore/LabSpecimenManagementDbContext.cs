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
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.Subcontractors;

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
    public DbSet<TestRequestSample> TestRequestSamples { get; set; }
    public DbSet<TestRequestSampleIndicator> TestRequestSampleIndicators { get; set; }
    public DbSet<Specimen> Specimens { get; set; }
    public DbSet<SpecimenHistory> SpecimenHistories { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<TestResult> TestResults { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<ReportTestResult> ReportTestResults { get; set; }

    // Master Data Entities
    public DbSet<Department> Departments { get; set; }
    public DbSet<SpecimenType> SpecimenTypes { get; set; }
    public DbSet<TestMethod> TestMethods { get; set; }
    public DbSet<StorageLocation> StorageLocations { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<IndicatorGroup> IndicatorGroups { get; set; }
    public DbSet<Indicator> Indicators { get; set; }
    public DbSet<Organization> Organizations { get; set; }

    // Junction Tables
    public DbSet<TestRequestIndicator> TestRequestIndicators { get; set; }

    // NCR Entities
    public DbSet<NCR> NCRs { get; set; }

    // Task Entities
    public DbSet<TaskAssignment> TaskAssignments { get; set; }

    // Subcontractor Entities
    public DbSet<Subcontractor> Subcontractors { get; set; }
    public DbSet<SubcontractorCertification> SubcontractorCertifications { get; set; }

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

            // Contract info
            b.Property(x => x.ContractNumber).HasMaxLength(100);
            b.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            b.Property(x => x.Province).HasMaxLength(100);
            b.Property(x => x.District).HasMaxLength(100);
            b.Property(x => x.Ward).HasMaxLength(100);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.TaxCode).HasMaxLength(50);
            b.Property(x => x.ContactPerson).HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(50);

            // Test method info
            b.Property(x => x.MethodCustomerDetail).HasMaxLength(500);
            b.Property(x => x.ResultCopyCount).HasMaxLength(50);
            b.Property(x => x.ConformityDeclaration).HasMaxLength(500);
            b.Property(x => x.OtherRequirements).HasMaxLength(1000);
            b.Property(x => x.SubContract).HasMaxLength(256);
            b.Property(x => x.TotalCost).HasPrecision(18, 2);
            b.Property(x => x.TotalCostInWords).HasMaxLength(500);
            b.Property(x => x.ReceiptNumber).HasMaxLength(100);

            // Legacy fields
            b.Property(x => x.RequestNumber).IsRequired().HasMaxLength(128);
            b.Property(x => x.Subcontractor).HasMaxLength(256);

            b.HasIndex(x => x.RequestNumber);
            b.HasIndex(x => x.ContractNumber);
            b.HasIndex(x => x.RequestDate);
            b.HasIndex(x => x.Status);
        });

        // TestRequestSample configuration
        builder.Entity<TestRequestSample>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestRequestSamples", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.SampleCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.SampleName).IsRequired().HasMaxLength(256);
            b.Property(x => x.SamplingAddress).HasMaxLength(500);
            b.Property(x => x.SampleQuantity).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.Property(x => x.StorageLocation).HasMaxLength(256);
            b.Property(x => x.TotalIndicatorCost).HasPrecision(18, 2);

            b.HasIndex(x => x.SampleCode);
            b.HasIndex(x => x.TestRequestId);

            b.HasOne(x => x.TestRequest)
                .WithMany(x => x.Samples)
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired();
        });

        // TestRequestSampleIndicator configuration
        builder.Entity<TestRequestSampleIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestRequestSampleIndicators", LabSpecimenManagementConsts.DbSchema);

            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.Notes).HasMaxLength(500);

            b.HasIndex(x => x.TestRequestSampleId);
            b.HasIndex(x => x.IndicatorId);

            b.HasOne(x => x.TestRequestSample)
                .WithMany(x => x.Indicators)
                .HasForeignKey(x => x.TestRequestSampleId)
                .IsRequired();

            b.HasOne(x => x.Indicator)
                .WithMany()
                .HasForeignKey(x => x.IndicatorId)
                .IsRequired();
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

        // ============ MASTER DATA CONFIGURATIONS ============

        // Department configuration
        builder.Entity<Department>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Departments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);

            b.HasIndex(x => x.Code).IsUnique();
        });

        // SpecimenType configuration
        builder.Entity<SpecimenType>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SpecimenTypes", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.DefaultStorageCondition).HasMaxLength(256);

            b.HasIndex(x => x.Code).IsUnique();
        });

        // TestMethod configuration
        builder.Entity<TestMethod>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestMethods", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Standard).HasMaxLength(256);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.DepartmentId);
        });

        // StorageLocation configuration
        builder.Entity<StorageLocation>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "StorageLocations", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Temperature).HasMaxLength(50);
            b.Property(x => x.Humidity).HasMaxLength(50);

            b.HasIndex(x => x.Code).IsUnique();
        });

        // Customer configuration
        builder.Entity<Customer>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Customers", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Address).HasMaxLength(512);
            b.Property(x => x.TaxCode).HasMaxLength(50);
            b.Property(x => x.ContactPerson).HasMaxLength(256);

            b.HasIndex(x => x.Code).IsUnique();
        });

        // IndicatorGroup configuration
        builder.Entity<IndicatorGroup>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "IndicatorGroups", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);

            b.HasIndex(x => x.Name).IsUnique();
        });

        // Indicator configuration
        builder.Entity<Indicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Indicators", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.IndicatorGroupId);
            b.HasIndex(x => x.IsActive);
        });

        // Organization configuration
        builder.Entity<Organization>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Organizations", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);

            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.IsActive);
        });

        // TestRequestIndicator configuration (Junction Table)
        builder.Entity<TestRequestIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestRequestIndicators", LabSpecimenManagementConsts.DbSchema);

            b.HasIndex(x => x.TestRequestId);
            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => new { x.TestRequestId, x.IndicatorId }).IsUnique();
        });

        // ============ NCR CONFIGURATION ============

        builder.Entity<NCR>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "NCRs", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.NCRNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.RootCause).HasMaxLength(2000);
            b.Property(x => x.CorrectiveAction).HasMaxLength(2000);
            b.Property(x => x.PreventiveAction).HasMaxLength(2000);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.Department).HasMaxLength(256);
            b.Property(x => x.RelatedEntityType).HasMaxLength(100);
            b.Property(x => x.RelatedEntityCode).HasMaxLength(100);

            b.HasIndex(x => x.NCRNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Severity);
            b.HasIndex(x => x.RelatedEntityId);
            b.HasIndex(x => x.AssignedToId);
        });

        // ============ REPORT CONFIGURATION ============

        builder.Entity<Report>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Reports", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ReportNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            b.Property(x => x.PdfPath).HasMaxLength(512);
            b.Property(x => x.QRCode).HasMaxLength(512);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.ReportNumber).IsUnique();
            b.HasIndex(x => x.TestRequestId);
            b.HasIndex(x => x.Status);
        });

        // ============ REPORT TEST RESULT CONFIGURATION (Junction Table) ============

        builder.Entity<ReportTestResult>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ReportTestResults", LabSpecimenManagementConsts.DbSchema);
            
            // Configure composite primary key
            b.HasKey(x => new { x.ReportId, x.TestResultId });

            // Configure relationships
            b.HasOne(x => x.Report)
                .WithMany()
                .HasForeignKey(x => x.ReportId)
                .IsRequired();

            b.HasOne(x => x.TestResult)
                .WithMany()
                .HasForeignKey(x => x.TestResultId)
                .IsRequired();

            b.HasIndex(x => x.ReportId);
            b.HasIndex(x => x.TestResultId);
        });

        // ============ TASK ASSIGNMENT CONFIGURATION ============

        builder.Entity<TaskAssignment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TaskAssignments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TaskNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Department).IsRequired().HasMaxLength(256);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.TaskNumber).IsUnique();
            b.HasIndex(x => x.AssignedToId);
            b.HasIndex(x => x.AssignedById);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.DueDate);
        });

        // ============ SUBCONTRACTOR CONFIGURATIONS ============

        builder.Entity<Subcontractor>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Subcontractors", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Address).HasMaxLength(512);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.ContactPerson).HasMaxLength(256);
            b.Property(x => x.TaxCode).HasMaxLength(50);
            b.Property(x => x.Capabilities).HasMaxLength(2000);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.IsActive);
        });

        builder.Entity<SubcontractorCertification>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SubcontractorCertifications", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.CertificateNumber).HasMaxLength(100);
            b.Property(x => x.IssuedBy).IsRequired().HasMaxLength(256);
            b.Property(x => x.FilePath).HasMaxLength(512);

            b.HasIndex(x => x.SubcontractorId);
            b.HasIndex(x => x.ExpiryDate);

            b.HasOne<Subcontractor>()
                .WithMany(x => x.Certifications)
                .HasForeignKey(x => x.SubcontractorId)
                .IsRequired();
        });
    }
}
