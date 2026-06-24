using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.Warehouses.Default)]
public class WarehouseAppService :
    CrudAppService<
        Warehouse,
        WarehouseDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateWarehouseDto>,
    IWarehouseAppService
{
    private const string CodePrefix = "KH";
    private readonly IDataFilter _dataFilter;

    public WarehouseAppService(
        IRepository<Warehouse, Guid> repository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _dataFilter = dataFilter;
        GetPolicyName = LabSpecimenManagementPermissions.Warehouses.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.Warehouses.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.MasterData.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.MasterData.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.MasterData.Delete;
    }

    public override async Task<PagedResultDto<WarehouseDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime).ApplyPaging(input));

        var dtos = ObjectMapper.Map<List<Warehouse>, List<WarehouseDto>>(items);

        return new PagedResultDto<WarehouseDto>(totalCount, dtos);
    }

    public override async Task<WarehouseDto> CreateAsync(CreateUpdateWarehouseDto input)
    {
        await ValidateDuplicateCodeAsync(input.Code);

        return await base.CreateAsync(input);
    }

    public override async Task<WarehouseDto> UpdateAsync(Guid id, CreateUpdateWarehouseDto input)
    {
        var entity = await Repository.GetAsync(id);
        if (entity.IsDefault)
        {
            entity.Name = input.Name;
            entity.PhysicalLocation = input.PhysicalLocation;
            await Repository.UpdateAsync(entity);
            return ObjectMapper.Map<Warehouse, WarehouseDto>(entity);
        }

        await ValidateDuplicateCodeAsync(input.Code, id);

        return await base.UpdateAsync(id, input);
    }

    public async Task<ListResultDto<WarehouseDto>> GetActiveWarehousesAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        var dtos = ObjectMapper.Map<List<Warehouse>, List<WarehouseDto>>(items);

        return new ListResultDto<WarehouseDto>(dtos);
    }

    public async Task<string> GetNextCodeAsync()
    {
        return await GenerateCodeAsync();
    }

    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        if (entity.IsDefault)
        {
            throw new UserFriendlyException("Không thể xóa kho mặc định.");
        }
        await base.DeleteAsync(id);
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        if (entity.IsDefault)
        {
            throw new UserFriendlyException("Không thể thay đổi trạng thái kho mặc định.");
        }
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
                var entity = await Repository.GetAsync(id);
                if (entity.IsDefault) continue;
                await Repository.DeleteAsync(id);
                count++;
            }
            catch
            {
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
                if (entity.IsDefault) continue;
                entity.IsActive = isActive;
                await Repository.UpdateAsync(entity);
                count++;
            }
            catch
            {
            }
        }
        return count;
    }

    protected override Task<Warehouse> MapToEntityAsync(CreateUpdateWarehouseDto createInput)
    {
        return Task.FromResult(new Warehouse(GuidGenerator.Create(), createInput.Code, createInput.Name)
        {
            PhysicalLocation = createInput.PhysicalLocation,
            IsActive = createInput.IsActive
        });
    }

    protected override Task MapToEntityAsync(CreateUpdateWarehouseDto updateInput, Warehouse entity)
    {
        entity.Code = updateInput.Code;
        entity.Name = updateInput.Name;
        entity.PhysicalLocation = updateInput.PhysicalLocation;
        entity.IsActive = updateInput.IsActive;
        return Task.CompletedTask;
    }

    public async Task<string> GenerateCodeAsync()
    {
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await Repository.GetQueryableAsync();
            var codes = query
                .Where(x => x.Code.StartsWith($"{CodePrefix}-"))
                .Select(x => x.Code)
                .ToList();

            int maxNumber = 0;
            foreach (var code in codes)
            {
                var numberPart = code.Replace($"{CodePrefix}-", "");
                if (int.TryParse(numberPart, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            var nextNumber = maxNumber + 1;
            return $"{CodePrefix}-{nextNumber:D3}";
        }
    }

    private async Task ValidateDuplicateCodeAsync(string code, Guid? excludeId = null)
    {
        var query = await Repository.GetQueryableAsync();
        var normalizedCode = code.Trim().ToLower();

        var exists = query.Any(x =>
            x.Code.ToLower() == normalizedCode &&
            (excludeId == null || x.Id != excludeId));

        if (exists)
        {
            throw new UserFriendlyException("Mã kho đã tồn tại. Vui lòng chọn mã khác.");
        }
    }
}
