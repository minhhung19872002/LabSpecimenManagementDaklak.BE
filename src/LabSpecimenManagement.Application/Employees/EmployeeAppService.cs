using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Email;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace LabSpecimenManagement.Employees;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.Users.Default)]
public class EmployeeAppService : ApplicationService, IEmployeeAppService
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IEmailService _emailService;

    public EmployeeAppService(
        IIdentityUserRepository userRepository,
        IdentityUserManager userManager,
        IRepository<Department, Guid> departmentRepository,
        IIdentityRoleRepository roleRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _departmentRepository = departmentRepository;
        _roleRepository = roleRepository;
        _emailService = emailService;
    }

    public async Task<PagedResultDto<EmployeeDto>> GetListAsync(GetEmployeesInput input)
    {
        // Get all users first for filtering (ABP Identity doesn't support advanced filters)
        var allUsers = await _userRepository.GetListAsync();

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        // Build filtered list with all conditions
        var filteredUsers = allUsers.AsEnumerable();

        // Filter by search text (name, username, email, phone)
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var searchText = input.Filter.ToLower();
            filteredUsers = filteredUsers.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(searchText)) ||
                (u.Name != null && u.Name.ToLower().Contains(searchText)) ||
                (u.Surname != null && u.Surname.ToLower().Contains(searchText)) ||
                (u.Email != null && u.Email.ToLower().Contains(searchText)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(searchText)) ||
                ($"{u.Surname} {u.Name}".ToLower().Contains(searchText))
            );
        }

        // Filter by department if specified
        if (input.DepartmentId.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.GetProperty<Guid?>("DepartmentId") == input.DepartmentId.Value);
        }

        // Filter by active status if specified
        if (input.IsActive.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.IsActive == input.IsActive.Value);
        }

        // Convert to list for role filtering
        var usersList = filteredUsers.ToList();

        // Filter by role if specified
        if (!string.IsNullOrEmpty(input.Role))
        {
            var usersWithRole = new List<IdentityUser>();
            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(input.Role))
                {
                    usersWithRole.Add(user);
                }
            }
            usersList = usersWithRole;
        }

        // Get total count after all filters
        var totalCount = usersList.Count;

        // Apply sorting
        var sorting = input.Sorting ?? "CreationTime DESC";
        if (sorting.Contains("CreationTime", StringComparison.OrdinalIgnoreCase))
        {
            usersList = sorting.Contains("DESC", StringComparison.OrdinalIgnoreCase)
                ? usersList.OrderByDescending(u => u.CreationTime).ToList()
                : usersList.OrderBy(u => u.CreationTime).ToList();
        }
        else if (sorting.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            usersList = sorting.Contains("DESC", StringComparison.OrdinalIgnoreCase)
                ? usersList.OrderByDescending(u => u.UserName).ToList()
                : usersList.OrderBy(u => u.UserName).ToList();
        }

        // Apply pagination
        var pagedUsers = usersList
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = new List<EmployeeDto>();
        foreach (var user in pagedUsers)
        {
            var dto = await MapToDtoAsync(user, departmentDict);
            dtos.Add(dto);
        }

        return new PagedResultDto<EmployeeDto>(totalCount, dtos);
    }

    public async Task<EmployeeDto> GetAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        return await MapToDtoAsync(user, departmentDict);
    }

    [Authorize(LabSpecimenManagementPermissions.Users.Create)]
    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto input)
    {
        // Parse fullName to get Name and Surname
        var nameParts = input.FullName.Trim().Split(' ', 2);
        var surname = nameParts.Length > 1 ? nameParts[0] : string.Empty;
        var name = nameParts.Length > 1 ? nameParts[1] : nameParts[0];

        var user = new IdentityUser(
            GuidGenerator.Create(),
            input.Username,
            input.Email,
            CurrentTenant.Id
        );

        user.Name = name;
        user.Surname = surname;
        user.SetPhoneNumber(input.Phone ?? string.Empty, false);
        user.SetIsActive(input.IsActive);

        // Set extra properties
        user.SetProperty("DepartmentId", input.DepartmentId);
        user.SetProperty("Position", input.Position ?? string.Empty);

        // Generate a random password
        var password = GenerateRandomPassword();

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Assign role
        if (!string.IsNullOrEmpty(input.Role))
        {
            await _userManager.AddToRoleAsync(user, input.Role);
        }

        // Send welcome email with credentials
        try
        {
            await _emailService.SendAccountCreatedEmailAsync(
                input.Email,
                input.FullName,
                input.Username,
                password
            );
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send welcome email to {Email}", input.Email);
            // Don't throw - user was created successfully, just email failed
        }

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        return await MapToDtoAsync(user, departmentDict);
    }

    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto input)
    {
        var user = await _userRepository.GetAsync(id);

        if (!string.IsNullOrEmpty(input.FullName))
        {
            var nameParts = input.FullName.Trim().Split(' ', 2);
            user.Surname = nameParts.Length > 1 ? nameParts[0] : string.Empty;
            user.Name = nameParts.Length > 1 ? nameParts[1] : nameParts[0];
        }

        if (!string.IsNullOrEmpty(input.Email))
        {
            await _userManager.SetEmailAsync(user, input.Email);
        }

        if (input.Phone != null)
        {
            user.SetPhoneNumber(input.Phone, false);
        }

        if (input.IsActive.HasValue)
        {
            user.SetIsActive(input.IsActive.Value);
        }

        if (input.DepartmentId.HasValue)
        {
            user.SetProperty("DepartmentId", input.DepartmentId.Value);
        }

        if (input.Position != null)
        {
            user.SetProperty("Position", input.Position);
        }

        // Update role if specified
        if (!string.IsNullOrEmpty(input.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, input.Role);
        }

        await _userManager.UpdateAsync(user);

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        return await MapToDtoAsync(user, departmentDict);
    }

    [Authorize(LabSpecimenManagementPermissions.Users.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        await _userManager.DeleteAsync(user);
    }

    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public async Task<EmployeeDto> ToggleActiveAsync(Guid id, bool isActive)
    {
        var user = await _userRepository.GetAsync(id);
        user.SetIsActive(isActive);
        await _userManager.UpdateAsync(user);

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        return await MapToDtoAsync(user, departmentDict);
    }

    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public async Task<ResetPasswordResultDto> ResetPasswordAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        var newPassword = GenerateRandomPassword();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            throw new UserFriendlyException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Send password reset email
        var fullName = $"{user.Surname} {user.Name}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = user.UserName ?? "User";
        }

        try
        {
            await _emailService.SendPasswordResetEmailAsync(
                user.Email!,
                fullName,
                user.UserName!,
                newPassword
            );
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
            // Don't throw - password was reset successfully, just email failed
        }

        return new ResetPasswordResultDto { Password = newPassword };
    }

    private async Task<EmployeeDto> MapToDtoAsync(IdentityUser user, Dictionary<Guid, string> departmentDict)
    {
        var departmentId = user.GetProperty<Guid?>("DepartmentId");
        string? departmentName = null;
        if (departmentId.HasValue && departmentDict.ContainsKey(departmentId.Value))
        {
            departmentName = departmentDict[departmentId.Value];
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault();

        var fullName = $"{user.Surname} {user.Name}".Trim();

        return new EmployeeDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber,
            Role = primaryRole,
            Department = departmentName,
            DepartmentId = departmentId,
            Position = user.GetProperty<string>("Position"),
            IsActive = user.IsActive,
            CreatedAt = user.CreationTime,
            LastLogin = user.GetProperty<DateTime?>("LastLoginTime")
        };
    }

    private string GenerateRandomPassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var random = new Random();
        var password = new char[12];

        // Ensure at least one of each type
        password[0] = uppercase[random.Next(uppercase.Length)];
        password[1] = lowercase[random.Next(lowercase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];

        // Fill the rest randomly
        var allChars = uppercase + lowercase + digits + special;
        for (int i = 4; i < 12; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle
        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }
}
