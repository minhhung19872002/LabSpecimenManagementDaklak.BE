using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LabSpecimenManagement.NCRs;

[RemoteService(IsEnabled = false)]
public class NCRAppService :
    CrudAppService<
        NCR,
        NCRDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateNCRDto,
        UpdateNCRDto>,
    INCRAppService
{
    private readonly ICurrentUser _currentUser;

    public NCRAppService(
        IRepository<NCR, Guid> repository,
        ICurrentUser currentUser)
        : base(repository)
    {
        _currentUser = currentUser;
    }

    protected override async Task<NCR> MapToEntityAsync(CreateNCRDto createInput)
    {
        var ncrNumber = await GenerateNCRNumberAsync();

        var entity = new NCR(
            GuidGenerator.Create(),
            ncrNumber,
            createInput.Title,
            createInput.Description,
            createInput.Type,
            createInput.Severity,
            _currentUser.Id ?? Guid.Empty,
            createInput.DueDate
        );

        entity.RelatedEntityType = createInput.RelatedEntityType;
        entity.RelatedEntityId = createInput.RelatedEntityId;
        entity.RelatedEntityCode = createInput.RelatedEntityCode;
        entity.Department = createInput.Department;
        entity.AssignedToId = createInput.AssignedToId;
        entity.Notes = createInput.Notes;

        return entity;
    }

    public async Task<NCRDto> CloseAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = NCRStatus.Closed;
        entity.ClosedById = _currentUser.Id;
        entity.ClosedAt = DateTime.Now;

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<ListResultDto<NCRDto>> GetOpenNCRsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.Status == NCRStatus.Open || x.Status == NCRStatus.InProgress)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        return new ListResultDto<NCRDto>(
            ObjectMapper.Map<System.Collections.Generic.List<NCR>, System.Collections.Generic.List<NCRDto>>(items)
        );
    }

    public async Task<NCRDto> AssignAsync(Guid id, Guid assignedToId)
    {
        var entity = await Repository.GetAsync(id);
        entity.AssignedToId = assignedToId;
        entity.Status = NCRStatus.InProgress;

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    private async Task<string> GenerateNCRNumberAsync()
    {
        var today = DateTime.Now;
        var prefix = $"NCR-{today:yyyyMMdd}";
        var count = await Repository.CountAsync(x => x.NCRNumber.StartsWith(prefix));
        var sequence = (count + 1).ToString("D4");
        return $"{prefix}-{sequence}";
    }
}
