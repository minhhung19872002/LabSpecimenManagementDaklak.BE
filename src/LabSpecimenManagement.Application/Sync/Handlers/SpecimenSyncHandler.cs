using System;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Specimens;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for Specimen entity
/// </summary>
public class SpecimenSyncHandler : ISyncEntityHandler, ITransientDependency
{
    private readonly ISpecimenAppService _specimenAppService;
    private readonly IRepository<Specimen, Guid> _specimenRepository;

    public SpecimenSyncHandler(
        ISpecimenAppService specimenAppService,
        IRepository<Specimen, Guid> specimenRepository)
    {
        _specimenAppService = specimenAppService;
        _specimenRepository = specimenRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.Specimen;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateSpecimenDto>(data);
        var result = await _specimenAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        var updateDto = DeserializeData<UpdateSpecimenDto>(data);
        var result = await _specimenAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _specimenAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _specimenAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _specimenRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _specimenRepository.FindAsync(serverId);
        return entity != null;
    }

    private static T DeserializeData<T>(object data) => SyncDataSerializer.Deserialize<T>(data);
}
