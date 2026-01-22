using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.PermissionManagement;

namespace LabSpecimenManagement.RoleManagement;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.RoleManagement.Default)]
public class RoleManagementAppService : ApplicationService, IRoleManagementAppService
{
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IdentityRoleManager _roleManager;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IRepository<Department, Guid> _departmentRepository;

    // Permission groups configuration for UI display
    private static readonly List<PermissionGroupConfig> PermissionGroupConfigs = new()
    {
        // ==================== NGHIỆP VỤ CHÍNH ====================
        new PermissionGroupConfig("PhieuYeuCau", "Phiếu yêu cầu", new[]
        {
            (LabSpecimenManagementPermissions.TestRequests.Default, "Xem phiếu yêu cầu"),
            (LabSpecimenManagementPermissions.TestRequests.Create, "Tạo phiếu yêu cầu"),
            (LabSpecimenManagementPermissions.TestRequests.Edit, "Chỉnh sửa phiếu yêu cầu"),
            (LabSpecimenManagementPermissions.Specimens.Create, "Gửi mẫu"),
            (LabSpecimenManagementPermissions.Specimens.Edit, "Duyệt thay đổi"),
            (LabSpecimenManagementPermissions.TestRequests.Delete, "Hủy yêu cầu"),
        }),
        new PermissionGroupConfig("QuanLyCongViec", "Quản lý công việc", new[]
        {
            (LabSpecimenManagementPermissions.LaboratoryTasks.Default, "Xem danh sách công việc"),
            (LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll, "Xem chi tiết công việc"),
            (LabSpecimenManagementPermissions.Tests.Assign, "Phân công thử nghiệm"),
            (LabSpecimenManagementPermissions.LaboratoryTasks.Edit, "Thêm người hỗ trợ"),
            (LabSpecimenManagementPermissions.Tests.Edit, "Sửa phương pháp"),
            (LabSpecimenManagementPermissions.Tests.Review, "Duyệt kết quả"),
        }),
        new PermissionGroupConfig("QuanLyThuNghiem", "Quản lý thử nghiệm", new[]
        {
            (LabSpecimenManagementPermissions.Tests.Default, "Xem danh sách công việc"),
            (LabSpecimenManagementPermissions.Tests.Execute, "Xem chi tiết công việc"),
            (LabSpecimenManagementPermissions.TestResults.Create, "Nhập kết quả"),
            (LabSpecimenManagementPermissions.TestResults.Edit, "Sửa kết quả"),
            (LabSpecimenManagementPermissions.TestResults.Submit, "Gửi kết quả"),
        }),
        new PermissionGroupConfig("QuanLyChatLuong", "Quản lý chất lượng", new[]
        {
            (LabSpecimenManagementPermissions.TestResults.Review, "Xem kết quả đã duyệt kỹ thuật"),
            (LabSpecimenManagementPermissions.TestResults.Approve, "Phê duyệt chất lượng"),
            (LabSpecimenManagementPermissions.TestResults.Reject, "Trả hồ sơ chỉnh sửa"),
            (LabSpecimenManagementPermissions.Reports.Create, "Tạo report"),
        }),
        new PermissionGroupConfig("QuanLyPhieuThu", "Quản lý phiếu thu", new[]
        {
            (LabSpecimenManagementPermissions.Receipts.View, "Xem phiếu thu"),
            (LabSpecimenManagementPermissions.Receipts.Confirm, "Xác nhận phiếu thu"),
        }),

        // ==================== BÁO GIÁ ====================
        new PermissionGroupConfig("BaoGia", "Báo giá", new[]
        {
            (LabSpecimenManagementPermissions.Quotations.Default, "Xem báo giá"),
            (LabSpecimenManagementPermissions.Quotations.Create, "Tạo báo giá"),
            (LabSpecimenManagementPermissions.Quotations.Edit, "Sửa báo giá"),
            (LabSpecimenManagementPermissions.Quotations.Delete, "Xóa báo giá"),
        }),

        // ==================== QUẢN LÝ MẪU ====================
        new PermissionGroupConfig("QuanLyMau", "Quản lý mẫu", new[]
        {
            (LabSpecimenManagementPermissions.Specimens.Default, "Xem danh sách mẫu"),
            (LabSpecimenManagementPermissions.Specimens.Create, "Tạo mẫu"),
            (LabSpecimenManagementPermissions.Specimens.Edit, "Sửa thông tin mẫu"),
            (LabSpecimenManagementPermissions.Specimens.Delete, "Xóa mẫu"),
        }),

        // ==================== BÁO CÁO ====================
        new PermissionGroupConfig("BaoCao", "Báo cáo", new[]
        {
            (LabSpecimenManagementPermissions.Reports.Default, "Xem báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Create, "Tạo báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Edit, "Sửa báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Delete, "Xóa báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Approve, "Duyệt báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Export, "Xuất báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Send, "Gửi báo cáo"),
        }),

        // ==================== NCR ====================
        new PermissionGroupConfig("NCR", "Báo cáo NCR", new[]
        {
            (LabSpecimenManagementPermissions.NCRs.Default, "Xem NCR"),
            (LabSpecimenManagementPermissions.NCRs.Create, "Tạo NCR"),
            (LabSpecimenManagementPermissions.NCRs.Edit, "Sửa NCR"),
            (LabSpecimenManagementPermissions.NCRs.Delete, "Xóa NCR"),
            (LabSpecimenManagementPermissions.NCRs.Close, "Đóng NCR"),
            (LabSpecimenManagementPermissions.NCRs.Assign, "Gán NCR"),
        }),

        // ==================== THỐNG KÊ ====================
        new PermissionGroupConfig("ThongKe", "Thống kê", new[]
        {
            (LabSpecimenManagementPermissions.Dashboard.Default, "Xem dashboard"),
            (LabSpecimenManagementPermissions.Statistics.Default, "Xem thống kê"),
            (LabSpecimenManagementPermissions.Statistics.Export, "Xuất báo cáo thống kê"),
        }),

        // ==================== NHÀ THẦU PHỤ ====================
        new PermissionGroupConfig("NhaThauPhu", "Nhà thầu phụ", new[]
        {
            (LabSpecimenManagementPermissions.Subcontractors.Default, "Xem nhà thầu phụ"),
            (LabSpecimenManagementPermissions.Subcontractors.Create, "Tạo nhà thầu phụ"),
            (LabSpecimenManagementPermissions.Subcontractors.Edit, "Sửa nhà thầu phụ"),
            (LabSpecimenManagementPermissions.Subcontractors.Delete, "Xóa nhà thầu phụ"),
        }),

        // ==================== DỮ LIỆU CHỦ (MASTER DATA) ====================
        new PermissionGroupConfig("MasterData", "Dữ liệu chủ", new[]
        {
            // Chỉ tiêu (Indicators)
            (LabSpecimenManagementPermissions.Indicators.Default, "Xem chỉ tiêu"),
            (LabSpecimenManagementPermissions.Indicators.Create, "Tạo chỉ tiêu"),
            (LabSpecimenManagementPermissions.Indicators.Edit, "Sửa chỉ tiêu"),
            (LabSpecimenManagementPermissions.Indicators.Delete, "Xóa chỉ tiêu"),
            (LabSpecimenManagementPermissions.Indicators.Import, "Import chỉ tiêu"),
            // Nhóm chỉ tiêu (Indicator Groups)
            (LabSpecimenManagementPermissions.IndicatorGroups.Default, "Xem nhóm chỉ tiêu"),
            (LabSpecimenManagementPermissions.IndicatorGroups.Create, "Tạo nhóm chỉ tiêu"),
            (LabSpecimenManagementPermissions.IndicatorGroups.Edit, "Sửa nhóm chỉ tiêu"),
            (LabSpecimenManagementPermissions.IndicatorGroups.Delete, "Xóa nhóm chỉ tiêu"),
            // Loại mẫu (Specimen Types)
            (LabSpecimenManagementPermissions.SpecimenTypes.Default, "Xem loại mẫu"),
            (LabSpecimenManagementPermissions.SpecimenTypes.Create, "Tạo loại mẫu"),
            (LabSpecimenManagementPermissions.SpecimenTypes.Edit, "Sửa loại mẫu"),
            (LabSpecimenManagementPermissions.SpecimenTypes.Delete, "Xóa loại mẫu"),
            // Tiêu chuẩn (Standards)
            (LabSpecimenManagementPermissions.Standards.Default, "Xem tiêu chuẩn"),
            (LabSpecimenManagementPermissions.Standards.Create, "Tạo tiêu chuẩn"),
            (LabSpecimenManagementPermissions.Standards.Edit, "Sửa tiêu chuẩn"),
            (LabSpecimenManagementPermissions.Standards.Delete, "Xóa tiêu chuẩn"),
            // Phương pháp thử (Test Methods)
            (LabSpecimenManagementPermissions.TestMethods.Default, "Xem phương pháp thử"),
            (LabSpecimenManagementPermissions.TestMethods.Create, "Tạo phương pháp thử"),
            (LabSpecimenManagementPermissions.TestMethods.Edit, "Sửa phương pháp thử"),
            (LabSpecimenManagementPermissions.TestMethods.Delete, "Xóa phương pháp thử"),
            // Vị trí lưu trữ (Storage Locations)
            (LabSpecimenManagementPermissions.StorageLocations.Default, "Xem vị trí lưu trữ"),
            (LabSpecimenManagementPermissions.StorageLocations.Create, "Tạo vị trí lưu trữ"),
            (LabSpecimenManagementPermissions.StorageLocations.Edit, "Sửa vị trí lưu trữ"),
            (LabSpecimenManagementPermissions.StorageLocations.Delete, "Xóa vị trí lưu trữ"),
            // Khách hàng (Customers)
            (LabSpecimenManagementPermissions.Customers.Default, "Xem khách hàng"),
            (LabSpecimenManagementPermissions.Customers.Create, "Tạo khách hàng"),
            (LabSpecimenManagementPermissions.Customers.Edit, "Sửa khách hàng"),
            (LabSpecimenManagementPermissions.Customers.Delete, "Xóa khách hàng"),
            // Phòng ban (Departments)
            (LabSpecimenManagementPermissions.Departments.Default, "Xem phòng ban"),
            (LabSpecimenManagementPermissions.Departments.Create, "Tạo phòng ban"),
            (LabSpecimenManagementPermissions.Departments.Edit, "Sửa phòng ban"),
            (LabSpecimenManagementPermissions.Departments.Delete, "Xóa phòng ban"),
            // Khoa phòng/Lab (Laboratories)
            (LabSpecimenManagementPermissions.Laboratories.Default, "Xem khoa phòng"),
            (LabSpecimenManagementPermissions.Laboratories.Create, "Tạo khoa phòng"),
            (LabSpecimenManagementPermissions.Laboratories.Edit, "Sửa khoa phòng"),
            (LabSpecimenManagementPermissions.Laboratories.Delete, "Xóa khoa phòng"),
            // Tình trạng mẫu (Sample Conditions)
            (LabSpecimenManagementPermissions.SampleConditions.Default, "Xem tình trạng mẫu"),
            (LabSpecimenManagementPermissions.SampleConditions.Create, "Tạo tình trạng mẫu"),
            (LabSpecimenManagementPermissions.SampleConditions.Edit, "Sửa tình trạng mẫu"),
            (LabSpecimenManagementPermissions.SampleConditions.Delete, "Xóa tình trạng mẫu"),
        }),

        // ==================== QUẢN TRỊ HỆ THỐNG ====================
        new PermissionGroupConfig("QuanTriHeThong", "Quản trị hệ thống", new[]
        {
            // Người dùng (Users)
            (LabSpecimenManagementPermissions.Users.Default, "Xem người dùng"),
            (LabSpecimenManagementPermissions.Users.Create, "Tạo người dùng"),
            (LabSpecimenManagementPermissions.Users.Edit, "Sửa người dùng"),
            (LabSpecimenManagementPermissions.Users.Delete, "Xóa người dùng"),
            (LabSpecimenManagementPermissions.Users.ManageRoles, "Quản lý vai trò người dùng"),
            // Phân quyền (Role Management)
            (LabSpecimenManagementPermissions.RoleManagement.Default, "Xem phân quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.Create, "Tạo nhóm quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.Edit, "Sửa nhóm quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.Delete, "Xóa nhóm quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.ManagePermissions, "Quản lý quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.ManageUsers, "Quản lý người dùng trong nhóm"),
            // Cài đặt (Settings)
            (LabSpecimenManagementPermissions.Settings.Default, "Xem cài đặt"),
            (LabSpecimenManagementPermissions.Settings.Edit, "Sửa cài đặt"),
            // Lịch sử thay đổi (Audit Logs)
            (LabSpecimenManagementPermissions.AuditLogs.Default, "Xem lịch sử thay đổi"),
        }),

        // ==================== IMPORT DỮ LIỆU ====================
        new PermissionGroupConfig("Import", "Import dữ liệu", new[]
        {
            (LabSpecimenManagementPermissions.Import.Default, "Quyền import"),
            (LabSpecimenManagementPermissions.Import.TestRequests, "Import phiếu yêu cầu"),
            (LabSpecimenManagementPermissions.Import.Specimens, "Import mẫu"),
            (LabSpecimenManagementPermissions.Import.Customers, "Import khách hàng"),
            (LabSpecimenManagementPermissions.Import.TestMethods, "Import phương pháp thử"),
            (LabSpecimenManagementPermissions.Import.Standards, "Import tiêu chuẩn"),
        }),
    };

    public RoleManagementAppService(
        IIdentityRoleRepository roleRepository,
        IdentityRoleManager roleManager,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager,
        IPermissionDefinitionManager permissionDefinitionManager,
        IRepository<Department, Guid> departmentRepository)
    {
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _userRepository = userRepository;
        _userManager = userManager;
        _permissionManager = permissionManager;
        _permissionDefinitionManager = permissionDefinitionManager;
        _departmentRepository = departmentRepository;
    }

    #region Role CRUD

    public async Task<PagedResultDto<RoleDto>> GetListAsync(GetRolesInput input)
    {
        var roles = await _roleRepository.GetListAsync();
        var allUsers = await _userRepository.GetListAsync();

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.ToLowerInvariant();
            roles = roles.Where(r => r.Name.ToLowerInvariant().Contains(filter)).ToList();
        }

        // Sort (default: Id DESC - sequential GUID represents creation order)
        roles = input.Sorting?.ToLowerInvariant() switch
        {
            "name" => roles.OrderBy(r => r.Name).ToList(),
            "name desc" => roles.OrderByDescending(r => r.Name).ToList(),
            _ => roles.OrderByDescending(r => r.Id).ToList()
        };

        var totalCount = roles.Count;

        // Pagination
        var pagedRoles = roles
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        // Map to DTOs with user count
        var roleDtos = new List<RoleDto>();
        foreach (var role in pagedRoles)
        {
            var userCount = 0;
            foreach (var user in allUsers)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains(role.Name))
                {
                    userCount++;
                }
            }

            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                NormalizedName = role.NormalizedName,
                UserCount = userCount,
                IsStatic = role.IsStatic,
                IsDefault = role.IsDefault,
                IsPublic = role.IsPublic,
                ConcurrencyStamp = role.ConcurrencyStamp
            });
        }

        return new PagedResultDto<RoleDto>(totalCount, roleDtos);
    }

    public async Task<RoleDetailDto> GetAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        // Get users in this role
        var allUsers = await _userRepository.GetListAsync();
        var roleUsers = new List<RoleUserDto>();
        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        foreach (var user in allUsers)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains(role.Name))
            {
                var departmentId = user.GetProperty<Guid?>("DepartmentId");
                roleUsers.Add(new RoleUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = $"{user.Surname} {user.Name}".Trim(),
                    Email = user.Email,
                    DepartmentId = departmentId,
                    DepartmentName = departmentId.HasValue && departmentDict.ContainsKey(departmentId.Value)
                        ? departmentDict[departmentId.Value]
                        : null,
                    Position = user.GetProperty<string?>("Position"),
                    IsActive = user.IsActive
                });
            }
        }

        // Get permissions for this role
        var permissionGroups = await GetRolePermissionsInternalAsync(role.Name);

        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            UserCount = roleUsers.Count,
            IsStatic = role.IsStatic,
            IsDefault = role.IsDefault,
            IsPublic = role.IsPublic,
            ConcurrencyStamp = role.ConcurrencyStamp,
            Users = roleUsers,
            PermissionGroups = permissionGroups
        };
    }

    [Authorize(LabSpecimenManagementPermissions.RoleManagement.Create)]
    public async Task<RoleDto> CreateAsync(CreateRoleInput input)
    {
        // Check if role name already exists
        var existingRole = await _roleRepository.FindByNormalizedNameAsync(input.Name.ToUpperInvariant());
        if (existingRole != null)
        {
            throw new UserFriendlyException($"Nhóm quyền '{input.Name}' đã tồn tại");
        }

        var role = new IdentityRole(GuidGenerator.Create(), input.Name)
        {
            IsDefault = input.IsDefault,
            IsPublic = input.IsPublic
        };

        await _roleManager.CreateAsync(role);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            UserCount = 0,
            IsStatic = role.IsStatic,
            IsDefault = role.IsDefault,
            IsPublic = role.IsPublic,
            ConcurrencyStamp = role.ConcurrencyStamp
        };
    }

    [Authorize(LabSpecimenManagementPermissions.RoleManagement.Edit)]
    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleInput input)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        if (role.IsStatic)
        {
            throw new UserFriendlyException("Không thể chỉnh sửa nhóm quyền hệ thống");
        }

        // Check concurrency
        if (!string.IsNullOrEmpty(input.ConcurrencyStamp) && role.ConcurrencyStamp != input.ConcurrencyStamp)
        {
            throw new UserFriendlyException("Dữ liệu đã bị thay đổi bởi người khác, vui lòng tải lại trang");
        }

        // Check if new name already exists (if changed)
        if (!role.Name.Equals(input.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existingRole = await _roleRepository.FindByNormalizedNameAsync(input.Name.ToUpperInvariant());
            if (existingRole != null)
            {
                throw new UserFriendlyException($"Nhóm quyền '{input.Name}' đã tồn tại");
            }
        }

        await _roleManager.SetRoleNameAsync(role, input.Name);
        role.IsDefault = input.IsDefault;
        role.IsPublic = input.IsPublic;

        await _roleManager.UpdateAsync(role);

        // Get user count
        var allUsers = await _userRepository.GetListAsync();
        var userCount = 0;
        foreach (var user in allUsers)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains(role.Name))
            {
                userCount++;
            }
        }

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            UserCount = userCount,
            IsStatic = role.IsStatic,
            IsDefault = role.IsDefault,
            IsPublic = role.IsPublic,
            ConcurrencyStamp = role.ConcurrencyStamp
        };
    }

    [Authorize(LabSpecimenManagementPermissions.RoleManagement.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var role = await _roleRepository.GetAsync(id);
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        if (role.IsStatic)
        {
            throw new UserFriendlyException("Không thể xóa nhóm quyền hệ thống");
        }

        // Check if any users are in this role
        var allUsers = await _userRepository.GetListAsync();
        foreach (var user in allUsers)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains(role.Name))
            {
                throw new UserFriendlyException($"Không thể xóa nhóm quyền vì còn người dùng đang sử dụng");
            }
        }

        await _roleManager.DeleteAsync(role);
    }

    #endregion

    #region Permissions

    public Task<List<PermissionGroupDto>> GetAllPermissionsAsync()
    {
        var result = PermissionGroupConfigs.Select(config => new PermissionGroupDto
        {
            Name = config.Name,
            DisplayName = config.DisplayName,
            Permissions = config.Permissions.Select(p => new PermissionDto
            {
                Name = p.PermissionName,
                DisplayName = p.DisplayName,
                IsGranted = false
            }).ToList()
        }).ToList();

        return Task.FromResult(result);
    }

    public async Task<List<PermissionGroupDto>> GetRolePermissionsAsync(string roleName)
    {
        return await GetRolePermissionsInternalAsync(roleName);
    }

    [Authorize(LabSpecimenManagementPermissions.RoleManagement.ManagePermissions)]
    public async Task UpdateRolePermissionsAsync(string roleName, UpdateRolePermissionsInput input)
    {
        var role = await _roleRepository.FindByNormalizedNameAsync(roleName.ToUpperInvariant());
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        // Get all permissions from config
        var allPermissions = PermissionGroupConfigs
            .SelectMany(g => g.Permissions.Select(p => p.PermissionName))
            .Distinct()
            .ToList();

        // Update each permission
        foreach (var permissionName in allPermissions)
        {
            var isGranted = input.Permissions.Contains(permissionName);
            await _permissionManager.SetForRoleAsync(roleName, permissionName, isGranted);
        }
    }

    private async Task<List<PermissionGroupDto>> GetRolePermissionsInternalAsync(string roleName)
    {
        var result = new List<PermissionGroupDto>();

        foreach (var config in PermissionGroupConfigs)
        {
            var group = new PermissionGroupDto
            {
                Name = config.Name,
                DisplayName = config.DisplayName,
                Permissions = new List<PermissionDto>()
            };

            foreach (var (permissionName, displayName) in config.Permissions)
            {
                var isGranted = await _permissionManager.GetForRoleAsync(roleName, permissionName);
                group.Permissions.Add(new PermissionDto
                {
                    Name = permissionName,
                    DisplayName = displayName,
                    IsGranted = isGranted.IsGranted
                });
            }

            result.Add(group);
        }

        return result;
    }

    #endregion

    #region Users

    public async Task<PagedResultDto<RoleUserDto>> GetRoleUsersAsync(Guid roleId, GetRoleUsersInput input)
    {
        var role = await _roleRepository.GetAsync(roleId);
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        var allUsers = await _userRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var roleUsers = new List<RoleUserDto>();
        foreach (var user in allUsers)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains(role.Name))
            {
                var departmentId = user.GetProperty<Guid?>("DepartmentId");
                var fullName = $"{user.Surname} {user.Name}".Trim();

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(input.Filter))
                {
                    var filter = input.Filter.ToLowerInvariant();
                    if (!user.UserName!.ToLowerInvariant().Contains(filter) &&
                        !fullName.ToLowerInvariant().Contains(filter) &&
                        !(user.Email?.ToLowerInvariant().Contains(filter) ?? false))
                    {
                        continue;
                    }
                }

                roleUsers.Add(new RoleUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = fullName,
                    Email = user.Email,
                    DepartmentId = departmentId,
                    DepartmentName = departmentId.HasValue && departmentDict.ContainsKey(departmentId.Value)
                        ? departmentDict[departmentId.Value]
                        : null,
                    Position = user.GetProperty<string?>("Position"),
                    IsActive = user.IsActive
                });
            }
        }

        var totalCount = roleUsers.Count;

        // Sort
        roleUsers = input.Sorting?.ToLowerInvariant() switch
        {
            "username" => roleUsers.OrderBy(u => u.UserName).ToList(),
            "username desc" => roleUsers.OrderByDescending(u => u.UserName).ToList(),
            "fullname" => roleUsers.OrderBy(u => u.FullName).ToList(),
            "fullname desc" => roleUsers.OrderByDescending(u => u.FullName).ToList(),
            _ => roleUsers.OrderBy(u => u.UserName).ToList()
        };

        // Pagination
        var pagedUsers = roleUsers
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<RoleUserDto>(totalCount, pagedUsers);
    }

    [Authorize(LabSpecimenManagementPermissions.RoleManagement.ManageUsers)]
    public async Task AddUserToRoleAsync(Guid roleId, Guid userId)
    {
        var role = await _roleRepository.GetAsync(roleId);
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        var user = await _userRepository.GetAsync(userId);
        if (user == null)
        {
            throw new UserFriendlyException("Không tìm thấy người dùng");
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Contains(role.Name))
        {
            throw new UserFriendlyException("Người dùng đã thuộc nhóm quyền này");
        }

        await _userManager.AddToRoleAsync(user, role.Name);
    }

    [Authorize(LabSpecimenManagementPermissions.RoleManagement.ManageUsers)]
    public async Task RemoveUserFromRoleAsync(Guid roleId, Guid userId)
    {
        var role = await _roleRepository.GetAsync(roleId);
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        var user = await _userRepository.GetAsync(userId);
        if (user == null)
        {
            throw new UserFriendlyException("Không tìm thấy người dùng");
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        if (!userRoles.Contains(role.Name))
        {
            throw new UserFriendlyException("Người dùng không thuộc nhóm quyền này");
        }

        await _userManager.RemoveFromRoleAsync(user, role.Name);
    }

    public async Task<List<RoleUserDto>> GetAvailableUsersAsync(Guid roleId, string? filter)
    {
        var role = await _roleRepository.GetAsync(roleId);
        if (role == null)
        {
            throw new UserFriendlyException("Không tìm thấy nhóm quyền");
        }

        var allUsers = await _userRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var availableUsers = new List<RoleUserDto>();
        foreach (var user in allUsers)
        {
            // Skip inactive users
            if (!user.IsActive) continue;

            var userRoles = await _userManager.GetRolesAsync(user);
            // Skip users already in this role
            if (userRoles.Contains(role.Name)) continue;

            var departmentId = user.GetProperty<Guid?>("DepartmentId");
            var fullName = $"{user.Surname} {user.Name}".Trim();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var filterLower = filter.ToLowerInvariant();
                if (!user.UserName!.ToLowerInvariant().Contains(filterLower) &&
                    !fullName.ToLowerInvariant().Contains(filterLower) &&
                    !(user.Email?.ToLowerInvariant().Contains(filterLower) ?? false))
                {
                    continue;
                }
            }

            availableUsers.Add(new RoleUserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = fullName,
                Email = user.Email,
                DepartmentId = departmentId,
                DepartmentName = departmentId.HasValue && departmentDict.ContainsKey(departmentId.Value)
                    ? departmentDict[departmentId.Value]
                    : null,
                Position = user.GetProperty<string?>("Position"),
                IsActive = user.IsActive
            });
        }

        return availableUsers.OrderBy(u => u.UserName).Take(50).ToList();
    }

    #endregion

    #region Helper Classes

    private class PermissionGroupConfig
    {
        public string Name { get; }
        public string DisplayName { get; }
        public (string PermissionName, string DisplayName)[] Permissions { get; }

        public PermissionGroupConfig(string name, string displayName, (string, string)[] permissions)
        {
            Name = name;
            DisplayName = displayName;
            Permissions = permissions;
        }
    }

    #endregion
}
