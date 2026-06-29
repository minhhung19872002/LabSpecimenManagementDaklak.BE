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
        await FillParentWarehouseNamesAsync(dtos);

        return new PagedResultDto<WarehouseDto>(totalCount, dtos);
    }

    public override async Task<WarehouseDto> CreateAsync(CreateUpdateWarehouseDto input)
    {
        await ValidateDuplicateCodeAsync(input.Code);
        await ValidateParentWarehouseAsync(null, input.ParentWarehouseId);

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
        await ValidateParentWarehouseAsync(id, input.ParentWarehouseId);

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
        await FillParentWarehouseNamesAsync(dtos);

        return new ListResultDto<WarehouseDto>(dtos);
    }

    public async Task<ListResultDto<WarehouseDto>> GetRootWarehousesAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .Where(x => x.Level == 0)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        var dtos = ObjectMapper.Map<List<Warehouse>, List<WarehouseDto>>(items);
        await FillParentWarehouseNamesAsync(dtos);

        return new ListResultDto<WarehouseDto>(dtos);
    }

    public async Task<string> GetNextCodeAsync()
    {
        return await GenerateCodeAsync();
    }

    public async Task<WarehouseDto?> GetDefaultAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var warehouse = query.FirstOrDefault(x => x.IsDefault);
        return warehouse == null ? null : ObjectMapper.Map<Warehouse, WarehouseDto>(warehouse);
    }

    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        if (entity.IsDefault)
        {
            throw new UserFriendlyException("Không thể xóa kho mặc định.");
        }
        await EnsureNoChildrenAsync(id);
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
                await EnsureNoChildrenAsync(id);
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

    protected override async Task<Warehouse> MapToEntityAsync(CreateUpdateWarehouseDto createInput)
    {
        return new Warehouse(GuidGenerator.Create(), createInput.Code, createInput.Name)
        {
            PhysicalLocation = createInput.PhysicalLocation,
            IsActive = createInput.IsActive,
            ParentWarehouseId = createInput.ParentWarehouseId,
            Level = await ComputeLevelAsync(createInput.ParentWarehouseId)
        };
    }

    protected override async Task MapToEntityAsync(CreateUpdateWarehouseDto updateInput, Warehouse entity)
    {
        entity.Code = updateInput.Code;
        entity.Name = updateInput.Name;
        entity.PhysicalLocation = updateInput.PhysicalLocation;
        entity.IsActive = updateInput.IsActive;
        entity.ParentWarehouseId = updateInput.ParentWarehouseId;
        entity.Level = await ComputeLevelAsync(updateInput.ParentWarehouseId);
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

    private async Task<int> ComputeLevelAsync(Guid? parentWarehouseId)
    {
        if (parentWarehouseId == null)
        {
            return 0;
        }

        var parent = await Repository.GetAsync(parentWarehouseId.Value);
        return parent.Level + 1;
    }

    private async Task ValidateParentWarehouseAsync(Guid? id, Guid? parentWarehouseId)
    {
        if (parentWarehouseId == null)
        {
            return;
        }

        if (id != null && parentWarehouseId == id)
        {
            throw new UserFriendlyException("Kho cha không thể là chính kho này.");
        }

        var query = await Repository.GetQueryableAsync();
        var allWarehouses = query.ToList();

        if (!allWarehouses.Any(x => x.Id == parentWarehouseId.Value))
        {
            throw new UserFriendlyException("Kho cha không tồn tại.");
        }

        if (id == null)
        {
            return;
        }

        // Walk up the ancestor chain from the chosen parent to detect a cycle back to `id`.
        var currentId = (Guid?)parentWarehouseId.Value;
        while (currentId != null)
        {
            if (currentId == id)
            {
                throw new UserFriendlyException("Không thể chọn kho con của chính kho này làm kho cha.");
            }

            currentId = allWarehouses.FirstOrDefault(x => x.Id == currentId)?.ParentWarehouseId;
        }
    }

    private async Task EnsureNoChildrenAsync(Guid id)
    {
        var query = await Repository.GetQueryableAsync();
        var hasChildren = query.Any(x => x.ParentWarehouseId == id);
        if (hasChildren)
        {
            throw new UserFriendlyException("Không thể xóa kho cha. Vui lòng xóa hoặc chuyển kho con sang kho cha khác trước.");
        }
    }

    private async Task FillParentWarehouseNamesAsync(List<WarehouseDto> dtos)
    {
        var parentIds = dtos.Where(x => x.ParentWarehouseId != null)
            .Select(x => x.ParentWarehouseId!.Value)
            .Distinct()
            .ToList();

        if (parentIds.Count == 0)
        {
            return;
        }

        var query = await Repository.GetQueryableAsync();
        var parents = query.Where(x => parentIds.Contains(x.Id))
            .ToList()
            .ToDictionary(x => x.Id, x => x.Name);

        foreach (var dto in dtos)
        {
            if (dto.ParentWarehouseId != null && parents.TryGetValue(dto.ParentWarehouseId.Value, out var name))
            {
                dto.ParentWarehouseName = name;
            }
        }
    }
}
