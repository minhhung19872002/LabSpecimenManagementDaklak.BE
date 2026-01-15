using System;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Employees;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for Employee entity (uses ABP Identity User)
/// </summary>
public class EmployeeSyncHandler : ISyncEntityHandler, ITransientDependency
{
    private readonly IEmployeeAppService _employeeAppService;
    private readonly IIdentityUserRepository _userRepository;

    public EmployeeSyncHandler(
        IEmployeeAppService employeeAppService,
        IIdentityUserRepository userRepository)
    {
        _employeeAppService = employeeAppService;
        _userRepository = userRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.Employee;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateEmployeeDto>(data);
        var result = await _employeeAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        var updateDto = DeserializeData<UpdateEmployeeDto>(data);
        var result = await _employeeAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _employeeAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _employeeAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _userRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _userRepository.FindAsync(serverId);
        return entity != null;
    }

    private static T DeserializeData<T>(object data)
    {
        if (data is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }

        if (data is T typedData)
        {
            return typedData;
        }

        var json = JsonSerializer.Serialize(data);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
