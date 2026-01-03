using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class SpecimenTypeAppService :
    CrudAppService<
        SpecimenType,
        SpecimenTypeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSpecimenTypeDto>,
    ISpecimenTypeAppService
{
    public SpecimenTypeAppService(IRepository<SpecimenType, Guid> repository)
        : base(repository)
    {
    }

    public override async Task<SpecimenTypeDto> CreateAsync(CreateUpdateSpecimenTypeDto input)
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
            throw new SpecimenTypeValidationException(errors);
        }

        return await base.CreateAsync(input);
    }

    private async Task<string> GenerateCodeAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var maxCode = query
            .Where(x => x.Code.StartsWith("LM"))
            .Select(x => x.Code)
            .OrderByDescending(x => x)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(maxCode))
        {
            return "LM001";
        }

        // Extract number from code like "LM001" -> 1
        var numberPart = maxCode.Substring(2);
        if (int.TryParse(numberPart, out var num))
        {
            return $"LM{(num + 1):D3}";
        }

        return $"LM{DateTime.Now:yyyyMMddHHmmss}";
    }

    public override async Task<SpecimenTypeDto> UpdateAsync(Guid id, CreateUpdateSpecimenTypeDto input)
    {
        // Validate duplicate code and name (exclude current entity)
        var errors = await ValidateDuplicateAsync(input.Code, input.Name, id);
        if (errors.Count > 0)
        {
            throw new SpecimenTypeValidationException(errors);
        }

        return await base.UpdateAsync(id, input);
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
                errors["code"] = "Mã loại mẫu đã tồn tại";
            }
        }

        // Check duplicate name
        var duplicateName = query.FirstOrDefault(x =>
            x.Name.ToLower() == name.ToLower() &&
            (excludeId == null || x.Id != excludeId));

        if (duplicateName != null)
        {
            errors["name"] = "Tên loại mẫu đã tồn tại";
        }

        return errors;
    }

    public async Task<ListResultDto<SpecimenTypeDto>> GetActiveSpecimenTypesAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<SpecimenTypeDto>(
            ObjectMapper.Map<List<SpecimenType>, List<SpecimenTypeDto>>(items)
        );
    }
}
