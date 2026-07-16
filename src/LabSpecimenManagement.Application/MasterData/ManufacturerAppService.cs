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
[Authorize(LabSpecimenManagementPermissions.Manufacturers.Default)]
public class ManufacturerAppService :
    CrudAppService<
        Manufacturer,
        ManufacturerDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateManufacturerDto>,
    IManufacturerAppService
{
    private const string CodePrefix = "HSX";
    private readonly IDataFilter _dataFilter;

    public ManufacturerAppService(
        IRepository<Manufacturer, Guid> repository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _dataFilter = dataFilter;
        GetPolicyName = LabSpecimenManagementPermissions.Manufacturers.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.Manufacturers.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.MasterData.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.MasterData.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.MasterData.Delete;
    }

    public override async Task<PagedResultDto<ManufacturerDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime).ApplyPaging(input));

        var dtos = ObjectMapper.Map<List<Manufacturer>, List<ManufacturerDto>>(items);

        return new PagedResultDto<ManufacturerDto>(totalCount, dtos);
    }

    public override async Task<ManufacturerDto> CreateAsync(CreateUpdateManufacturerDto input)
    {
        await ValidateDuplicateNameAsync(input.Name);

        return await base.CreateAsync(input);
    }

    public override async Task<ManufacturerDto> UpdateAsync(Guid id, CreateUpdateManufacturerDto input)
    {
        var entity = await Repository.GetAsync(id);
        if (entity.IsSystem)
            throw new UserFriendlyException("Không thể chỉnh sửa hãng sản xuất hệ thống.");

        await ValidateDuplicateNameAsync(input.Name, id);

        return await base.UpdateAsync(id, input);
    }

    public async Task<ListResultDto<ManufacturerDto>> GetActiveAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            query.WhereActive().OrderBy(x => x.Name));

        var dtos = ObjectMapper.Map<List<Manufacturer>, List<ManufacturerDto>>(items);

        return new ListResultDto<ManufacturerDto>(dtos);
    }

    public async Task<string> GetNextCodeAsync()
    {
        return await GenerateCodeAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.IsActive = !entity.IsActive;
        await Repository.UpdateAsync(entity);
    }

    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        if (entity.IsSystem)
            throw new UserFriendlyException("Không thể xóa hãng sản xuất hệ thống.");

        await base.DeleteAsync(id);
    }

    public async Task<int> BatchDeleteAsync(List<Guid> ids)
    {
        var count = 0;
        foreach (var id in ids)
        {
            try
            {
                var entity = await Repository.GetAsync(id);
                if (entity.IsSystem) continue;
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

    protected override async Task<Manufacturer> MapToEntityAsync(CreateUpdateManufacturerDto createInput)
    {
        var code = await GenerateCodeAsync();
        return new Manufacturer(GuidGenerator.Create(), code, createInput.Name)
        {
            Description = createInput.Description,
            IsActive = createInput.IsActive
        };
    }

    protected override Task MapToEntityAsync(CreateUpdateManufacturerDto updateInput, Manufacturer entity)
    {
        entity.Name = updateInput.Name;
        entity.Description = updateInput.Description;
        entity.IsActive = updateInput.IsActive;
        return Task.CompletedTask;
    }

    public async Task<string> GenerateCodeAsync()
    {
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await Repository.GetQueryableAsync();
            var codes = query
                .Where(x => x.Code.StartsWith(CodePrefix))
                .Select(x => x.Code)
                .ToList();

            int maxNumber = 0;
            foreach (var code in codes)
            {
                var numberPart = code.Replace($"{CodePrefix}-", "").Replace(CodePrefix, "");
                if (int.TryParse(numberPart, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            var nextNumber = maxNumber + 1;
            return $"{CodePrefix}-{nextNumber:D3}";
        }
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
            throw new UserFriendlyException("Tên hãng sản xuất đã tồn tại. Vui lòng chọn tên khác.");
        }
    }
}
