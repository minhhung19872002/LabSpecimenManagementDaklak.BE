using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.RoleWarehouses;
using LabSpecimenManagement.Shared;
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
    private readonly IRepository<RoleWarehouse, Guid> _roleWarehouseRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;

    // Use shared permission group configs (single source of truth)
    private static List<PermissionGroupConfig> PermissionGroupConfigs => Shared.PermissionGroupConfigs.All;

    public RoleManagementAppService(
        IIdentityRoleRepository roleRepository,
        IdentityRoleManager roleManager,
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IPermissionManager permissionManager,
        IPermissionDefinitionManager permissionDefinitionManager,
        IRepository<Department, Guid> departmentRepository,
        IRepository<RoleWarehouse, Guid> roleWarehouseRepository,
        IRepository<Warehouse, Guid> warehouseRepository)
    {
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _userRepository = userRepository;
        _userManager = userManager;
        _permissionManager = permissionManager;
        _permissionDefinitionManager = permissionDefinitionManager;
        _departmentRepository = departmentRepository;
        _roleWarehouseRepository = roleWarehouseRepository;
        _warehouseRepository = warehouseRepository;
    }

    #region Role CRUD

    public async Task<PagedResultDto<RoleDto>> GetListAsync(GetRolesInput input)
    {
        var roles = await _roleRepository.GetListAsync();
        var allUsers = await _userRepository.GetListAsync();

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
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

    public async Task<List<RoleUserDto>> GetRoleUsersAsync(Guid roleId)
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

        return roleUsers.OrderBy(u => u.UserName).ToList();
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
                var filterLower = filter.Trim().ToLowerInvariant();
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

    #region Warehouse Assignments

    public async Task<List<RoleWarehouseDto>> GetRoleWarehousesAsync(Guid roleId)
    {
        var rwQuery = await _roleWarehouseRepository.GetQueryableAsync();
        var whQuery = await _warehouseRepository.GetQueryableAsync();

        var result = (from rw in rwQuery
                      join wh in whQuery on rw.WarehouseId equals wh.Id
                      where rw.RoleId == roleId
                      select new RoleWarehouseDto
                      {
                          Id = rw.Id,
                          RoleId = rw.RoleId,
                          WarehouseId = rw.WarehouseId,
                          WarehouseName = wh.Name,
                          WarehouseCode = wh.Code,
                          WarehouseLevel = wh.Level
                      }).ToList();

        return result;
    }

    [Authorize(LabSpecimenManagementPermissions.RoleManagement.ManageUsers)]
    public async Task AssignRoleWarehousesAsync(Guid roleId, AssignRoleWarehousesInput input)
    {
        var role = await _roleRepository.GetAsync(roleId);
        if (role == null)
            throw new UserFriendlyException("Không tìm thấy nhóm quyền.");

        var rwQuery = await _roleWarehouseRepository.GetQueryableAsync();
        var existing = rwQuery.Where(x => x.RoleId == roleId).ToList();
        var inputIds = input.WarehouseIds.ToHashSet();

        var toDelete = existing.Where(x => !inputIds.Contains(x.WarehouseId)).ToList();
        foreach (var item in toDelete)
            await _roleWarehouseRepository.DeleteAsync(item.Id);

        var existingIds = existing.Select(x => x.WarehouseId).ToHashSet();
        var toAdd = inputIds.Where(id => !existingIds.Contains(id)).ToList();
        foreach (var warehouseId in toAdd)
        {
            await _roleWarehouseRepository.InsertAsync(
                new RoleWarehouse(GuidGenerator.Create(), roleId, warehouseId));
        }
    }

    #endregion
}
