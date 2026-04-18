using System;
using System.Collections.Generic;
using System.Linq;
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
/// Seed permissions cho role Admin với tất cả quyền
/// </summary>
public class RolePermissionsDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IConfiguration _configuration;

    // Role Admin - tên role phải khớp với LabUsersDataSeedContributor
    public const string RoleAdmin = "admin";

    private static readonly List<string> MasterDataDetailTabPermissions = new()
    {
        LabSpecimenManagementPermissions.Departments.Default,
        LabSpecimenManagementPermissions.Laboratories.Default,
        LabSpecimenManagementPermissions.SampleTypeCategories.Default,
        LabSpecimenManagementPermissions.SampleConditions.Default,
        LabSpecimenManagementPermissions.SpecimenTypes.Default,
        LabSpecimenManagementPermissions.TestMethods.Default,
        LabSpecimenManagementPermissions.Standards.Default,
        LabSpecimenManagementPermissions.StorageLocations.Default,
        LabSpecimenManagementPermissions.Indicators.Default,
        LabSpecimenManagementPermissions.FormTemplates.Default,
        LabSpecimenManagementPermissions.TestResultValues.Default,
        LabSpecimenManagementPermissions.Notes.Default,
    };

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

        // Chỉ seed permissions cho role Admin với tất cả quyền
        await SeedAdminPermissionsAsync();

        // Backfill quyền tab dữ liệu chi tiết cho role cũ đang có MasterData.Default
        // để không làm gián đoạn truy cập sau khi tách quyền theo tab.
        await BackfillMasterDataDetailTabPermissionsAsync();
    }

    /// <summary>
    /// Admin - Full quyền tất cả permissions
    /// </summary>
    private async Task SeedAdminPermissionsAsync()
    {
        var permissions = GetAllPermissions();
        await EnsureRoleAndPermissionsAsync(RoleAdmin, permissions, isStatic: true);
    }

    private async Task BackfillMasterDataDetailTabPermissionsAsync()
    {
        var roles = await _roleRepository.GetListAsync();
        var nonAdminRoles = roles
            .Where(r => !string.Equals(r.Name, RoleAdmin, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!nonAdminRoles.Any())
        {
            return;
        }

        foreach (var role in nonAdminRoles)
        {
            var currentPermissions = await _permissionManager.GetAllForRoleAsync(role.Name);
            var hasMasterDataDefault = currentPermissions.Any(
                p => p.IsGranted && p.Name == LabSpecimenManagementPermissions.MasterData.Default
            );

            var hasAnyDetailTabPermission = currentPermissions.Any(
                p => p.IsGranted && MasterDataDetailTabPermissions.Contains(p.Name)
            );

            // Chỉ backfill cho role đang có quyền vào Master Data
            // nhưng chưa được cấp bất kỳ quyền tab chi tiết nào.
            if (!hasMasterDataDefault || hasAnyDetailTabPermission)
            {
                continue;
            }

            foreach (var permission in MasterDataDetailTabPermissions)
            {
                await _permissionManager.SetForRoleAsync(role.Name, permission, true);
            }
        }
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
            LabSpecimenManagementPermissions.TestRequests.SendSample,

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

            // Master Data - Unified Permissions (for all master data features)
            LabSpecimenManagementPermissions.MasterData.Default,
            LabSpecimenManagementPermissions.MasterData.View,
            LabSpecimenManagementPermissions.MasterData.Create,
            LabSpecimenManagementPermissions.MasterData.Edit,
            LabSpecimenManagementPermissions.MasterData.Delete,
            LabSpecimenManagementPermissions.MasterData.Import,

            // Master Data - Departments (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.Departments.Default,
            LabSpecimenManagementPermissions.Departments.Create,
            LabSpecimenManagementPermissions.Departments.Edit,
            LabSpecimenManagementPermissions.Departments.Delete,

            // Master Data - Specimen Types (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.SpecimenTypes.Default,
            LabSpecimenManagementPermissions.SpecimenTypes.Create,
            LabSpecimenManagementPermissions.SpecimenTypes.Edit,
            LabSpecimenManagementPermissions.SpecimenTypes.Delete,

            // Master Data - Test Methods (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.TestMethods.Default,
            LabSpecimenManagementPermissions.TestMethods.Create,
            LabSpecimenManagementPermissions.TestMethods.Edit,
            LabSpecimenManagementPermissions.TestMethods.Delete,

            // Master Data - Standards (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.Standards.Default,
            LabSpecimenManagementPermissions.Standards.Create,
            LabSpecimenManagementPermissions.Standards.Edit,
            LabSpecimenManagementPermissions.Standards.Delete,

            // Master Data - Storage Locations (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.StorageLocations.Default,
            LabSpecimenManagementPermissions.StorageLocations.Create,
            LabSpecimenManagementPermissions.StorageLocations.Edit,
            LabSpecimenManagementPermissions.StorageLocations.Delete,

            // Master Data - Customers (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.Customers.Default,
            LabSpecimenManagementPermissions.Customers.Create,
            LabSpecimenManagementPermissions.Customers.Edit,
            LabSpecimenManagementPermissions.Customers.Delete,

            // Master Data - Indicator Groups (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.IndicatorGroups.Default,
            LabSpecimenManagementPermissions.IndicatorGroups.Create,
            LabSpecimenManagementPermissions.IndicatorGroups.Edit,
            LabSpecimenManagementPermissions.IndicatorGroups.Delete,
            LabSpecimenManagementPermissions.IndicatorGroups.Import,

            // Master Data - Indicators (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.Indicators.Default,
            LabSpecimenManagementPermissions.Indicators.Create,
            LabSpecimenManagementPermissions.Indicators.Edit,
            LabSpecimenManagementPermissions.Indicators.Delete,
            LabSpecimenManagementPermissions.Indicators.Import,

            // Master Data - Laboratories (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.Laboratories.Default,
            LabSpecimenManagementPermissions.Laboratories.Create,
            LabSpecimenManagementPermissions.Laboratories.Edit,
            LabSpecimenManagementPermissions.Laboratories.Delete,

            // Master Data - Sample Conditions (DEPRECATED - use MasterData instead)
            LabSpecimenManagementPermissions.SampleConditions.Default,
            LabSpecimenManagementPermissions.SampleConditions.Create,
            LabSpecimenManagementPermissions.SampleConditions.Edit,
            LabSpecimenManagementPermissions.SampleConditions.Delete,

            // Master Data - Sample Type Categories
            LabSpecimenManagementPermissions.SampleTypeCategories.Default,
            LabSpecimenManagementPermissions.SampleTypeCategories.Create,
            LabSpecimenManagementPermissions.SampleTypeCategories.Edit,
            LabSpecimenManagementPermissions.SampleTypeCategories.Delete,

            // Master Data - Notes
            LabSpecimenManagementPermissions.Notes.Default,
            LabSpecimenManagementPermissions.Notes.Create,
            LabSpecimenManagementPermissions.Notes.Edit,
            LabSpecimenManagementPermissions.Notes.Delete,

            // Master Data - Test Result Values
            LabSpecimenManagementPermissions.TestResultValues.Default,
            LabSpecimenManagementPermissions.TestResultValues.Create,
            LabSpecimenManagementPermissions.TestResultValues.Edit,
            LabSpecimenManagementPermissions.TestResultValues.Delete,

            // Master Data - Form Templates
            LabSpecimenManagementPermissions.FormTemplates.Default,

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
