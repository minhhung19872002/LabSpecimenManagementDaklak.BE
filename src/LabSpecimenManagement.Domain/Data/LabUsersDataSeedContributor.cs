using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace LabSpecimenManagement.Data;

/// <summary>
/// Seed data cho Users và Roles phục vụ hệ thống quản lý phòng thí nghiệm
/// </summary>
public class LabUsersDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IConfiguration _configuration;

    // Role names
    public const string RoleLabTechnicalManager = "LabTechnicalManager";  // Quản lý kỹ thuật Lab (labvisinh, labhoa)
    public const string RoleSampleReceiver = "SampleReceiver";            // Bộ phận Giao nhận mẫu
    public const string RoleQualityManager = "QualityManager";            // Quản lý chất lượng (QLCL)
    public const string RoleLabTechnician = "LabTechnician";              // Kỹ thuật viên (KTV)

    public LabUsersDataSeedContributor(
        IGuidGenerator guidGenerator,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        IPermissionManager permissionManager,
        IConfiguration configuration)
    {
        _guidGenerator = guidGenerator;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionManager = permissionManager;
        _configuration = configuration;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // Skip seeding in test environment
        var skipSeeding = _configuration.GetValue<bool>("SkipLabUsersSeed", false);
        if (skipSeeding)
        {
            return;
        }

        await CreateRolesAsync();
        await CreateUsersAsync();
    }

    private async Task CreateRolesAsync()
    {
        // 1. LabTechnicalManager - Quản lý kỹ thuật Lab (labvisinh, labhoa)
        // Access: Dashboard, Quản lý công việc, Quản lý xét nghiệm, Quản lý mẫu
        await CreateRoleIfNotExistsAsync(RoleLabTechnicalManager, new[]
        {
            // Dashboard
            LabSpecimenManagementPermissions.Dashboard.Default,
            // Quản lý công việc (LaboratoryTasks)
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,
            LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll,
            LabSpecimenManagementPermissions.LaboratoryTasks.UpdateStatus,
            // Quản lý xét nghiệm (Tests)
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Execute,
            LabSpecimenManagementPermissions.Tests.Review,
            // Quản lý mẫu (Specimens)
            LabSpecimenManagementPermissions.Specimens.Default,
            // TestResults - để xem kết quả
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Review,
        });

        // 2. SampleReceiver - Bộ phận Giao nhận mẫu (bpgiaonhan)
        // Access: Quản lý phiếu yêu cầu
        await CreateRoleIfNotExistsAsync(RoleSampleReceiver, new[]
        {
            // Quản lý phiếu yêu cầu (TestRequests)
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.TestRequests.Create,
            LabSpecimenManagementPermissions.TestRequests.Edit,
            // Specimens - cần để tạo mẫu trong phiếu yêu cầu
            LabSpecimenManagementPermissions.Specimens.Default,
            LabSpecimenManagementPermissions.Specimens.Create,
            LabSpecimenManagementPermissions.Specimens.Edit,
        });

        // 3. QualityManager - Quản lý chất lượng (quanlychatluong)
        // Access: Dashboard, Quản lý phiếu yêu cầu, Quản lý chất lượng, Quản lý mẫu, Quản lý xét nghiệm, Quản lý công việc
        await CreateRoleIfNotExistsAsync(RoleQualityManager, new[]
        {
            // Dashboard
            LabSpecimenManagementPermissions.Dashboard.Default,
            // Quản lý phiếu yêu cầu (TestRequests)
            LabSpecimenManagementPermissions.TestRequests.Default,
            LabSpecimenManagementPermissions.TestRequests.Create,
            LabSpecimenManagementPermissions.TestRequests.Edit,
            // Quản lý công việc (LaboratoryTasks)
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,
            LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll,
            // Quản lý xét nghiệm (Tests)
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Execute,
            LabSpecimenManagementPermissions.Tests.Review,
            LabSpecimenManagementPermissions.Tests.Approve,
            // Quản lý mẫu (Specimens)
            LabSpecimenManagementPermissions.Specimens.Default,
            // TestResults
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Review,
            LabSpecimenManagementPermissions.TestResults.Approve,
            LabSpecimenManagementPermissions.TestResults.Reject,
            // Reports
            LabSpecimenManagementPermissions.Reports.Default,
            LabSpecimenManagementPermissions.Reports.Approve,
            LabSpecimenManagementPermissions.Reports.Export,
            LabSpecimenManagementPermissions.Reports.Send,
        });

        // 4. LabTechnician - Kỹ thuật viên (kythuatvien1, kythuatvien2)
        // Access: Dashboard, Quản lý xét nghiệm, Quản lý mẫu, Quản lý công việc (để xem task được giao)
        await CreateRoleIfNotExistsAsync(RoleLabTechnician, new[]
        {
            // Dashboard - để xem overview và task được giao
            LabSpecimenManagementPermissions.Dashboard.Default,
            // Quản lý công việc (LaboratoryTasks) - cần để truy cập trang Testing
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,
            // Quản lý xét nghiệm (Tests)
            LabSpecimenManagementPermissions.Tests.Default,
            LabSpecimenManagementPermissions.Tests.Execute,
            // Quản lý mẫu (Specimens)
            LabSpecimenManagementPermissions.Specimens.Default,
            // TestResults
            LabSpecimenManagementPermissions.TestResults.Default,
            LabSpecimenManagementPermissions.TestResults.Create,
            LabSpecimenManagementPermissions.TestResults.Edit,
            LabSpecimenManagementPermissions.TestResults.Submit,
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
        const string defaultPassword = "1q2w3E*";

        // 1. Admin1 - Full access (role admin của ABP)
        await CreateUserIfNotExistsAsync(
            "admin1",
            "admin1@lab.vn",
            "Admin 1",
            defaultPassword,
            new[] { "admin" }
        );

        // 2. Lab Vi Sinh - Quản lý kỹ thuật
        await CreateUserIfNotExistsAsync(
            "labvisinh",
            "labvisinh@lab.vn",
            "Lab Vi Sinh - QLKT",
            defaultPassword,
            new[] { RoleLabTechnicalManager }
        );

        // 3. Lab Hóa - Quản lý kỹ thuật
        await CreateUserIfNotExistsAsync(
            "labhoa",
            "labhoa@lab.vn",
            "Lab Hóa - QLKT",
            defaultPassword,
            new[] { RoleLabTechnicalManager }
        );

        // 4. Bộ phận Giao nhận mẫu
        await CreateUserIfNotExistsAsync(
            "bpgiaonhan",
            "bpgiaonhan@lab.vn",
            "BP Giao nhận mẫu",
            defaultPassword,
            new[] { RoleSampleReceiver }
        );

        // 5. Quản lý chất lượng
        await CreateUserIfNotExistsAsync(
            "quanlychatluong",
            "quanlychatluong@lab.vn",
            "Quản lý chất lượng",
            defaultPassword,
            new[] { RoleQualityManager }
        );

        // 6. Kỹ thuật viên 1
        await CreateUserIfNotExistsAsync(
            "kythuatvien1",
            "kythuatvien1@lab.vn",
            "Kỹ thuật viên 1",
            defaultPassword,
            new[] { RoleLabTechnician }
        );

        // 7. Kỹ thuật viên 2
        await CreateUserIfNotExistsAsync(
            "kythuatvien2",
            "kythuatvien2@lab.vn",
            "Kỹ thuật viên 2",
            defaultPassword,
            new[] { RoleLabTechnician }
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
