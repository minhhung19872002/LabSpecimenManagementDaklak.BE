using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.Units;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.Units.Default)]
public class UnitAppService :
    CrudAppService<
        Unit,
        UnitDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateUnitDto>,
    IUnitAppService
{
    public UnitAppService(IRepository<Unit, Guid> repository)
        : base(repository)
    {
        GetPolicyName = LabSpecimenManagementPermissions.Units.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.Units.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.MasterData.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.MasterData.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.MasterData.Delete;
    }

    public async Task<ListResultDto<UnitDto>> GetActiveUnitsAsync(List<Guid>? includeIds = null)
    {
        var query = await Repository.GetQueryableAsync();

        var activeItems = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        if (includeIds != null && includeIds.Count > 0)
        {
            var activeIds = activeItems.Select(x => x.Id).ToHashSet();
            var missingIds = includeIds.Where(id => !activeIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
            {
                var inactiveItems = query
                    .Where(x => missingIds.Contains(x.Id))
                    .ToList();
                activeItems.AddRange(inactiveItems);
            }
        }

        var dtos = ObjectMapper.Map<List<Unit>, List<UnitDto>>(activeItems);

        return new ListResultDto<UnitDto>(dtos);
    }

    public override async Task<PagedResultDto<UnitDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.Name)
            .ApplyPaging(input)
            .ToList();

        var dtos = ObjectMapper.Map<List<Unit>, List<UnitDto>>(items);

        return new PagedResultDto<UnitDto>(totalCount, dtos);
    }

    public override async Task<UnitDto> CreateAsync(CreateUpdateUnitDto input)
    {
        await ValidateDuplicateCodeAsync(input.Code);

        return await base.CreateAsync(input);
    }

    public override async Task<UnitDto> UpdateAsync(Guid id, CreateUpdateUnitDto input)
    {
        await ValidateDuplicateCodeAsync(input.Code, id);

        return await base.UpdateAsync(id, input);
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.IsActive = !entity.IsActive;
        await Repository.UpdateAsync(entity);
    }

    public async Task<int> BatchDeleteAsync(List<Guid> ids)
    {
        var count = 0;
        foreach (var id in ids)
        {
            try
            {
                await Repository.DeleteAsync(id);
                count++;
            }
            catch
            {
                // Skip failed deletions
            }
        }
        return count;
    }

    public async Task<int> BatchToggleActiveAsync(List<Guid> ids, bool isActive)
    {
        var count = 0;
        foreach (var id in ids)
        {
            try
            {
                var entity = await Repository.GetAsync(id);
                entity.IsActive = isActive;
                await Repository.UpdateAsync(entity);
                count++;
            }
            catch
            {
                // Skip failed updates
            }
        }
        return count;
    }

    protected override Task<Unit> MapToEntityAsync(CreateUpdateUnitDto createInput)
    {
        return Task.FromResult(new Unit(GuidGenerator.Create(), createInput.Code, createInput.Name)
        {
            Category = createInput.Category,
            IsActive = createInput.IsActive
        });
    }

    protected override Task MapToEntityAsync(CreateUpdateUnitDto updateInput, Unit entity)
    {
        entity.Code = updateInput.Code;
        entity.Name = updateInput.Name;
        entity.Category = updateInput.Category;
        entity.IsActive = updateInput.IsActive;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate duplicate unit code (case-insensitive)
    /// </summary>
    private async Task ValidateDuplicateCodeAsync(string code, Guid? excludeId = null)
    {
        var query = await Repository.GetQueryableAsync();
        var normalizedCode = code.Trim().ToLower();

        var exists = query.Any(x =>
            x.Code.ToLower() == normalizedCode &&
            (excludeId == null || x.Id != excludeId));

        if (exists)
        {
            throw new UserFriendlyException("Mã đơn vị hàng hóa đã tồn tại. Vui lòng chọn mã khác.");
        }
    }
}
