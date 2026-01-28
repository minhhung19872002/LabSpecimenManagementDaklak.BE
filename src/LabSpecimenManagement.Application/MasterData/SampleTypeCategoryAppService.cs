using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.SampleTypeCategories;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class SampleTypeCategoryAppService :
    CrudAppService<
        SampleTypeCategory,
        SampleTypeCategoryDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSampleTypeCategoryDto>,
    ISampleTypeCategoryAppService
{
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;

    public SampleTypeCategoryAppService(
        IRepository<SampleTypeCategory, Guid> repository,
        IRepository<SpecimenType, Guid> specimenTypeRepository)
        : base(repository)
    {
        _specimenTypeRepository = specimenTypeRepository;

        GetPolicyName = LabSpecimenManagementPermissions.MasterData.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.MasterData.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.MasterData.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.MasterData.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.MasterData.Delete;
    }

    public async Task<ListResultDto<SampleTypeCategoryDto>> GetActiveCategoriesAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<SampleTypeCategory>, List<SampleTypeCategoryDto>>(items);
        await EnrichDtosAsync(dtos);

        return new ListResultDto<SampleTypeCategoryDto>(dtos);
    }

    public async Task<ListResultDto<SampleTypeCategoryDto>> GetCategoriesWithCountAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<SampleTypeCategory>, List<SampleTypeCategoryDto>>(items);
        await EnrichDtosAsync(dtos);

        return new ListResultDto<SampleTypeCategoryDto>(dtos);
    }

    public override async Task<PagedResultDto<SampleTypeCategoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.Name)
            .ApplyPaging(input)
            .ToList();

        var dtos = ObjectMapper.Map<List<SampleTypeCategory>, List<SampleTypeCategoryDto>>(items);
        await EnrichDtosAsync(dtos);

        return new PagedResultDto<SampleTypeCategoryDto>(totalCount, dtos);
    }

    public override async Task<SampleTypeCategoryDto> CreateAsync(CreateUpdateSampleTypeCategoryDto input)
    {
        // Check duplicate name (case-insensitive)
        await ValidateDuplicateNameAsync(input.Name);

        return await base.CreateAsync(input);
    }

    public override async Task<SampleTypeCategoryDto> UpdateAsync(Guid id, CreateUpdateSampleTypeCategoryDto input)
    {
        // Check duplicate name (case-insensitive), excluding current entity
        await ValidateDuplicateNameAsync(input.Name, id);

        return await base.UpdateAsync(id, input);
    }

    public override async Task DeleteAsync(Guid id)
    {
        // Check if there are any SpecimenTypes in this category
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();
        var hasSpecimenTypes = specimenTypeQuery.Any(x => x.SampleTypeCategoryId == id);

        if (hasSpecimenTypes)
        {
            throw new UserFriendlyException("Không thể xóa danh mục vì còn loại mẫu bên trong. Vui lòng di chuyển hoặc xóa các loại mẫu trước.");
        }

        await base.DeleteAsync(id);
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var newIsActive = !entity.IsActive;

        // Update the category
        entity.IsActive = newIsActive;
        await Repository.UpdateAsync(entity);

        // Cascade update all SpecimenTypes in this category
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();
        var specimenTypes = specimenTypeQuery
            .Where(x => x.SampleTypeCategoryId == id)
            .ToList();

        foreach (var specimenType in specimenTypes)
        {
            specimenType.IsActive = newIsActive;
            await _specimenTypeRepository.UpdateAsync(specimenType);
        }
    }

    protected override Task<SampleTypeCategory> MapToEntityAsync(CreateUpdateSampleTypeCategoryDto createInput)
    {
        return Task.FromResult(new SampleTypeCategory(GuidGenerator.Create(), createInput.Name, createInput.Description)
        {
            IsActive = createInput.IsActive
        });
    }

    protected override Task MapToEntityAsync(CreateUpdateSampleTypeCategoryDto updateInput, SampleTypeCategory entity)
    {
        entity.Name = updateInput.Name;
        entity.Description = updateInput.Description;
        entity.IsActive = updateInput.IsActive;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Enriches DTOs with specimen type counts (batch operation to avoid N+1)
    /// </summary>
    private async Task EnrichDtosAsync(List<SampleTypeCategoryDto> dtos)
    {
        if (dtos.Count == 0) return;

        var categoryIds = dtos.Select(d => d.Id).ToList();

        // Load specimen type counts
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();
        var counts = specimenTypeQuery
            .Where(x => x.SampleTypeCategoryId != null && categoryIds.Contains(x.SampleTypeCategoryId.Value))
            .GroupBy(x => x.SampleTypeCategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.CategoryId, x => x.Count);

        foreach (var dto in dtos)
        {
            dto.SpecimenTypeCount = counts.GetValueOrDefault(dto.Id, 0);
        }
    }

    /// <summary>
    /// Validate duplicate category name (case-insensitive)
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
            throw new UserFriendlyException("Tên danh mục loại mẫu đã tồn tại. Vui lòng chọn tên khác.");
        }
    }
}
