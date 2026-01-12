using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace LabSpecimenManagement.Users;

[RemoteService(IsEnabled = false)]
public class UserLookupAppService : ApplicationService, IUserLookupAppService
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;

    public UserLookupAppService(
        IIdentityUserRepository userRepository,
        IRepository<Department, Guid> departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<ListResultDto<UserLookupDto>> GetAllAsync()
    {
        var users = await _userRepository.GetListAsync();
        var activeUsers = users.Where(u => u.IsActive).ToList();

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var dtos = activeUsers.Select(u => MapToLookupDto(u, departmentDict)).ToList();

        return new ListResultDto<UserLookupDto>(dtos);
    }

    public async Task<ListResultDto<UserLookupDto>> SearchAsync(string? filter)
    {
        var users = await _userRepository.GetListAsync();
        var activeUsers = users.Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            activeUsers = activeUsers.WhereContainsIgnoreCase(
                filter,
                u => u.UserName,
                u => u.Name,
                u => u.Surname,
                u => u.Email
            );
        }

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var dtos = activeUsers.Select(u => MapToLookupDto(u, departmentDict)).ToList();

        return new ListResultDto<UserLookupDto>(dtos);
    }

    public async Task<UserLookupDto?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.FindAsync(id);
        if (user == null)
        {
            return null;
        }

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        return MapToLookupDto(user, departmentDict);
    }

    public async Task<ListResultDto<UserLookupDto>> GetByIdsAsync(List<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            return new ListResultDto<UserLookupDto>(new List<UserLookupDto>());
        }

        var users = await _userRepository.GetListAsync();
        var filteredUsers = users.Where(u => ids.Contains(u.Id)).ToList();

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var dtos = filteredUsers.Select(u => MapToLookupDto(u, departmentDict)).ToList();

        return new ListResultDto<UserLookupDto>(dtos);
    }

    public async Task<ListResultDto<UserLookupDto>> GetVicePresidentsAsync()
    {
        var users = await _userRepository.GetListAsync();
        var vicePresidents = users
            .Where(u => u.IsActive)
            .Where(u =>
            {
                var position = u.GetProperty<string>("Position");
                return !string.IsNullOrWhiteSpace(position) &&
                       position.Contains("Phó Giám đốc", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var dtos = vicePresidents.Select(u => MapToLookupDto(u, departmentDict)).ToList();

        return new ListResultDto<UserLookupDto>(dtos);
    }

    private UserLookupDto MapToLookupDto(IdentityUser user, Dictionary<Guid, string> departmentDict)
    {
        var departmentId = user.GetProperty<Guid?>("DepartmentId");
        string? departmentName = null;
        if (departmentId.HasValue && departmentDict.ContainsKey(departmentId.Value))
        {
            departmentName = departmentDict[departmentId.Value];
        }

        var fullName = $"{user.Surname} {user.Name}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName)
            ? user.UserName
            : $"{fullName} ({user.UserName})";

        return new UserLookupDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name,
            Surname = user.Surname,
            DisplayName = displayName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Position = user.GetProperty<string>("Position"),
            DepartmentName = departmentName,
            IsActive = user.IsActive
        };
    }
}
