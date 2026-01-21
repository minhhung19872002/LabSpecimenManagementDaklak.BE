using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
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
    private readonly IRepository<Department, Guid> _departmentRepository;

    // Cache for department lookup
    private Dictionary<string, Guid>? _departmentCodeToIdMap;

    // Role names
    public const string RoleLabTechnicalManager = "LabTechnicalManager";  // Quản lý kỹ thuật Lab (labvisinh, labhoa)
    public const string RoleSampleReceiver = "SampleReceiver";            // Bộ phận Giao nhận mẫu
    public const string RoleQualityManager = "QualityManager";            // Quản lý chất lượng (QLCL)
    public const string RoleLabTechnician = "LabTechnician";              // Thử nghiệm viên (KTV)
    public const string RoleAccountant = "Accountant";                    // Kế toán

    public LabUsersDataSeedContributor(
        IGuidGenerator guidGenerator,
        IIdentityRoleRepository roleRepository,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        IPermissionManager permissionManager,
        IConfiguration configuration,
        IRepository<Department, Guid> departmentRepository)
    {
        _guidGenerator = guidGenerator;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionManager = permissionManager;
        _configuration = configuration;
        _departmentRepository = departmentRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // Skip seeding in test environment
        var skipSeeding = _configuration.GetValue<bool>("SkipLabUsersSeed", false);
        if (skipSeeding)
        {
            return;
        }

        // Seed Departments first (if not exists) for DepartmentId assignment
        await SeedDepartmentsIfNotExistsAsync();

        await CreateRolesAsync();
        await CreateUsersAsync();
    }

    /// <summary>
    /// Seed Departments nếu chưa có - để có thể gán DepartmentId cho Users
    /// </summary>
    private async Task SeedDepartmentsIfNotExistsAsync()
    {
        if (await _departmentRepository.GetCountAsync() > 0) return;

        var departments = new[]
        {
            // Phòng ban chung
            ("QL", "Quản lý"),
            ("BGD", "Ban Giám đốc"),
            ("PHH", "Phòng Hóa học"),
            ("PVS", "Phòng Vi sinh"),
            ("PVL", "Phòng Vật lý"),
            ("PMT", "Phòng Môi trường"),
            ("TNM", "Tiếp nhận mẫu"),
            // Phòng ban cho Laboratories
            ("TTKN", "Trung tâm kiểm nghiệm"),
            ("HLSK", "Lab Hóa Lý sắc ký"),
            ("LVS", "Lab vi sinh"),
            ("PNM", "Phòng Nhận mẫu"),
            ("KVK", "Khoa vi khuẩn"),
            ("HDP", "Hợp đồng phụ"),
            ("KKT", "Khoa Kế Toán"),
            ("QTN-KLN", "Quầy thu ngân - Kim loại nặng"),
            ("LH", "Lab Hóa"),
        };

        foreach (var (code, name) in departments)
        {
            var entity = new Department(_guidGenerator.Create(), code, name);
            await _departmentRepository.InsertAsync(entity);
        }
    }

    private async Task CreateRolesAsync()
    {
        // 1. LabTechnicalManager - Quản lý kỹ thuật Lab (labvisinh, labhoa)
        // Access: Dashboard, Quản lý công việc, Quản lý thử nghiệm, Quản lý mẫu
        await CreateRoleIfNotExistsAsync(RoleLabTechnicalManager, new[]
        {
            // Dashboard
            LabSpecimenManagementPermissions.Dashboard.Default,
            // Quản lý công việc (LaboratoryTasks)
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,
            LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll,
            LabSpecimenManagementPermissions.LaboratoryTasks.UpdateStatus,
            // Quản lý thử nghiệm (Tests)
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
        // Access: Dashboard, Quản lý phiếu yêu cầu, Quản lý chất lượng, Quản lý mẫu, Quản lý thử nghiệm, Quản lý công việc
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
            // Quản lý thử nghiệm (Tests)
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

        // 4. LabTechnician - Thử nghiệm viên (kythuatvien1, kythuatvien2)
        // Access: Dashboard, Quản lý thử nghiệm, Quản lý mẫu, Quản lý công việc (để xem task được giao)
        await CreateRoleIfNotExistsAsync(RoleLabTechnician, new[]
        {
            // Dashboard - để xem overview và task được giao
            LabSpecimenManagementPermissions.Dashboard.Default,
            // Quản lý công việc (LaboratoryTasks) - cần để truy cập trang Testing
            LabSpecimenManagementPermissions.LaboratoryTasks.Default,
            LabSpecimenManagementPermissions.LaboratoryTasks.Edit,
            // Quản lý thử nghiệm (Tests)
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

        // 5. Accountant - Kế toán (ketoan)
        // Access: Báo giá
        await CreateRoleIfNotExistsAsync(RoleAccountant, new[]
        {
            // Báo giá (Quotations)
            LabSpecimenManagementPermissions.Quotations.Default,
            LabSpecimenManagementPermissions.Quotations.Create,
            LabSpecimenManagementPermissions.Quotations.Edit,
            LabSpecimenManagementPermissions.Quotations.Delete,
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

        // Load departments for mapping
        await LoadDepartmentMapAsync();

        // 1. Admin1 - Full access (role admin của ABP) - thuộc Ban Giám đốc
        await CreateUserIfNotExistsAsync(
            "admin1",
            "admin1@lab.vn",
            "Admin 1",
            defaultPassword,
            new[] { "admin" },
            "BGD"  // Ban Giám đốc
        );

        // 2. Lab Vi Sinh - Quản lý kỹ thuật - thuộc Lab vi sinh
        await CreateUserIfNotExistsAsync(
            "labvisinh",
            "labvisinh@lab.vn",
            "Lab Vi Sinh - QLKT",
            defaultPassword,
            new[] { RoleLabTechnicalManager },
            "LVS"  // Lab vi sinh
        );

        // 3. Lab Hóa - Quản lý kỹ thuật - thuộc Lab Hóa
        await CreateUserIfNotExistsAsync(
            "labhoa",
            "labhoa@lab.vn",
            "Lab Hóa - QLKT",
            defaultPassword,
            new[] { RoleLabTechnicalManager },
            "LH"  // Lab Hóa
        );

        // 4. Bộ phận Giao nhận mẫu - thuộc Phòng Nhận mẫu
        await CreateUserIfNotExistsAsync(
            "bpgiaonhan",
            "bpgiaonhan@lab.vn",
            "BP Giao nhận mẫu",
            defaultPassword,
            new[] { RoleSampleReceiver },
            "PNM"  // Phòng Nhận mẫu
        );

        // 5. Quản lý chất lượng - thuộc Trung tâm kiểm nghiệm
        await CreateUserIfNotExistsAsync(
            "quanlychatluong",
            "quanlychatluong@lab.vn",
            "Quản lý chất lượng",
            defaultPassword,
            new[] { RoleQualityManager },
            "TTKN"  // Trung tâm kiểm nghiệm
        );

        // 6. Thử nghiệm viên 1 - thuộc Lab vi sinh
        await CreateUserIfNotExistsAsync(
            "kythuatvien1",
            "kythuatvien1@lab.vn",
            "Thử nghiệm viên 1",
            defaultPassword,
            new[] { RoleLabTechnician },
            "LVS"  // Lab vi sinh
        );

        // 7. Thử nghiệm viên 2 - thuộc Lab Hóa
        await CreateUserIfNotExistsAsync(
            "kythuatvien2",
            "kythuatvien2@lab.vn",
            "Thử nghiệm viên 2",
            defaultPassword,
            new[] { RoleLabTechnician },
            "LH"  // Lab Hóa
        );

        // 8. Kế toán - thuộc Khoa Kế Toán
        await CreateUserIfNotExistsAsync(
            "ketoan",
            "ketoan@lab.vn",
            "Kế toán",
            defaultPassword,
            new[] { RoleAccountant },
            "KKT"  // Khoa Kế Toán
        );
    }

    private async Task LoadDepartmentMapAsync()
    {
        if (_departmentCodeToIdMap != null) return;

        var departments = await _departmentRepository.GetListAsync();
        _departmentCodeToIdMap = departments.ToDictionary(d => d.Code, d => d.Id);
    }

    private Guid? GetDepartmentId(string? departmentCode)
    {
        if (string.IsNullOrEmpty(departmentCode) || _departmentCodeToIdMap == null)
            return null;

        return _departmentCodeToIdMap.TryGetValue(departmentCode, out var id) ? id : null;
    }

    private async Task CreateUserIfNotExistsAsync(
        string userName,
        string email,
        string name,
        string password,
        string[] roleNames,
        string? departmentCode = null)
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

            // Set DepartmentId extra property
            var departmentId = GetDepartmentId(departmentCode);
            if (departmentId.HasValue)
            {
                user.SetProperty("DepartmentId", departmentId.Value);
            }

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
