using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace LabSpecimenManagement.Data;

/// <summary>
/// Seed data cho Users và Roles phục vụ workflow duyệt kết quả
/// </summary>
public class LabUsersDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;

    // Role names
    public const string RoleTechnician = "Technician";      // Kỹ thuật viên
    public const string RoleReviewer = "Reviewer";          // Trưởng phòng - Duyệt B1
    public const string RoleApprover = "Approver";          // Giám đốc PTN - Duyệt B2
    public const string RoleLabManager = "LabManager";      // Quản lý phòng thí nghiệm

    public LabUsersDataSeedContributor(
        IGuidGenerator guidGenerator,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        IPermissionManager permissionManager)
    {
        _guidGenerator = guidGenerator;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionManager = permissionManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await CreateRolesAsync();
        await CreateUsersAsync();
    }

    private async Task CreateRolesAsync()
    {
        // 1. Technician - Kỹ thuật viên
        await CreateRoleIfNotExistsAsync(RoleTechnician, new[]
        {
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Execute,
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Create,
            LabSpecimenManagementPermissions.TestResults.Edit,
            LabSpecimenManagementPermissions.TestResults.Submit,
        });

        // 2. Reviewer - Trưởng phòng (Duyệt B1)
        await CreateRoleIfNotExistsAsync(RoleReviewer, new[]
        {
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Review,
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Review,
            LabSpecimenManagementPermissions.TestResults.Reject,
            LabSpecimenManagementPermissions.NCRs.Default,
            LabSpecimenManagementPermissions.NCRs.Create,
        });

        // 3. Approver - Giám đốc PTN (Duyệt B2)
        await CreateRoleIfNotExistsAsync(RoleApprover, new[]
        {
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Approve,
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Approve,
            LabSpecimenManagementPermissions.TestResults.Reject,
            LabSpecimenManagementPermissions.Reports.Default,
            LabSpecimenManagementPermissions.Reports.Approve,
            LabSpecimenManagementPermissions.Reports.Export,
            LabSpecimenManagementPermissions.Reports.Send,
            LabSpecimenManagementPermissions.NCRs.Default,
            LabSpecimenManagementPermissions.NCRs.Close,
        });

        // 4. LabManager - Quản lý PTN (Full access)
        await CreateRoleIfNotExistsAsync(RoleLabManager, new[]
        {
            // Full TestRequest
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.TestRequests.Create,
            LabSpecimenManagementPermissions.TestRequests.Edit,
            LabSpecimenManagementPermissions.TestRequests.Delete,
            // Full Specimen
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Specimens.Create,
            LabSpecimenManagementPermissions.Specimens.Edit,
            LabSpecimenManagementPermissions.Specimens.Delete,
            // Full Test
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Create,
            LabSpecimenManagementPermissions.Tests.Edit,
            LabSpecimenManagementPermissions.Tests.Delete,
            LabSpecimenManagementPermissions.Tests.Assign,
            LabSpecimenManagementPermissions.Tests.Execute,
            LabSpecimenManagementPermissions.Tests.Review,
            LabSpecimenManagementPermissions.Tests.Approve,
            // Full TestResult
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Create,
            LabSpecimenManagementPermissions.TestResults.Edit,
            LabSpecimenManagementPermissions.TestResults.Delete,
            LabSpecimenManagementPermissions.TestResults.Submit,
            LabSpecimenManagementPermissions.TestResults.Review,
            LabSpecimenManagementPermissions.TestResults.Approve,
            LabSpecimenManagementPermissions.TestResults.Reject,
            // Full Report
            LabSpecimenManagementPermissions.Reports.Default,
            LabSpecimenManagementPermissions.Reports.Create,
            LabSpecimenManagementPermissions.Reports.Edit,
            LabSpecimenManagementPermissions.Reports.Delete,
            LabSpecimenManagementPermissions.Reports.Approve,
            LabSpecimenManagementPermissions.Reports.Export,
            LabSpecimenManagementPermissions.Reports.Send,
            // Full NCR
            LabSpecimenManagementPermissions.NCRs.Default,
            LabSpecimenManagementPermissions.NCRs.Create,
            LabSpecimenManagementPermissions.NCRs.Edit,
            LabSpecimenManagementPermissions.NCRs.Delete,
            LabSpecimenManagementPermissions.NCRs.Close,
        });
    }

    private async Task CreateRoleIfNotExistsAsync(string roleName, string[] permissions)
    {
        var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());
        if (role == null)
        {
            role = new IdentityRole(_guidGenerator.Create(), roleName);
            await _roleManager.CreateAsync(role);
        }

        // Assign permissions to role
        foreach (var permission in permissions)
        {
            await _permissionManager.SetForRoleAsync(roleName, permission, true);
        }
    }

    private async Task CreateUsersAsync()
    {
        // 1. Technician Users - Kỹ thuật viên
        await CreateUserIfNotExistsAsync(
            "ktv01",
            "ktv01@lab.vn",
            "Nguyen Van A",
            "1q2w3E*",
            new[] { RoleTechnician }
        );

        await CreateUserIfNotExistsAsync(
            "ktv02",
            "ktv02@lab.vn",
            "Tran Thi B",
            "1q2w3E*",
            new[] { RoleTechnician }
        );

        await CreateUserIfNotExistsAsync(
            "ktv03",
            "ktv03@lab.vn",
            "Le Van C",
            "1q2w3E*",
            new[] { RoleTechnician }
        );

        // 2. Reviewer - Trưởng phòng (Duyệt B1)
        await CreateUserIfNotExistsAsync(
            "reviewer01",
            "reviewer01@lab.vn",
            "Pham Thi D - Truong phong Vi sinh",
            "1q2w3E*",
            new[] { RoleReviewer }
        );

        await CreateUserIfNotExistsAsync(
            "reviewer02",
            "reviewer02@lab.vn",
            "Hoang Van E - Truong phong Hoa",
            "1q2w3E*",
            new[] { RoleReviewer }
        );

        // 3. Approver - Giám đốc PTN (Duyệt B2)
        await CreateUserIfNotExistsAsync(
            "approver",
            "approver@lab.vn",
            "PGS.TS Nguyen Van F - Giam doc PTN",
            "1q2w3E*",
            new[] { RoleApprover }
        );

        // 4. LabManager - Quản lý PTN (Full access)
        await CreateUserIfNotExistsAsync(
            "labmanager",
            "labmanager@lab.vn",
            "ThS. Tran Van G - Quan ly PTN",
            "1q2w3E*",
            new[] { RoleLabManager }
        );
    }

    private async Task CreateUserIfNotExistsAsync(
        string userName,
        string email,
        string name,
        string password,
        string[] roleNames)
    {
        var user = await _userRepository.FindByNormalizedUserNameAsync(userName.ToUpperInvariant());
        if (user == null)
        {
            user = new IdentityUser(
                _guidGenerator.Create(),
                userName,
                email
            );
            user.Name = name;

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                foreach (var roleName in roleNames)
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }
        }
    }
}
