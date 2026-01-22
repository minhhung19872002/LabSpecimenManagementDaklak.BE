using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace LabSpecimenManagement.Data;

/// <summary>
/// Seed permissions cho các roles mặc định
/// Tạo 6 nhóm quyền với permissions chi tiết theo Permission Matrix
/// </summary>
public class RolePermissionsDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IConfiguration _configuration;

    // Role names - phải khớp với LabUsersDataSeedContributor
    public const string RoleAdmin = "admin";
    public const string RoleLabTechnicalManager = "LabTechnicalManager";
    public const string RoleSampleReceiver = "SampleReceiver";
    public const string RoleQualityManager = "QualityManager";
    public const string RoleLabTechnician = "LabTechnician";
    public const string RoleAccountant = "Accountant";

    public RolePermissionsDataSeedContributor(
        IGuidGenerator guidGenerator,
        IIdentityRoleRepository roleRepository,
        IdentityRoleManager roleManager,
        IPermissionManager permissionManager,
        IConfiguration configuration)
    {
        _guidGenerator = guidGenerator;
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _permissionManager = permissionManager;
        _configuration = configuration;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var skipSeeding = _configuration.GetValue<bool>("SkipRolePermissionsSeed", false);
        if (skipSeeding)
        {
            return;
        }

        // Seed permissions cho từng role
        await SeedAdminPermissionsAsync();
        await SeedQualityManagerPermissionsAsync();
        await SeedLabTechnicalManagerPermissionsAsync();
        await SeedLabTechnicianPermissionsAsync();
        await SeedSampleReceiverPermissionsAsync();
        await SeedAccountantPermissionsAsync();
    }

    /// <summary>
    /// Admin - Full quyền tất cả permissions
    /// </summary>
    private async Task SeedAdminPermissionsAsync()
    {
        var permissions = GetAllPermissions();
        await EnsureRoleAndPermissionsAsync(RoleAdmin, permissions, isStatic: true);
    }

    /// <summary>
    /// QualityManager (QLCL) - Quản lý chất lượng
    /// Access: Dashboard, Test Requests (view), Quality Management, Specimens, Specimen Histories, NCR, Quotations, Settings
    /// </summary>
    private async Task SeedQualityManagerPermissionsAsync()
    {
        var permissions = new List<string>
        {
            // Dashboard
            LabSpecimenManagementPermissions.Dashboard.Default,

            // Test Requests (view only)
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.TestRequests.Create,
            LabSpecimenManagementPermissions.TestRequests.Edit,

            // Specimens
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Specimens.Create,
            LabSpecimenManagementPermissions.Specimens.Edit,

            // Laboratory Tasks
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll,

            // Tests
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Execute,
            LabSpecimenManagementPermissions.Tests.Review,
            LabSpecimenManagementPermissions.Tests.Approve,

            // Test Results - Full quality management
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Review,
            LabSpecimenManagementPermissions.TestResults.Approve,
            LabSpecimenManagementPermissions.TestResults.Reject,

            // Reports - Full
            LabSpecimenManagementPermissions.Reports.Default,
            LabSpecimenManagementPermissions.Reports.Create,
            LabSpecimenManagementPermissions.Reports.Edit,
            LabSpecimenManagementPermissions.Reports.Approve,
            LabSpecimenManagementPermissions.Reports.Export,
            LabSpecimenManagementPermissions.Reports.Send,

            // NCRs - Full
            LabSpecimenManagementPermissions.NCRs.Default,
            LabSpecimenManagementPermissions.NCRs.Create,
            LabSpecimenManagementPermissions.NCRs.Edit,
            LabSpecimenManagementPermissions.NCRs.Delete,
            LabSpecimenManagementPermissions.NCRs.Close,
            LabSpecimenManagementPermissions.NCRs.Assign,

            // Quotations - Full
            LabSpecimenManagementPermissions.Quotations.Default,
            LabSpecimenManagementPermissions.Quotations.Create,
            LabSpecimenManagementPermissions.Quotations.Edit,
            LabSpecimenManagementPermissions.Quotations.Delete,

            // Settings
            LabSpecimenManagementPermissions.Settings.Default,
        };

        await EnsureRoleAndPermissionsAsync(RoleQualityManager, permissions, isStatic: true);
    }

    /// <summary>
    /// LabTechnicalManager (QLKT) - Quản lý kỹ thuật
    /// Access: Dashboard, Tasks (full), Testing (view), Specimens, Quotations, Settings
    /// </summary>
    private async Task SeedLabTechnicalManagerPermissionsAsync()
    {
        var permissions = new List<string>
        {
            // Dashboard
            LabSpecimenManagementPermissions.Dashboard.Default,

            // Laboratory Tasks - Full
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,
            LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll,
            LabSpecimenManagementPermissions.LaboratoryTasks.UpdateStatus,

            // Tests
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Execute,
            LabSpecimenManagementPermissions.Tests.Review,
            LabSpecimenManagementPermissions.Tests.Assign,

            // Specimens
            LabSpecimenManagementPermissions.Specimens.Default,

            // Test Results
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Review,

            // Quotations - Full
            LabSpecimenManagementPermissions.Quotations.Default,
            LabSpecimenManagementPermissions.Quotations.Create,
            LabSpecimenManagementPermissions.Quotations.Edit,
            LabSpecimenManagementPermissions.Quotations.Delete,

            // Settings
            LabSpecimenManagementPermissions.Settings.Default,
        };

        await EnsureRoleAndPermissionsAsync(RoleLabTechnicalManager, permissions, isStatic: true);
    }

    /// <summary>
    /// LabTechnician (KTV) - Thử nghiệm viên
    /// Access: Testing, Specimens, Quotations, Settings
    /// </summary>
    private async Task SeedLabTechnicianPermissionsAsync()
    {
        var permissions = new List<string>
        {
            // Dashboard
            LabSpecimenManagementPermissions.Dashboard.Default,

            // Laboratory Tasks
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,

            // Tests
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Execute,

            // Specimens
            LabSpecimenManagementPermissions.Specimens.Default,

            // Test Results - Input and submit
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Create,
            LabSpecimenManagementPermissions.TestResults.Edit,
            LabSpecimenManagementPermissions.TestResults.Submit,

            // Quotations - Full
            LabSpecimenManagementPermissions.Quotations.Default,
            LabSpecimenManagementPermissions.Quotations.Create,
            LabSpecimenManagementPermissions.Quotations.Edit,
            LabSpecimenManagementPermissions.Quotations.Delete,

            // Settings
            LabSpecimenManagementPermissions.Settings.Default,
        };

        await EnsureRoleAndPermissionsAsync(RoleLabTechnician, permissions, isStatic: true);
    }

    /// <summary>
    /// SampleReceiver - Bộ phận giao nhận mẫu
    /// Access: Test Requests, Quotations, Settings
    /// </summary>
    private async Task SeedSampleReceiverPermissionsAsync()
    {
        var permissions = new List<string>
        {
            // Test Requests - Full
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.TestRequests.Create,
            LabSpecimenManagementPermissions.TestRequests.Edit,
            LabSpecimenManagementPermissions.TestRequests.Delete,

            // Specimens - Create and Edit for test requests
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Specimens.Create,
            LabSpecimenManagementPermissions.Specimens.Edit,

            // Quotations - Full
            LabSpecimenManagementPermissions.Quotations.Default,
            LabSpecimenManagementPermissions.Quotations.Create,
            LabSpecimenManagementPermissions.Quotations.Edit,
            LabSpecimenManagementPermissions.Quotations.Delete,

            // Settings
            LabSpecimenManagementPermissions.Settings.Default,
        };

        await EnsureRoleAndPermissionsAsync(RoleSampleReceiver, permissions, isStatic: true);
    }

    /// <summary>
    /// Accountant - Kế toán
    /// Access: Receipts, Quotations, Settings
    /// </summary>
    private async Task SeedAccountantPermissionsAsync()
    {
        var permissions = new List<string>
        {
            // Receipts - Full
            LabSpecimenManagementPermissions.Receipts.Default,
            LabSpecimenManagementPermissions.Receipts.View,
            LabSpecimenManagementPermissions.Receipts.Confirm,

            // Quotations - Full
            LabSpecimenManagementPermissions.Quotations.Default,
            LabSpecimenManagementPermissions.Quotations.Create,
            LabSpecimenManagementPermissions.Quotations.Edit,
            LabSpecimenManagementPermissions.Quotations.Delete,

            // Settings
            LabSpecimenManagementPermissions.Settings.Default,
        };

        await EnsureRoleAndPermissionsAsync(RoleAccountant, permissions, isStatic: true);
    }

    /// <summary>
    /// Ensure role exists and has the specified permissions
    /// </summary>
    private async Task EnsureRoleAndPermissionsAsync(string roleName, List<string> permissions, bool isStatic = false)
    {
        // Check if role exists
        var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());
        if (role == null)
        {
            // Create role if not exists
            role = new IdentityRole(_guidGenerator.Create(), roleName)
            {
                IsStatic = isStatic,
                IsPublic = true
            };
            await _roleManager.CreateAsync(role);
        }
        else if (isStatic && !role.IsStatic)
        {
            // Update IsStatic if needed
            role.IsStatic = true;
            await _roleManager.UpdateAsync(role);
        }

        // Grant permissions
        foreach (var permission in permissions)
        {
            await _permissionManager.SetForRoleAsync(roleName, permission, true);
        }
    }

    /// <summary>
    /// Get all permissions defined in the system
    /// </summary>
    private List<string> GetAllPermissions()
    {
        return new List<string>
        {
            // Dashboard
            LabSpecimenManagementPermissions.Dashboard.Default,

            // Statistics
            LabSpecimenManagementPermissions.Statistics.Default,
            LabSpecimenManagementPermissions.Statistics.Export,

            // Test Requests
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.TestRequests.Create,
            LabSpecimenManagementPermissions.TestRequests.Edit,
            LabSpecimenManagementPermissions.TestRequests.Delete,

            // Specimens
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Specimens.Create,
            LabSpecimenManagementPermissions.Specimens.Edit,
            LabSpecimenManagementPermissions.Specimens.Delete,

            // Tests
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Create,
            LabSpecimenManagementPermissions.Tests.Edit,
            LabSpecimenManagementPermissions.Tests.Delete,
            LabSpecimenManagementPermissions.Tests.Assign,
            LabSpecimenManagementPermissions.Tests.Execute,
            LabSpecimenManagementPermissions.Tests.Review,
            LabSpecimenManagementPermissions.Tests.Approve,

            // Test Results
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Create,
            LabSpecimenManagementPermissions.TestResults.Edit,
            LabSpecimenManagementPermissions.TestResults.Delete,
            LabSpecimenManagementPermissions.TestResults.Submit,
            LabSpecimenManagementPermissions.TestResults.Review,
            LabSpecimenManagementPermissions.TestResults.Approve,
            LabSpecimenManagementPermissions.TestResults.Reject,

            // Reports
            LabSpecimenManagementPermissions.Reports.Default,
            LabSpecimenManagementPermissions.Reports.Create,
            LabSpecimenManagementPermissions.Reports.Edit,
            LabSpecimenManagementPermissions.Reports.Delete,
            LabSpecimenManagementPermissions.Reports.Approve,
            LabSpecimenManagementPermissions.Reports.Export,
            LabSpecimenManagementPermissions.Reports.Send,

            // NCRs
            LabSpecimenManagementPermissions.NCRs.Default,
            LabSpecimenManagementPermissions.NCRs.Create,
            LabSpecimenManagementPermissions.NCRs.Edit,
            LabSpecimenManagementPermissions.NCRs.Delete,
            LabSpecimenManagementPermissions.NCRs.Close,
            LabSpecimenManagementPermissions.NCRs.Assign,

            // Laboratory Tasks
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,
            LabSpecimenManagementPermissions.LaboratoryTasks.UpdateStatus,
            LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll,

            // Task Assignments
            LabSpecimenManagementPermissions.TaskAssignments.Default,
            LabSpecimenManagementPermissions.TaskAssignments.Create,
            LabSpecimenManagementPermissions.TaskAssignments.Edit,
            LabSpecimenManagementPermissions.TaskAssignments.Delete,
            LabSpecimenManagementPermissions.TaskAssignments.Accept,
            LabSpecimenManagementPermissions.TaskAssignments.Reject,
            LabSpecimenManagementPermissions.TaskAssignments.Complete,

            // Subcontractors
            LabSpecimenManagementPermissions.Subcontractors.Default,
            LabSpecimenManagementPermissions.Subcontractors.Create,
            LabSpecimenManagementPermissions.Subcontractors.Edit,
            LabSpecimenManagementPermissions.Subcontractors.Delete,

            // Quotations
            LabSpecimenManagementPermissions.Quotations.Default,
            LabSpecimenManagementPermissions.Quotations.Create,
            LabSpecimenManagementPermissions.Quotations.Edit,
            LabSpecimenManagementPermissions.Quotations.Delete,

            // Receipts
            LabSpecimenManagementPermissions.Receipts.Default,
            LabSpecimenManagementPermissions.Receipts.View,
            LabSpecimenManagementPermissions.Receipts.Confirm,

            // Master Data - Departments
            LabSpecimenManagementPermissions.Departments.Default,
            LabSpecimenManagementPermissions.Departments.Create,
            LabSpecimenManagementPermissions.Departments.Edit,
            LabSpecimenManagementPermissions.Departments.Delete,

            // Master Data - Specimen Types
            LabSpecimenManagementPermissions.SpecimenTypes.Default,
            LabSpecimenManagementPermissions.SpecimenTypes.Create,
            LabSpecimenManagementPermissions.SpecimenTypes.Edit,
            LabSpecimenManagementPermissions.SpecimenTypes.Delete,

            // Master Data - Test Methods
            LabSpecimenManagementPermissions.TestMethods.Default,
            LabSpecimenManagementPermissions.TestMethods.Create,
            LabSpecimenManagementPermissions.TestMethods.Edit,
            LabSpecimenManagementPermissions.TestMethods.Delete,

            // Master Data - Standards
            LabSpecimenManagementPermissions.Standards.Default,
            LabSpecimenManagementPermissions.Standards.Create,
            LabSpecimenManagementPermissions.Standards.Edit,
            LabSpecimenManagementPermissions.Standards.Delete,

            // Master Data - Storage Locations
            LabSpecimenManagementPermissions.StorageLocations.Default,
            LabSpecimenManagementPermissions.StorageLocations.Create,
            LabSpecimenManagementPermissions.StorageLocations.Edit,
            LabSpecimenManagementPermissions.StorageLocations.Delete,

            // Master Data - Customers
            LabSpecimenManagementPermissions.Customers.Default,
            LabSpecimenManagementPermissions.Customers.Create,
            LabSpecimenManagementPermissions.Customers.Edit,
            LabSpecimenManagementPermissions.Customers.Delete,

            // Master Data - Indicator Groups
            LabSpecimenManagementPermissions.IndicatorGroups.Default,
            LabSpecimenManagementPermissions.IndicatorGroups.Create,
            LabSpecimenManagementPermissions.IndicatorGroups.Edit,
            LabSpecimenManagementPermissions.IndicatorGroups.Delete,
            LabSpecimenManagementPermissions.IndicatorGroups.Import,

            // Master Data - Indicators
            LabSpecimenManagementPermissions.Indicators.Default,
            LabSpecimenManagementPermissions.Indicators.Create,
            LabSpecimenManagementPermissions.Indicators.Edit,
            LabSpecimenManagementPermissions.Indicators.Delete,
            LabSpecimenManagementPermissions.Indicators.Import,

            // Master Data - Laboratories
            LabSpecimenManagementPermissions.Laboratories.Default,
            LabSpecimenManagementPermissions.Laboratories.Create,
            LabSpecimenManagementPermissions.Laboratories.Edit,
            LabSpecimenManagementPermissions.Laboratories.Delete,

            // Master Data - Sample Conditions
            LabSpecimenManagementPermissions.SampleConditions.Default,
            LabSpecimenManagementPermissions.SampleConditions.Create,
            LabSpecimenManagementPermissions.SampleConditions.Edit,
            LabSpecimenManagementPermissions.SampleConditions.Delete,

            // Users
            LabSpecimenManagementPermissions.Users.Default,
            LabSpecimenManagementPermissions.Users.Create,
            LabSpecimenManagementPermissions.Users.Edit,
            LabSpecimenManagementPermissions.Users.Delete,
            LabSpecimenManagementPermissions.Users.ManageRoles,

            // Role Management
            LabSpecimenManagementPermissions.RoleManagement.Default,
            LabSpecimenManagementPermissions.RoleManagement.Create,
            LabSpecimenManagementPermissions.RoleManagement.Edit,
            LabSpecimenManagementPermissions.RoleManagement.Delete,
            LabSpecimenManagementPermissions.RoleManagement.ManagePermissions,
            LabSpecimenManagementPermissions.RoleManagement.ManageUsers,

            // Settings
            LabSpecimenManagementPermissions.Settings.Default,
            LabSpecimenManagementPermissions.Settings.Edit,

            // Audit Logs
            LabSpecimenManagementPermissions.AuditLogs.Default,

            // Import
            LabSpecimenManagementPermissions.Import.Default,
            LabSpecimenManagementPermissions.Import.TestRequests,
            LabSpecimenManagementPermissions.Import.Specimens,
            LabSpecimenManagementPermissions.Import.Customers,
            LabSpecimenManagementPermissions.Import.TestMethods,
            LabSpecimenManagementPermissions.Import.Standards,

            // Sync
            LabSpecimenManagementPermissions.Sync.Default,
            LabSpecimenManagementPermissions.Sync.TestRequests,
        };
    }
}
