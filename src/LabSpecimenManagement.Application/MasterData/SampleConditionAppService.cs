using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.SampleConditions;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class SampleConditionAppService :
    CrudAppService<
        SampleCondition,
        SampleConditionDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSampleConditionDto>,
    ISampleConditionAppService
{
    public SampleConditionAppService(IRepository<SampleCondition, Guid> repository)
        : base(repository)
    {
        GetPolicyName = LabSpecimenManagementPermissions.MasterData.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.MasterData.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.MasterData.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.MasterData.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.MasterData.Delete;
    }

    public async Task<ListResultDto<SampleConditionDto>> GetActiveConditionsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<SampleCondition>, List<SampleConditionDto>>(items);

        return new ListResultDto<SampleConditionDto>(dtos);
    }

    public override async Task<PagedResultDto<SampleConditionDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.Name)
            .ApplyPaging(input)
            .ToList();

        var dtos = ObjectMapper.Map<List<SampleCondition>, List<SampleConditionDto>>(items);

        return new PagedResultDto<SampleConditionDto>(totalCount, dtos);
    }

    public override async Task<SampleConditionDto> CreateAsync(CreateUpdateSampleConditionDto input)
    {
        // Check duplicate name (case-insensitive)
        await ValidateDuplicateNameAsync(input.Name);

        return await base.CreateAsync(input);
    }

    public override async Task<SampleConditionDto> UpdateAsync(Guid id, CreateUpdateSampleConditionDto input)
    {
        // Check duplicate name (case-insensitive), excluding current entity
        await ValidateDuplicateNameAsync(input.Name, id);

        return await base.UpdateAsync(id, input);
    }

    protected override Task<SampleCondition> MapToEntityAsync(CreateUpdateSampleConditionDto createInput)
    {
        return Task.FromResult(new SampleCondition(GuidGenerator.Create(), createInput.Name, createInput.Description)
        {
            IsActive = createInput.IsActive
        });
    }

    protected override Task MapToEntityAsync(CreateUpdateSampleConditionDto updateInput, SampleCondition entity)
    {
        entity.Name = updateInput.Name;
        entity.Description = updateInput.Description;
        entity.IsActive = updateInput.IsActive;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate duplicate condition name (case-insensitive)
    /// </summary>
    private async Task ValidateDuplicateNameAsync(string name, Guid? excludeId = null)
    {
        var query = await Repository.GetQueryableAsync();
        var normalizedName = name.Trim().ToLower();

        var exists = query.Any(x =>
            x.Name.ToLower() == normalizedName &&
            (excludeId == null || x.Id != excludeId));

        if (exists)
        {
            throw new UserFriendlyException("Tên tình trạng mẫu đã tồn tại. Vui lòng chọn tên khác.");
        }
    }
}
