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
[Authorize(LabSpecimenManagementPermissions.SampleConditions.Default)]
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
        GetPolicyName = LabSpecimenManagementPermissions.SampleConditions.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.SampleConditions.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.SampleConditions.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.SampleConditions.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.SampleConditions.Delete;
    }

    public async Task<ListResultDto<SampleConditionDto>> GetActiveAsync()
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
        await ValidateDuplicateNameAsync(input.Name);
        return await base.CreateAsync(input);
    }

    public override async Task<SampleConditionDto> UpdateAsync(Guid id, CreateUpdateSampleConditionDto input)
    {
        await ValidateDuplicateNameAsync(input.Name, id);
        return await base.UpdateAsync(id, input);
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.IsActive = !entity.IsActive;
        await Repository.UpdateAsync(entity);
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
