using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class ManufacturerAppService :
    CrudAppService<
        Manufacturer,
        ManufacturerDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateManufacturerDto>,
    IManufacturerAppService
{
    private readonly IDataFilter _dataFilter;

    public ManufacturerAppService(
        IRepository<Manufacturer, Guid> repository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _dataFilter = dataFilter;
    }

    public override async Task<ManufacturerDto> CreateAsync(CreateUpdateManufacturerDto input)
    {
        // Auto-generate code if not provided
        if (string.IsNullOrWhiteSpace(input.Code))
        {
            input.Code = await GenerateCodeAsync();
        }

        // Validate duplicate code and name
        var errors = await ValidateDuplicateAsync(input.Code, input.Name, null);
        if (errors.Count > 0)
        {
            throw new UserFriendlyException(string.Join("; ", errors.Values));
        }

        return await base.CreateAsync(input);
    }

    public override async Task<ManufacturerDto> UpdateAsync(Guid id, CreateUpdateManufacturerDto input)
    {
        // Get existing entity to preserve code if not provided
        var existingEntity = await Repository.GetAsync(id);

        // If code is not provided, keep the existing code
        if (string.IsNullOrWhiteSpace(input.Code))
        {
            input.Code = existingEntity.Code;
        }

        // Validate duplicate code and name (exclude current entity)
        var errors = await ValidateDuplicateAsync(input.Code, input.Name, id);
        if (errors.Count > 0)
        {
            throw new UserFriendlyException(string.Join("; ", errors.Values));
        }

        return await base.UpdateAsync(id, input);
    }

    public async Task<ListResultDto<ManufacturerDto>> GetActiveAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<Manufacturer>, List<ManufacturerDto>>(items);

        return new ListResultDto<ManufacturerDto>(dtos);
    }

    protected override Task<Manufacturer> MapToEntityAsync(CreateUpdateManufacturerDto createInput)
    {
        return Task.FromResult(new Manufacturer(GuidGenerator.Create(), createInput.Code!, createInput.Name)
        {
            IsActive = createInput.IsActive
        });
    }

    protected override Task MapToEntityAsync(CreateUpdateManufacturerDto updateInput, Manufacturer entity)
    {
        entity.Code = updateInput.Code!;
        entity.Name = updateInput.Name;
        entity.IsActive = updateInput.IsActive;
        return Task.CompletedTask;
    }

    private async Task<string> GenerateCodeAsync()
    {
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await Repository.GetQueryableAsync();
            var codes = query
                .Where(x => x.Code.StartsWith("HSX"))
                .Select(x => x.Code)
                .ToList();

            var maxNumber = 0;
            foreach (var code in codes)
            {
                var numberPart = code.Substring(3);
                if (int.TryParse(numberPart, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            return $"HSX{(maxNumber + 1):D3}";
        }
    }

    private async Task<Dictionary<string, string>> ValidateDuplicateAsync(string? code, string name, Guid? excludeId)
    {
        var errors = new Dictionary<string, string>();
        var query = await Repository.GetQueryableAsync();

        // Check duplicate code (only if code is provided)
        if (!string.IsNullOrWhiteSpace(code))
        {
            var duplicateCode = query.FirstOrDefault(x =>
                x.Code.ToLower() == code.ToLower() &&
                (excludeId == null || x.Id != excludeId));

            if (duplicateCode != null)
            {
                errors["code"] = "Mã hãng sản xuất đã tồn tại";
            }
        }

        // Check duplicate name
        var duplicateName = query.FirstOrDefault(x =>
            x.Name.ToLower() == name.ToLower() &&
            (excludeId == null || x.Id != excludeId));

        if (duplicateName != null)
        {
            errors["name"] = "Tên hãng sản xuất đã tồn tại";
        }

        return errors;
    }
}
