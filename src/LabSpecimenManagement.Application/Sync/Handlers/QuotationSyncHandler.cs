using System;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Quotations;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for Quotation entity
/// </summary>
public class QuotationSyncHandler : ISyncEntityHandler, ITransientDependency
{
    private readonly IQuotationAppService _quotationAppService;
    private readonly IRepository<Quotation, Guid> _quotationRepository;

    public QuotationSyncHandler(
        IQuotationAppService quotationAppService,
        IRepository<Quotation, Guid> quotationRepository)
    {
        _quotationAppService = quotationAppService;
        _quotationRepository = quotationRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.Quotation;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateUpdateQuotationDto>(data);
        var result = await _quotationAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        var updateDto = DeserializeData<CreateUpdateQuotationDto>(data);
        var result = await _quotationAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _quotationAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _quotationAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _quotationRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _quotationRepository.FindAsync(serverId);
        return entity != null;
    }

    private static T DeserializeData<T>(object data) => SyncDataSerializer.Deserialize<T>(data);
}
