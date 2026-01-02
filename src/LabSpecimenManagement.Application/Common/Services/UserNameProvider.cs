using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Implementation of user name provider service
/// Provides centralized user name lookup functionality
/// </summary>
public class UserNameProvider : IUserNameProvider, ITransientDependency
{
    private readonly IIdentityUserRepository _userRepository;

    public UserNameProvider(IIdentityUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid?> userIds)
    {
        var validIds = userIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (validIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var users = await _userRepository.GetListByIdsAsync(validIds);
        return users.ToDictionary(
            u => u.Id,
            u => FormatUserName(u.Surname, u.Name, u.UserName)
        );
    }

    /// <inheritdoc/>
    public async Task<string?> GetUserNameAsync(Guid? userId)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        var user = await _userRepository.FindAsync(userId.Value);
        if (user == null)
        {
            return null;
        }

        return FormatUserName(user.Surname, user.Name, user.UserName);
    }

    /// <inheritdoc/>
    public async Task EnrichWithUserNamesAsync<TDto>(
        IEnumerable<TDto> dtos,
        Func<TDto, Guid?> userIdSelector,
        Action<TDto, string> userNameSetter)
    {
        var dtoList = dtos.ToList();
        if (dtoList.Count == 0)
        {
            return;
        }

        var userIds = dtoList.Select(userIdSelector);
        var userNames = await GetUserNamesAsync(userIds);

        foreach (var dto in dtoList)
        {
            var userId = userIdSelector(dto);
            if (userId.HasValue && userNames.TryGetValue(userId.Value, out var userName))
            {
                userNameSetter(dto, userName);
            }
        }
    }

    /// <summary>
    /// Formats user name from surname, name, and username
    /// </summary>
    private static string FormatUserName(string? surname, string? name, string? userName)
    {
        var fullName = $"{surname} {name}".Trim();
        return string.IsNullOrEmpty(fullName) ? userName ?? string.Empty : fullName;
    }
}
