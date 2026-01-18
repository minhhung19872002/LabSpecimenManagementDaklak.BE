using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
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
            filteredUsers = filteredUsers.WhereContainsIgnoreCase(
                input.Filter,
                u => u.UserName,
                u => u.Name,
                u => u.Surname,
                u => u.Email,
                u => u.PhoneNumber,
                u => $"{u.Surname} {u.Name}"
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
        var sorting = input.Sorting ?? AppConstants.DefaultEmployeeSorting;
        if (sorting.Contains(SortingFields.CreationTime, StringComparison.OrdinalIgnoreCase))
        {
            usersList = sorting.Contains(AppConstants.SortDescending, StringComparison.OrdinalIgnoreCase)
                ? usersList.OrderByDescending(u => u.CreationTime).ToList()
                : usersList.OrderBy(u => u.CreationTime).ToList();
        }
        else if (sorting.Contains("UserName", StringComparison.OrdinalIgnoreCase))
        {
            usersList = sorting.Contains(AppConstants.SortDescending, StringComparison.OrdinalIgnoreCase)
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

        // Set account expiration if temporary account
        if (input.IsTemporaryAccount && input.AccountExpirationDate.HasValue)
        {
            // Store expiration date as end of day (23:59:59) for proper comparison
            var expirationDate = input.AccountExpirationDate.Value.Date.AddDays(1).AddTicks(-1);
            user.SetProperty("AccountExpirationDate", expirationDate);
        }

        // Determine password based on mode
        string password;
        if (input.UseManualPassword)
        {
            // Validate manual password
            if (string.IsNullOrEmpty(input.Password))
            {
                throw new ValidationException("password", "Vui lòng nhập mật khẩu");
            }
            if (input.Password != input.ConfirmPassword)
            {
                throw new ValidationException("confirmPassword", "Mật khẩu xác nhận không khớp");
            }
            password = input.Password;
        }
        else
        {
            // Auto-generate password
            password = GenerateRandomPassword();
        }

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new ValidationException("user", string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Assign role
        if (!string.IsNullOrEmpty(input.Role))
        {
            await _userManager.AddToRoleAsync(user, input.Role);
        }

        // Send welcome email with credentials (only for auto-generated password)
        if (!input.UseManualPassword)
        {
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

        // Update account expiration
        if (input.IsTemporaryAccount.HasValue)
        {
            if (input.IsTemporaryAccount.Value)
            {
                // Set expiration date
                if (input.AccountExpirationDate.HasValue)
                {
                    // Store expiration date as end of day (23:59:59) for proper comparison
                    var expirationDate = input.AccountExpirationDate.Value.Date.AddDays(1).AddTicks(-1);
                    user.SetProperty("AccountExpirationDate", expirationDate);
                }
                else
                {
                    throw new ValidationException("accountExpirationDate", "Vui lòng chọn ngày hết hạn cho tài khoản tạm thời");
                }
            }
            else
            {
                // Remove expiration (make permanent)
                user.RemoveProperty("AccountExpirationDate");
            }
        }
        else if (input.AccountExpirationDate.HasValue)
        {
            // Update expiration date only
            if (input.AccountExpirationDate.Value <= DateTime.Now)
            {
                throw new ValidationException("accountExpirationDate", "Ngày hết hạn phải lớn hơn ngày hiện tại");
            }
            user.SetProperty("AccountExpirationDate", input.AccountExpirationDate.Value);
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
    public async Task<ResetPasswordResultDto> ResetPasswordAsync(Guid id, ResetPasswordDto input)
    {
        var user = await _userRepository.GetAsync(id);

        // Determine password based on mode
        string newPassword;
        if (input.UseManualPassword)
        {
            // Validate manual password
            if (string.IsNullOrEmpty(input.Password))
            {
                throw new ValidationException("password", "Vui lòng nhập mật khẩu");
            }
            if (input.Password != input.ConfirmPassword)
            {
                throw new ValidationException("confirmPassword", "Mật khẩu xác nhận không khớp");
            }
            newPassword = input.Password;
        }
        else
        {
            // Auto-generate password
            newPassword = GenerateRandomPassword();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            throw new ValidationException("password", string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Send password reset email (only for auto-generated password)
        if (!input.UseManualPassword)
        {
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
            LastLogin = user.GetProperty<DateTime?>("LastLoginTime"),
            AccountExpirationDate = user.GetProperty<DateTime?>("AccountExpirationDate")
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

    /// <summary>
    /// Deactivate all accounts that have expired.
    /// Called by background job.
    /// </summary>
    public async Task<int> DeactivateExpiredAccountsAsync()
    {
        var allUsers = await _userRepository.GetListAsync();
        var now = DateTime.Now;
        var deactivatedCount = 0;

        foreach (var user in allUsers)
        {
            if (!user.IsActive) continue;

            var expirationDate = user.GetProperty<DateTime?>("AccountExpirationDate");
            if (expirationDate.HasValue && expirationDate.Value <= now)
            {
                user.SetIsActive(false);
                await _userManager.UpdateAsync(user);
                deactivatedCount++;
                Logger.LogInformation("Deactivated expired account: {Username} (expired at {ExpirationDate})",
                    user.UserName, expirationDate.Value);
            }
        }

        return deactivatedCount;
    }

    /// <summary>
    /// Check if a user's account has expired.
    /// Returns true if expired, false if still valid or no expiration set.
    /// </summary>
    public async Task<bool> IsAccountExpiredAsync(Guid userId)
    {
        var user = await _userRepository.GetAsync(userId);
        var expirationDate = user.GetProperty<DateTime?>("AccountExpirationDate");

        if (!expirationDate.HasValue) return false;

        var isExpired = expirationDate.Value <= DateTime.Now;

        // Auto-deactivate if expired
        if (isExpired && user.IsActive)
        {
            user.SetIsActive(false);
            await _userManager.UpdateAsync(user);
            Logger.LogInformation("Auto-deactivated expired account on access check: {Username}", user.UserName);
        }

        return isExpired;
    }
}
