using System;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Subcontractors;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for Subcontractor entity
/// </summary>
public class SubcontractorSyncHandler : ISyncEntityHandler, ITransientDependency
{
    private readonly ISubcontractorAppService _subcontractorAppService;
    private readonly IRepository<Subcontractor, Guid> _subcontractorRepository;

    public SubcontractorSyncHandler(
        ISubcontractorAppService subcontractorAppService,
        IRepository<Subcontractor, Guid> subcontractorRepository)
    {
        _subcontractorAppService = subcontractorAppService;
        _subcontractorRepository = subcontractorRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.Subcontractor;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateUpdateSubcontractorDto>(data);
        var result = await _subcontractorAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        var updateDto = DeserializeData<CreateUpdateSubcontractorDto>(data);
        var result = await _subcontractorAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _subcontractorAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _subcontractorAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _subcontractorRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _subcontractorRepository.FindAsync(serverId);
        return entity != null;
    }

    private static T DeserializeData<T>(object data) => SyncDataSerializer.Deserialize<T>(data);
}
