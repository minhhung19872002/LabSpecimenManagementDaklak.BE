using System;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.NCRs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for NCR (Non-Conformance Report) entity
/// </summary>
public class NCRSyncHandler : ISyncEntityHandler, ITransientDependency
{
    private readonly INCRAppService _ncrAppService;
    private readonly IRepository<NCR, Guid> _ncrRepository;

    public NCRSyncHandler(
        INCRAppService ncrAppService,
        IRepository<NCR, Guid> ncrRepository)
    {
        _ncrAppService = ncrAppService;
        _ncrRepository = ncrRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.NCR;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateNCRDto>(data);
        var result = await _ncrAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        var updateDto = DeserializeData<UpdateNCRDto>(data);
        var result = await _ncrAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _ncrAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _ncrAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _ncrRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _ncrRepository.FindAsync(serverId);
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
