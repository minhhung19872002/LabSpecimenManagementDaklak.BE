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
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.Quotations;
using LabSpecimenManagement.Receipts;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.Suppliers;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.ApprovalProcesses;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.StockAdjustments;
using LabSpecimenManagement.PublishedReports;
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.WarehouseImports;

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
    public DbSet<TestRequestSampleAttachment> TestRequestSampleAttachments { get; set; }
    public DbSet<TestRequestSampleIndicator> TestRequestSampleIndicators { get; set; }
    public DbSet<SampleIndicatorNote> SampleIndicatorNotes { get; set; }
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
    public DbSet<TestMethodIndicator> TestMethodIndicators { get; set; }
    public DbSet<StorageLocation> StorageLocations { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<IndicatorGroup> IndicatorGroups { get; set; }
    public DbSet<Indicator> Indicators { get; set; }
    public DbSet<SampleTypeCategory> SampleTypeCategories { get; set; }
    public DbSet<SampleCondition> SampleConditions { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Supplier> SupplierEntities { get; set; }
    public DbSet<ProductAttachment> ProductAttachments { get; set; }
    public DbSet<ProductUnitConversion> ProductUnitConversions { get; set; }
    public DbSet<ProductSku> ProductSkus { get; set; }
    public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public DbSet<GoodsReceiptItem> GoodsReceiptItems { get; set; }
    public DbSet<GoodsReceiptPayment> GoodsReceiptPayments { get; set; }
    public DbSet<WarehouseImportReceipt> WarehouseImportReceipts { get; set; }
    public DbSet<WarehouseImportReceiptItem> WarehouseImportReceiptItems { get; set; }
    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<MaterialConsumptionItem> MaterialConsumptionItems { get; set; }
    public DbSet<StockAdjustment> StockAdjustments { get; set; }
    public DbSet<StockAdjustmentItem> StockAdjustmentItems { get; set; }
    public DbSet<StockMovementLog> StockMovementLogs { get; set; }
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<EquipmentAttachment> EquipmentAttachments { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Laboratory> Laboratories { get; set; }
    public DbSet<Standard> Standards { get; set; }
    public DbSet<StandardIndicator> StandardIndicators { get; set; }
    public DbSet<SpecimenTypeIndicator> SpecimenTypeIndicators { get; set; }

    // Junction Tables
    public DbSet<TestRequestIndicator> TestRequestIndicators { get; set; }

    // NCR Entities
    public DbSet<NCR> NCRs { get; set; }

    // Task Entities
    public DbSet<TaskAssignment> TaskAssignments { get; set; }

    // Subcontractor Entities
    public DbSet<Subcontractor> Subcontractors { get; set; }
    public DbSet<SubcontractorCertification> SubcontractorCertifications { get; set; }

    // Laboratory Task Entities
    public DbSet<LaboratoryTask> LaboratoryTasks { get; set; }
    public DbSet<LaboratoryTaskIndicator> LaboratoryTaskIndicators { get; set; }
    public DbSet<LaboratoryTaskSupporter> LaboratoryTaskSupporters { get; set; }
    public DbSet<LaboratoryDepartmentSupporter> LaboratoryDepartmentSupporters { get; set; }
    public DbSet<LaboratoryTaskSampleReceived> LaboratoryTaskSampleReceiveds { get; set; }
    public DbSet<IndicatorRejectionHistory> IndicatorRejectionHistories { get; set; }
    public DbSet<RejectionHistoryImage> RejectionHistoryImages { get; set; }
    public DbSet<MethodChangeHistory> MethodChangeHistories { get; set; }
    public DbSet<SampleTestingImage> SampleTestingImages { get; set; }
    public DbSet<IndicatorImage> IndicatorImages { get; set; }

    // Change History and Notifications
    public DbSet<TestRequestChangeHistory> TestRequestChangeHistories { get; set; }
    public DbSet<CostChangeHistory> CostChangeHistories { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    // Quality Management Entities
    public DbSet<QualityManagementTask> QualityManagementTasks { get; set; }
    public DbSet<QualityManagementIndicator> QualityManagementIndicators { get; set; }
    public DbSet<QualityIndicatorStatusHistory> QualityIndicatorStatusHistories { get; set; }

    // Quotation Entities
    public DbSet<Quotation> Quotations { get; set; }
    public DbSet<QuotationSample> QuotationSamples { get; set; }
    public DbSet<QuotationSampleIndicator> QuotationSampleIndicators { get; set; }

    // Receipt Entities
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<PaymentHistory> PaymentHistories { get; set; }

    // Published Report Entities
    public DbSet<PublishedReport> PublishedReports { get; set; }

    // Test Result Values (Master Data)
    public DbSet<TestResultValue> TestResultValues { get; set; }
    public DbSet<TestResultValueIndicator> TestResultValueIndicators { get; set; }

    // Notes (Master Data)
    public DbSet<Note> Notes { get; set; }

    // Test Batch Entities
    public DbSet<TestBatch> TestBatches { get; set; }
    public DbSet<TestBatchItem> TestBatchItems { get; set; }
    public DbSet<TestBatchConsumption> TestBatchConsumptions { get; set; }

    // Approval Process Entities
    public DbSet<ApprovalProcess> ApprovalProcesses { get; set; }
    public DbSet<ApprovalProcessStep> ApprovalProcessSteps { get; set; }
    public DbSet<ApprovalSnapshot> ApprovalSnapshots { get; set; }
    public DbSet<ApprovalSnapshotStep> ApprovalSnapshotSteps { get; set; }

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

            // Form print tracking
            b.Property(x => x.LastPrintedIndicatorHash).HasMaxLength(64);

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
            b.Property(x => x.OriginalUnitPrice).HasPrecision(18, 2);
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

            b.HasIndex(x => x.LaboratoryId);
            b.HasOne(x => x.Laboratory)
                .WithMany()
                .HasForeignKey(x => x.LaboratoryId)
                .IsRequired(false);

            b.HasIndex(x => x.TestMethodId);
            b.HasOne(x => x.TestMethod)
                .WithMany()
                .HasForeignKey(x => x.TestMethodId)
                .IsRequired(false);
        });

        // TestRequestSampleAttachment configuration
        builder.Entity<TestRequestSampleAttachment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestRequestSampleAttachments", LabSpecimenManagementConsts.DbSchema);

            b.Property(x => x.FileId).IsRequired().HasMaxLength(100);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(500);

            b.HasIndex(x => x.TestRequestSampleId);
            b.HasIndex(x => x.FileId).IsUnique();

            b.HasOne(x => x.Sample)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.TestRequestSampleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
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

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        // SampleTypeCategory configuration
        builder.Entity<SampleTypeCategory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SampleTypeCategories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => x.IsActive);
        });

        // SampleCondition configuration
        builder.Entity<SampleCondition>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SampleConditions", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.IsActive);
        });

        // Unit configuration
        builder.Entity<Unit>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Units", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Category).HasConversion<int?>();

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.IsActive);
        });

        // Supplier configuration
        builder.Entity<Supplier>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Suppliers", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(500);
            b.Property(x => x.TaxCode).HasMaxLength(50);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.LogoFileId).HasMaxLength(50);
            b.Property(x => x.SupplierGroup).HasMaxLength(256);
            b.Property(x => x.Note).HasMaxLength(1000);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.SupplierGroup);
            b.HasIndex(x => x.IsActive);
        });

        // Product configuration
        builder.Entity<Product>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Products", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(500);
            b.Property(x => x.FormattedName);
            b.Property(x => x.IdentificationCode).HasMaxLength(256);
            b.Property(x => x.PackagingQuantity);
            b.Property(x => x.PackagingUnitId);
            b.Property(x => x.VolumePerUnit);
            b.Property(x => x.VolumeUnitId);
            b.Property(x => x.PackagingSpec);
            b.Property(x => x.CASNumber).HasMaxLength(100);
            b.Property(x => x.Manufacturer).HasMaxLength(256);
            b.Property(x => x.Origin).HasMaxLength(256);
            b.Property(x => x.Concentration).HasMaxLength(256);
            b.Property(x => x.FormattedConcentration);
            b.Property(x => x.FormattedOrigin);
            b.Property(x => x.GhsCode).HasMaxLength(128);
            b.Property(x => x.StandardCertificationType);
            b.Property(x => x.FormattedChemicalFormula);
            b.Property(x => x.CertifiedValue);
            b.Property(x => x.FormattedCertifiedValue);
            b.Property(x => x.MeasurementUncertainty);
            b.Property(x => x.FormattedMeasurementUncertainty);
            b.Property(x => x.StandardLink).HasMaxLength(500);
            b.Property(x => x.AnalysisGrade).HasMaxLength(256);
            b.Property(x => x.FormattedPackagingSpec);
            b.Property(x => x.MaterialComposition);
            b.Property(x => x.FormattedMaterialComposition);
            b.Property(x => x.IsReusable);
            b.Property(x => x.MaxReuseCount);
            b.Property(x => x.IsSterile);
            b.Property(x => x.UsagePurpose);
            b.Property(x => x.ShelfLifeDays);
            b.Property(x => x.BaseProductId);
            b.Property(x => x.FormattedStorageConditions);
            b.Property(x => x.ChemicalFormula).HasMaxLength(256);
            b.Property(x => x.MinStockLevel).HasPrecision(18, 4);

            b.HasMany(x => x.Skus).WithOne().HasForeignKey(x => x.ProductId);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.ProductType);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.BaseUnitId);
        });

        // ProductSku configuration
        builder.Entity<ProductSku>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ProductSkus", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => new { x.ProductId, x.ManufacturerId }).IsUnique();
        });

        // ProductAttachment configuration
        builder.Entity<ProductAttachment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ProductAttachments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileId).IsRequired().HasMaxLength(50);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(500);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
            b.Property(x => x.FileType).HasMaxLength(50);

            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.FileId);
        });

        // ProductUnitConversion configuration
        builder.Entity<ProductUnitConversion>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ProductUnitConversions", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.ProductId);
        });

        // GoodsReceipt configuration
        builder.Entity<GoodsReceipt>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "GoodsReceipts", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.ReceiptType).HasDefaultValue(GoodsReceiptType.Goods);
            b.Property(x => x.SupplierName).HasMaxLength(500);
            b.Property(x => x.SupplierAddress).HasMaxLength(500);
            b.Property(x => x.SupplierPhone).HasMaxLength(20);
            b.Property(x => x.PurchaserName).HasMaxLength(200);
            b.Property(x => x.DeliveryNoteNumber).HasMaxLength(100);
            b.Property(x => x.PreparedByName).HasMaxLength(200);
            b.Property(x => x.SupplierRepresentativePhone).HasMaxLength(20);
            b.Property(x => x.SupplierRepresentativeName).HasMaxLength(200);
            b.Property(x => x.ExpectedDeliveryDate);
            b.Property(x => x.PurchaserId);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.VatPercent).HasPrecision(5, 2);
            b.Property(x => x.TotalAmountBeforeVat).HasPrecision(18, 2);
            b.Property(x => x.TotalVatAmount).HasPrecision(18, 2);
            b.Property(x => x.TotalAmountAfterVat).HasPrecision(18, 2);
            b.Property(x => x.TotalPaid).HasPrecision(18, 2);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.ReceiptType);
            b.HasIndex(x => x.SupplierId);
            b.HasIndex(x => x.DocumentDate);
            b.HasIndex(x => x.WarehouseStatus);
            b.HasIndex(x => x.OrderStatus);
            b.HasIndex(x => x.PaymentStatus);
        });

        builder.Entity<GoodsReceiptItem>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "GoodsReceiptItems", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ProductName).HasMaxLength(500);
            b.Property(x => x.UnitName).HasMaxLength(100);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.AmountBeforeVat).HasPrecision(18, 2);
            b.Property(x => x.VatPercent).HasPrecision(5, 2);
            b.Property(x => x.VatAmount).HasPrecision(18, 2);
            b.Property(x => x.AmountAfterVat).HasPrecision(18, 2);
            b.Property(x => x.LotNumber).HasMaxLength(100);
            b.Property(x => x.IsEquipment).HasDefaultValue(false);
            b.Property(x => x.SerialNumber).HasMaxLength(200);
            b.Property(x => x.Model).HasMaxLength(256);
            b.Property(x => x.Manufacturer).HasMaxLength(500);
            b.Property(x => x.SupplierName).HasMaxLength(500);

            b.Property(x => x.ReceivedDate).IsRequired(false);
            b.Property(x => x.ExpiryDate).IsRequired(false);
            b.Property(x => x.WarehouseId).IsRequired(false);

            b.HasIndex(x => x.GoodsReceiptId);
            b.HasIndex(x => x.ProductId);
        });

        builder.Entity<Manufacturer>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Manufacturers", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(500);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.IsSystem).HasDefaultValue(false);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.Name);
        });

        builder.Entity<GoodsReceiptPayment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "GoodsReceiptPayments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.RemainingBeforePayment).HasPrecision(18, 2);
            b.Property(x => x.Phone).HasMaxLength(20);
            b.Property(x => x.RepresentativeName).HasMaxLength(200);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasIndex(x => x.GoodsReceiptId);
        });

        // WarehouseImportReceipt configuration
        builder.Entity<WarehouseImportReceipt>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "WarehouseImportReceipts", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.GoodsReceiptCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.WarehouseKeeperName).HasMaxLength(200);
            b.Property(x => x.WarehouseName).HasMaxLength(500);
            b.Property(x => x.DeliveryDocumentFileId).HasMaxLength(50);
            b.Property(x => x.DeliveryDocumentFileName).HasMaxLength(500);
            b.Property(x => x.DeliveryDocumentFilePath).HasMaxLength(500);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.GoodsReceiptId);
            b.HasIndex(x => x.WarehouseId);
            b.HasIndex(x => x.Status);
        });

        builder.Entity<WarehouseImportReceiptItem>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "WarehouseImportReceiptItems", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ProductName).IsRequired().HasMaxLength(500);
            b.Property(x => x.OrderedQuantity).HasPrecision(18, 4);
            b.Property(x => x.ActualReceivedQuantity).HasPrecision(18, 4);
            b.Property(x => x.UnitName).HasMaxLength(100);
            b.Property(x => x.LotNumber).HasMaxLength(100);
            b.Property(x => x.QualityDocumentFileId).HasMaxLength(50);
            b.Property(x => x.QualityDocumentFileName).HasMaxLength(500);
            b.Property(x => x.QualityDocumentFilePath).HasMaxLength(500);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasIndex(x => x.WarehouseImportReceiptId);
            b.HasIndex(x => x.GoodsReceiptItemId);
            b.HasIndex(x => x.ProductId);
        });

        // SpecimenType configuration
        builder.Entity<SpecimenType>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SpecimenTypes", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.DefaultStorageCondition).HasMaxLength(256);

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.SampleTypeCategoryId);

            // FK to SampleTypeCategory
            b.HasOne<SampleTypeCategory>()
                .WithMany()
                .HasForeignKey(x => x.SampleTypeCategoryId)
                .IsRequired(false);
        });

        // SpecimenTypeIndicator configuration (Junction Table for Many-to-Many)
        builder.Entity<SpecimenTypeIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SpecimenTypeIndicators", LabSpecimenManagementConsts.DbSchema);

            b.HasIndex(x => x.SpecimenTypeId);
            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => new { x.SpecimenTypeId, x.IndicatorId }).IsUnique();
        });

        // TestMethod configuration
        builder.Entity<TestMethod>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestMethods", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Unit).HasMaxLength(100);

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.IsActive);
        });

        // TestMethodIndicator configuration (Junction Table for Many-to-Many)
        builder.Entity<TestMethodIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestMethodIndicators", LabSpecimenManagementConsts.DbSchema);

            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.Unit).HasMaxLength(100).IsRequired(false);
            b.Property(x => x.FormattedUnit).HasMaxLength(500).IsRequired(false);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => x.TestMethodId);
            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => x.SpecimenTypeId);
            // Unique constraint: TestMethodId + IndicatorId + SpecimenTypeId
            // Allows same indicator multiple times with different specimen types
            b.HasIndex(x => new { x.TestMethodId, x.IndicatorId, x.SpecimenTypeId }).IsUnique();
        });

        // MaterialConsumptionItem configuration
        builder.Entity<MaterialConsumptionItem>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "MaterialConsumptionItems", LabSpecimenManagementConsts.DbSchema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.HasIndex(x => x.TestMethodIndicatorId);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.EquipmentId);
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

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        // Warehouse configuration
        builder.Entity<Warehouse>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Warehouses", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.PhysicalLocation).HasMaxLength(500);
            b.Property(x => x.IsDefault).HasDefaultValue(false);
            b.Property(x => x.Level).HasDefaultValue(0);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.ParentWarehouseId);

            b.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => x.ParentWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
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

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        // TestResultValue configuration
        builder.Entity<TestResultValue>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestResultValues", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(500);

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        // TestResultValueIndicator configuration (Junction Table for Many-to-Many)
        builder.Entity<TestResultValueIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestResultValueIndicators", LabSpecimenManagementConsts.DbSchema);

            b.HasIndex(x => x.TestResultValueId);
            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => x.SpecimenTypeId);
            // Composite unique key: (TestResultValueId, IndicatorId, SpecimenTypeId)
            b.HasIndex(x => new { x.TestResultValueId, x.IndicatorId, x.SpecimenTypeId }).IsUnique();
        });

        // Note configuration
        builder.Entity<Note>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Notes", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Content).IsRequired();

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.IsActive);
        });

        // IndicatorGroup configuration
        builder.Entity<IndicatorGroup>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "IndicatorGroups", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        // Indicator configuration
        builder.Entity<Indicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Indicators", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.FormattedName).HasMaxLength(1000);

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.IndicatorGroupId);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.ParentIndicatorId);
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

        // Laboratory (Khoa phòng) configuration
        builder.Entity<Laboratory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Laboratories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(20);
            b.Property(x => x.DepartmentId).IsRequired();

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.DepartmentId);
            b.HasIndex(x => x.IsActive);
        });

        // Standard (Tiêu chuẩn) configuration
        builder.Entity<Standard>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Standards", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.LimitDescription).HasMaxLength(500);

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.IsActive);
        });

        // StandardIndicator configuration (Junction Table for Many-to-Many)
        builder.Entity<StandardIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "StandardIndicators", LabSpecimenManagementConsts.DbSchema);

            b.Property(x => x.Unit).IsRequired().HasMaxLength(100);
            b.Property(x => x.ComparisonOperator).HasMaxLength(10);
            b.Property(x => x.AllowedLimit).HasMaxLength(256);
            b.Property(x => x.LowerOperator).HasMaxLength(10);
            b.Property(x => x.LowerLimit).HasMaxLength(256);
            b.Property(x => x.UpperOperator).HasMaxLength(10);
            b.Property(x => x.UpperLimit).HasMaxLength(256);

            b.HasIndex(x => x.StandardId);
            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => x.TestMethodId);
            b.HasIndex(x => x.SpecimenTypeId);
            b.HasIndex(x => new { x.StandardId, x.IndicatorId, x.TestMethodId, x.SpecimenTypeId }).IsUnique();
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

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.NCRNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
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

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.ReportNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
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

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.TaskNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
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

            // Filtered unique index - only enforce uniqueness for non-deleted records
            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
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

        // ============ LABORATORY TASK CONFIGURATION ============

        builder.Entity<LaboratoryTask>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "LaboratoryTasks", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ContractNumber).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.TestRequestId);
            b.HasIndex(x => x.LaboratoryId);
            b.HasIndex(x => x.TechnicalManagerId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.TransferDate);
            b.HasIndex(x => x.RequestCompletionDate);

            b.HasOne(x => x.TestRequest)
                .WithMany()
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired();

            b.HasOne(x => x.Laboratory)
                .WithMany()
                .HasForeignKey(x => x.LaboratoryId)
                .IsRequired();
        });

        builder.Entity<LaboratoryTaskIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "LaboratoryTaskIndicators", LabSpecimenManagementConsts.DbSchema);

            // String properties
            b.Property(x => x.Result).HasMaxLength(500);
            b.Property(x => x.SubContractMethodName).HasMaxLength(500);
            b.Property(x => x.SubContractNumber).HasMaxLength(200);
            b.Property(x => x.SubContractCost).HasPrecision(18, 2);
            // Unit: no length limit (text type) - copied from TestMethod which may have long values
            b.Property(x => x.RejectionReason).HasMaxLength(1000);

            // Indexes
            b.HasIndex(x => x.LaboratoryTaskId);
            b.HasIndex(x => x.TestRequestSampleIndicatorId);
            b.HasIndex(x => x.MethodId);
            b.HasIndex(x => x.TechnicianId);
            b.HasIndex(x => x.ApprovalStatus);

            // Foreign keys
            b.HasOne(x => x.LaboratoryTask)
                .WithMany(x => x.Indicators)
                .HasForeignKey(x => x.LaboratoryTaskId)
                .IsRequired();

            b.HasOne(x => x.TestRequestSampleIndicator)
                .WithMany()
                .HasForeignKey(x => x.TestRequestSampleIndicatorId)
                .IsRequired();
        });

        // ============ LABORATORY TASK SUPPORTER CONFIGURATION ============

        builder.Entity<LaboratoryTaskSupporter>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "LaboratoryTaskSupporters", LabSpecimenManagementConsts.DbSchema);

            b.HasIndex(x => x.LaboratoryTaskId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.LaboratoryTaskId, x.UserId }).IsUnique();

            b.HasOne(x => x.LaboratoryTask)
                .WithMany(x => x.Supporters)
                .HasForeignKey(x => x.LaboratoryTaskId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============ LABORATORY DEPARTMENT SUPPORTER CONFIGURATION ============

        builder.Entity<LaboratoryDepartmentSupporter>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "LaboratoryDepartmentSupporters", LabSpecimenManagementConsts.DbSchema);

            b.HasIndex(x => x.LaboratoryId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.LaboratoryId, x.UserId }).IsUnique();
            b.HasIndex(x => x.ExpirationDate); // For efficient cleanup query
        });

        // ============ LABORATORY TASK SAMPLE RECEIVED CONFIGURATION ============

        builder.Entity<LaboratoryTaskSampleReceived>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "LaboratoryTaskSampleReceiveds", LabSpecimenManagementConsts.DbSchema);

            b.HasIndex(x => x.LaboratoryTaskId);
            b.HasIndex(x => x.SampleId);
            b.HasIndex(x => new { x.LaboratoryTaskId, x.SampleId }).IsUnique();

            b.Property(x => x.ReceivedByUserName).HasMaxLength(256);

            b.HasOne(x => x.LaboratoryTask)
                .WithMany()
                .HasForeignKey(x => x.LaboratoryTaskId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Sample)
                .WithMany()
                .HasForeignKey(x => x.SampleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============ INDICATOR REJECTION HISTORY CONFIGURATION ============

        builder.Entity<IndicatorRejectionHistory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "IndicatorRejectionHistories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.RejectionReason).IsRequired().HasMaxLength(1000);
            b.Property(x => x.ReportedByRole).HasMaxLength(50);

            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => x.RejectedDate);
            b.HasIndex(x => x.IsCannotTest);

            b.HasOne(x => x.Indicator)
                .WithMany(x => x.RejectionHistories)
                .HasForeignKey(x => x.IndicatorId)
                .IsRequired();
        });

        // ============ REJECTION HISTORY IMAGE CONFIGURATION ============

        builder.Entity<RejectionHistoryImage>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "RejectionHistoryImages", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileId).IsRequired().HasMaxLength(100);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(500);

            b.HasIndex(x => x.RejectionHistoryId);

            b.HasOne(x => x.RejectionHistory)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.RejectionHistoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============ METHOD CHANGE HISTORY CONFIGURATION ============

        builder.Entity<MethodChangeHistory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "MethodChangeHistories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.OldMethodName).HasMaxLength(500);
            b.Property(x => x.NewMethodName).IsRequired().HasMaxLength(500);
            b.Property(x => x.Reason).IsRequired().HasMaxLength(2000);
            b.Property(x => x.RequestedByUserName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ConfirmedByUserName).HasMaxLength(256);

            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => x.RequestedDate);
            b.HasIndex(x => x.IsApproved);

            b.HasOne(x => x.Indicator)
                .WithMany(x => x.MethodChangeHistories)
                .HasForeignKey(x => x.IndicatorId)
                .IsRequired();
        });

        // ============ SAMPLE TESTING IMAGE CONFIGURATION ============

        builder.Entity<SampleTestingImage>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "SampleTestingImages", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileId).IsRequired().HasMaxLength(100);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(500);

            b.HasIndex(x => x.LaboratoryTaskId);
            b.HasIndex(x => x.SampleId);
            b.HasIndex(x => new { x.LaboratoryTaskId, x.SampleId, x.ImageType });

            b.HasOne(x => x.LaboratoryTask)
                .WithMany()
                .HasForeignKey(x => x.LaboratoryTaskId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============ INDICATOR IMAGE CONFIGURATION ============

        builder.Entity<IndicatorImage>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "IndicatorImages", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileId).IsRequired().HasMaxLength(100);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(500);

            b.HasIndex(x => x.LaboratoryTaskIndicatorId);

            b.HasOne(x => x.LaboratoryTaskIndicator)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.LaboratoryTaskIndicatorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============ TEST REQUEST CHANGE HISTORY CONFIGURATION ============

        builder.Entity<TestRequestChangeHistory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestRequestChangeHistories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FieldName).IsRequired().HasMaxLength(100);
            b.Property(x => x.OldValue).HasColumnType("text");
            b.Property(x => x.NewValue).HasColumnType("text");
            b.Property(x => x.RejectionReason).HasMaxLength(1000);

            b.HasIndex(x => x.TestRequestId);
            b.HasIndex(x => x.ApprovalStatus);
            b.HasIndex(x => x.CreationTime);

            b.HasOne<TestRequest>()
                .WithMany()
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired();
        });

        // ============ NOTIFICATION CONFIGURATION ============

        builder.Entity<Notification>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Notifications", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            b.Property(x => x.ReferenceId).HasMaxLength(100);
            b.Property(x => x.ReferenceType).HasMaxLength(100);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.IsRead });
            b.HasIndex(x => x.CreationTime);
        });

        // ============ QUALITY MANAGEMENT CONFIGURATION ============

        builder.Entity<QualityManagementTask>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "QualityManagementTasks", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ContractNumber).IsRequired().HasMaxLength(100);

            b.HasIndex(x => x.TestRequestId);
            b.HasIndex(x => x.ContractNumber);
            b.HasIndex(x => x.TaskType);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.TransferDate);
            b.HasIndex(x => x.RequestCompletionDate);
            b.HasIndex(x => x.ChangeHistoryId);

            b.HasOne(x => x.TestRequest)
                .WithMany()
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired();
        });

        builder.Entity<QualityManagementIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "QualityManagementIndicators", LabSpecimenManagementConsts.DbSchema);

            b.Property(x => x.ReferenceLimit).HasMaxLength(500);
            b.Property(x => x.RejectionReason).HasMaxLength(1000);

            b.HasIndex(x => x.QualityManagementTaskId);
            b.HasIndex(x => x.LaboratoryTaskIndicatorId);
            b.HasIndex(x => x.StandardId);
            b.HasIndex(x => x.ApprovalStatus);

            b.HasOne(x => x.QualityManagementTask)
                .WithMany(x => x.Indicators)
                .HasForeignKey(x => x.QualityManagementTaskId)
                .IsRequired();

            b.HasOne(x => x.LaboratoryTaskIndicator)
                .WithMany()
                .HasForeignKey(x => x.LaboratoryTaskIndicatorId)
                .IsRequired();
        });

        builder.Entity<QualityIndicatorStatusHistory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "QualityIndicatorStatusHistories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Comment).HasMaxLength(1000);

            b.HasIndex(x => x.QualityManagementIndicatorId);
            b.HasIndex(x => x.CreationTime);

            b.HasOne(x => x.QualityManagementIndicator)
                .WithMany()
                .HasForeignKey(x => x.QualityManagementIndicatorId)
                .IsRequired();
        });

        // ============ QUOTATION CONFIGURATION ============

        builder.Entity<Quotation>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Quotations", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.QuotationNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.CustomerName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ProvinceCode).HasMaxLength(10);
            b.Property(x => x.DistrictCode).HasMaxLength(10);
            b.Property(x => x.WardCode).HasMaxLength(10);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.TaxCode).HasMaxLength(50);
            b.Property(x => x.ContactPerson).HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(50);
            b.Property(x => x.TotalCost).HasPrecision(18, 2);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.QuotationNumber).IsUnique();
            b.HasIndex(x => x.QuotationDate);
            b.HasIndex(x => x.CustomerName);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.OrganizationId);

            b.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .IsRequired(false);
        });

        builder.Entity<QuotationSample>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "QuotationSamples", LabSpecimenManagementConsts.DbSchema);

            b.Property(x => x.SampleName).IsRequired().HasMaxLength(256);
            b.Property(x => x.TotalPrice).HasPrecision(18, 2);

            b.HasIndex(x => x.QuotationId);

            b.HasOne(x => x.Quotation)
                .WithMany(x => x.Samples)
                .HasForeignKey(x => x.QuotationId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuotationSampleIndicator>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "QuotationSampleIndicators", LabSpecimenManagementConsts.DbSchema);

            b.Property(x => x.IndicatorName).IsRequired().HasMaxLength(256);
            b.Property(x => x.TestMethodName).IsRequired().HasMaxLength(256);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);

            b.HasIndex(x => x.QuotationSampleId);
            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => x.TestMethodId);

            b.HasOne(x => x.QuotationSample)
                .WithMany(x => x.Indicators)
                .HasForeignKey(x => x.QuotationSampleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Indicator)
                .WithMany()
                .HasForeignKey(x => x.IndicatorId)
                .IsRequired();

            b.HasOne(x => x.TestMethod)
                .WithMany()
                .HasForeignKey(x => x.TestMethodId)
                .IsRequired();

            b.Property(x => x.SpecimenTypeName).HasMaxLength(256);
            b.HasIndex(x => x.SpecimenTypeId);

            b.HasOne(x => x.SpecimenType)
                .WithMany()
                .HasForeignKey(x => x.SpecimenTypeId)
                .IsRequired(false);
        });

        // ============ RECEIPT CONFIGURATION ============

        builder.Entity<Receipt>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Receipts", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ReceiptNumber).HasMaxLength(50);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.TestRequestId).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ReceiptNumber);

            b.HasOne(x => x.TestRequest)
                .WithMany()
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============ PAYMENT HISTORY CONFIGURATION ============

        builder.Entity<PaymentHistory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "PaymentHistories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.RemainingAmountBeforePayment).HasPrecision(18, 2);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasIndex(x => x.ReceiptId);
            b.HasIndex(x => x.PaymentNumber);
            b.HasIndex(x => x.PaymentDate);

            b.HasOne(x => x.Receipt)
                .WithMany(x => x.PaymentHistories)
                .HasForeignKey(x => x.ReceiptId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============ PUBLISHED REPORT CONFIGURATION ============

        builder.Entity<PublishedReport>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "PublishedReports", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileName).IsRequired().HasMaxLength(500);
            b.Property(x => x.ReportType).IsRequired().HasMaxLength(20);
            b.Property(x => x.PrintNote).HasMaxLength(1000);
            b.Property(x => x.IndicatorIds).IsRequired();

            b.HasIndex(x => x.TestRequestId);
            b.HasIndex(x => x.QualityManagementTaskId);
            b.HasIndex(x => x.CreationTime);

            b.HasOne(x => x.TestRequest)
                .WithMany()
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.QualityManagementTask)
                .WithMany()
                .HasForeignKey(x => x.QualityManagementTaskId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ============ COST CHANGE HISTORY CONFIGURATION ============

        builder.Entity<CostChangeHistory>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "CostChangeHistories", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.AffectedIndicatorName).HasMaxLength(500);
            b.Property(x => x.Reason).HasMaxLength(1000);

            b.HasIndex(x => x.TestRequestId);
            b.HasIndex(x => x.CreationTime);

            b.HasOne(x => x.TestRequest)
                .WithMany()
                .HasForeignKey(x => x.TestRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StockAdjustment configuration
        builder.Entity<StockAdjustment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "StockAdjustments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreationTime);
        });

        builder.Entity<StockAdjustmentItem>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "StockAdjustmentItems", LabSpecimenManagementConsts.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ProductName).IsRequired().HasMaxLength(500);
            b.Property(x => x.UnitName).HasMaxLength(100);
            b.Property(x => x.QuantityBefore).HasPrecision(18, 4);
            b.Property(x => x.ActualQuantity).HasPrecision(18, 4);
            b.Property(x => x.AdjustmentQuantity).HasPrecision(18, 4);

            b.HasIndex(x => x.StockAdjustmentId);
            b.HasIndex(x => x.ProductId);
        });

        builder.Entity<StockMovementLog>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "StockMovementLogs", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ProductName).IsRequired().HasMaxLength(500);
            b.Property(x => x.ReferenceCode).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.Property(x => x.QuantityBefore).HasPrecision(18, 4);
            b.Property(x => x.QuantityAfter).HasPrecision(18, 4);
            b.Property(x => x.ChangeQuantity).HasPrecision(18, 4);

            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.MovementType);
            b.HasIndex(x => x.ReferenceId);
            b.HasIndex(x => x.CreationTime);
        });

        builder.Entity<Equipment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "Equipments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(500);
            b.Property(x => x.SerialNumber).IsRequired().HasMaxLength(200);
            b.Property(x => x.GoodsReceiptCode).HasMaxLength(50);
            b.Property(x => x.SupplierName).HasMaxLength(500);
            b.Property(x => x.Model).HasMaxLength(256);
            b.Property(x => x.Manufacturer).HasMaxLength(256);
            b.Property(x => x.Origin).HasMaxLength(256);
            b.Property(x => x.UnitName).HasMaxLength(100);
            b.Property(x => x.Price).HasPrecision(18, 2);
            b.Property(x => x.LotNumber).HasMaxLength(100);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.Code);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SupplierId);
            b.HasIndex(x => x.GoodsReceiptId);
            b.HasIndex(x => x.CreationTime);
        });

        builder.Entity<EquipmentAttachment>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "EquipmentAttachments", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FileId).IsRequired().HasMaxLength(50);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(500);
            b.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
            b.Property(x => x.FileType).HasMaxLength(50);

            b.HasIndex(x => x.EquipmentId);
            b.HasIndex(x => x.FileId);
        });

        // ============ TEST BATCH CONFIGURATION ============

        builder.Entity<TestBatch>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestBatches", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Note).HasMaxLength(2000);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.IndicatorId);
            b.HasIndex(x => x.MethodId);
            b.HasIndex(x => x.SpecimenTypeId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CreationTime);

            b.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.TestBatchId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Consumptions)
                .WithOne()
                .HasForeignKey(x => x.TestBatchId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TestBatchItem>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestBatchItems", LabSpecimenManagementConsts.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.ContractNumber).HasMaxLength(100);
            b.Property(x => x.SampleCode).HasMaxLength(100);
            b.Property(x => x.Result).HasMaxLength(500);

            b.HasIndex(x => x.TestBatchId);
            b.HasIndex(x => x.LaboratoryTaskIndicatorId);
            b.HasIndex(x => x.LaboratoryTaskId);
        });

        builder.Entity<TestBatchConsumption>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "TestBatchConsumptions", LabSpecimenManagementConsts.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.RecommendedQuantity).HasPrecision(18, 4);
            b.Property(x => x.ActualQuantity).HasPrecision(18, 4);

            b.HasIndex(x => x.TestBatchId);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.EquipmentId);
        });

        builder.Entity<ApprovalProcess>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ApprovalProcesses", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasKey(x => x.Id);

            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => x.ProcessType).IsUnique().HasFilter("\"IsDeleted\" = false AND \"IsActive\" = true");

            b.HasMany(x => x.Steps)
                .WithOne()
                .HasForeignKey(x => x.ApprovalProcessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApprovalProcessStep>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ApprovalProcessSteps", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasKey(x => x.Id);

            b.Property(x => x.ApproverName).HasMaxLength(256).IsRequired();

            b.HasIndex(x => x.ApprovalProcessId);
        });

        builder.Entity<ApprovalSnapshot>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ApprovalSnapshots", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasKey(x => x.Id);

            b.Property(x => x.ReferenceType).HasMaxLength(100).IsRequired();
            b.Property(x => x.ReferenceCode).HasMaxLength(100).IsRequired();

            b.HasIndex(x => x.ReferenceId);
            b.HasIndex(x => x.ReferenceType);

            b.HasMany(x => x.Steps)
                .WithOne()
                .HasForeignKey(x => x.ApprovalSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApprovalSnapshotStep>(b =>
        {
            b.ToTable(LabSpecimenManagementConsts.DbTablePrefix + "ApprovalSnapshotSteps", LabSpecimenManagementConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasKey(x => x.Id);

            b.Property(x => x.ApproverName).HasMaxLength(256).IsRequired();
            b.Property(x => x.ActorName).HasMaxLength(256);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.HasIndex(x => x.ApprovalSnapshotId);
        });
    }
}
