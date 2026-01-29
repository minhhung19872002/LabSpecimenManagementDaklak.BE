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
public class SpecimenTypeAppService :
    CrudAppService<
        SpecimenType,
        SpecimenTypeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSpecimenTypeDto>,
    ISpecimenTypeAppService
{
    private readonly IRepository<SpecimenTypeIndicator, Guid> _specimenTypeIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<SampleTypeCategory, Guid> _sampleTypeCategoryRepository;
    private readonly IDataFilter _dataFilter;

    public SpecimenTypeAppService(
        IRepository<SpecimenType, Guid> repository,
        IRepository<SpecimenTypeIndicator, Guid> specimenTypeIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<SampleTypeCategory, Guid> sampleTypeCategoryRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _specimenTypeIndicatorRepository = specimenTypeIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _sampleTypeCategoryRepository = sampleTypeCategoryRepository;
        _dataFilter = dataFilter;
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

        var result = await base.CreateAsync(input);
        await EnrichDtoAsync(result);
        return result;
    }

    public override async Task<SpecimenTypeDto> GetAsync(Guid id)
    {
        var result = await base.GetAsync(id);
        await EnrichDtoAsync(result);
        return result;
    }

    public override async Task<PagedResultDto<SpecimenTypeDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var result = await base.GetListAsync(input);
        await EnrichDtosAsync(result.Items.ToList());
        return result;
    }

    private async Task<string> GenerateCodeAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
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
    }

    public override async Task<SpecimenTypeDto> UpdateAsync(Guid id, CreateUpdateSpecimenTypeDto input)
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

        var dtos = ObjectMapper.Map<List<SpecimenType>, List<SpecimenTypeDto>>(items);
        await EnrichDtosAsync(dtos);

        return new ListResultDto<SpecimenTypeDto>(dtos);
    }

    /// <summary>
    /// Enrich single DTO with category name
    /// </summary>
    private async Task EnrichDtoAsync(SpecimenTypeDto dto)
    {
        if (dto.SampleTypeCategoryId.HasValue)
        {
            var category = await _sampleTypeCategoryRepository.FindAsync(dto.SampleTypeCategoryId.Value);
            dto.SampleTypeCategoryName = category?.Name;
        }
    }

    /// <summary>
    /// Enrich multiple DTOs with category names (batch operation to avoid N+1)
    /// </summary>
    private async Task EnrichDtosAsync(List<SpecimenTypeDto> dtos)
    {
        if (dtos.Count == 0) return;

        var categoryIds = dtos
            .Where(d => d.SampleTypeCategoryId.HasValue)
            .Select(d => d.SampleTypeCategoryId!.Value)
            .Distinct()
            .ToList();

        if (categoryIds.Count == 0) return;

        var categoryQuery = await _sampleTypeCategoryRepository.GetQueryableAsync();
        var categoryNames = categoryQuery
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionary(c => c.Id, c => c.Name);

        foreach (var dto in dtos)
        {
            if (dto.SampleTypeCategoryId.HasValue && categoryNames.TryGetValue(dto.SampleTypeCategoryId.Value, out var name))
            {
                dto.SampleTypeCategoryName = name;
            }
        }
    }

    #region Indicator Management

    public async Task<List<SpecimenTypeWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        var specimenTypes = await Repository.GetListAsync();
        var specimenTypeIndicators = await _specimenTypeIndicatorRepository.GetListAsync();
        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorGroups = await _indicatorGroupRepository.GetListAsync();

        // Load all categories for enrichment
        var categoryIds = specimenTypes
            .Where(st => st.SampleTypeCategoryId.HasValue)
            .Select(st => st.SampleTypeCategoryId!.Value)
            .Distinct()
            .ToList();
        var categoryQuery = await _sampleTypeCategoryRepository.GetQueryableAsync();
        var categoryNames = categoryQuery
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionary(c => c.Id, c => c.Name);

        var result = specimenTypes
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(st =>
            {
                var dto = ObjectMapper.Map<SpecimenType, SpecimenTypeWithIndicatorsDto>(st);

                // Enrich category name
                if (st.SampleTypeCategoryId.HasValue && categoryNames.TryGetValue(st.SampleTypeCategoryId.Value, out var categoryName))
                {
                    dto.SampleTypeCategoryName = categoryName;
                }

                var stIndicators = specimenTypeIndicators
                    .Where(sti => sti.SpecimenTypeId == st.Id)
                    .OrderBy(sti => sti.SortOrder)
                    .Select(sti =>
                    {
                        var indicator = indicators.FirstOrDefault(i => i.Id == sti.IndicatorId);
                        var indicatorGroup = indicator != null
                            ? indicatorGroups.FirstOrDefault(g => g.Id == indicator.IndicatorGroupId)
                            : null;

                        return new SpecimenTypeIndicatorDto
                        {
                            Id = sti.Id,
                            SpecimenTypeId = sti.SpecimenTypeId,
                            IndicatorId = sti.IndicatorId,
                            IndicatorCode = indicator?.Code ?? "",
                            IndicatorName = indicator?.Name ?? "",
                            IndicatorFormattedName = indicator?.FormattedName,
                            IndicatorGroupName = indicatorGroup?.Name,
                            IsActive = sti.IsActive,
                            SortOrder = sti.SortOrder
                        };
                    })
                    .ToList();

                dto.Indicators = stIndicators;
                dto.IndicatorCount = stIndicators.Count;

                return dto;
            })
            .ToList();

        return result;
    }

    public async Task<SpecimenTypeWithIndicatorsDto> GetWithIndicatorsAsync(Guid id)
    {
        var specimenType = await Repository.GetAsync(id);
        var specimenTypeIndicatorQuery = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var indicatorGroupQuery = await _indicatorGroupRepository.GetQueryableAsync();

        var dto = ObjectMapper.Map<SpecimenType, SpecimenTypeWithIndicatorsDto>(specimenType);

        // Get indicators linked to this specimen type
        var stIndicators = specimenTypeIndicatorQuery
            .Where(sti => sti.SpecimenTypeId == id)
            .OrderBy(sti => sti.SortOrder)
            .ToList();

        // Get indicator details
        var indicatorIds = stIndicators.Select(sti => sti.IndicatorId).ToList();
        var indicators = indicatorQuery.Where(i => indicatorIds.Contains(i.Id)).ToList();

        // Get indicator group details
        var groupIds = indicators.Select(i => i.IndicatorGroupId).Distinct().ToList();
        var indicatorGroups = indicatorGroupQuery.Where(g => groupIds.Contains(g.Id)).ToList();

        // Build indicators list
        dto.Indicators = stIndicators.Select(sti =>
        {
            var indicator = indicators.FirstOrDefault(i => i.Id == sti.IndicatorId);
            var indicatorGroup = indicator != null
                ? indicatorGroups.FirstOrDefault(g => g.Id == indicator.IndicatorGroupId)
                : null;

            return new SpecimenTypeIndicatorDto
            {
                Id = sti.Id,
                SpecimenTypeId = sti.SpecimenTypeId,
                IndicatorId = sti.IndicatorId,
                IndicatorCode = indicator?.Code ?? "",
                IndicatorName = indicator?.Name ?? "",
                IndicatorFormattedName = indicator?.FormattedName,
                IndicatorGroupName = indicatorGroup?.Name,
                IsActive = sti.IsActive,
                SortOrder = sti.SortOrder
            };
        }).ToList();

        dto.IndicatorCount = dto.Indicators.Count;

        return dto;
    }

    public async Task<List<SpecimenTypeIndicatorDto>> GetIndicatorsAsync(Guid specimenTypeId)
    {
        var specimenTypeIndicatorQuery = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var indicatorGroupQuery = await _indicatorGroupRepository.GetQueryableAsync();

        var stIndicators = specimenTypeIndicatorQuery
            .Where(sti => sti.SpecimenTypeId == specimenTypeId)
            .OrderBy(sti => sti.SortOrder)
            .ToList();

        var indicatorIds = stIndicators.Select(sti => sti.IndicatorId).ToList();
        var indicators = indicatorQuery.Where(i => indicatorIds.Contains(i.Id)).ToList();

        var groupIds = indicators.Select(i => i.IndicatorGroupId).Distinct().ToList();
        var indicatorGroups = indicatorGroupQuery.Where(g => groupIds.Contains(g.Id)).ToList();

        return stIndicators.Select(sti =>
        {
            var indicator = indicators.FirstOrDefault(i => i.Id == sti.IndicatorId);
            var indicatorGroup = indicator != null
                ? indicatorGroups.FirstOrDefault(g => g.Id == indicator.IndicatorGroupId)
                : null;

            return new SpecimenTypeIndicatorDto
            {
                Id = sti.Id,
                SpecimenTypeId = sti.SpecimenTypeId,
                IndicatorId = sti.IndicatorId,
                IndicatorCode = indicator?.Code ?? "",
                IndicatorName = indicator?.Name ?? "",
                IndicatorFormattedName = indicator?.FormattedName,
                IndicatorGroupName = indicatorGroup?.Name,
                IsActive = sti.IsActive,
                SortOrder = sti.SortOrder
            };
        }).ToList();
    }

    public async Task AddIndicatorsAsync(Guid specimenTypeId, AddIndicatorsToSpecimenTypeDto input)
    {
        // Verify specimen type exists
        var specimenType = await Repository.GetAsync(specimenTypeId);

        // Get existing indicators to avoid duplicates
        var existingQuery = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var existingIndicatorIds = existingQuery
            .Where(sti => sti.SpecimenTypeId == specimenTypeId)
            .Select(sti => sti.IndicatorId)
            .ToHashSet();

        // Get max sort order
        var existingSortOrders = existingQuery
            .Where(sti => sti.SpecimenTypeId == specimenTypeId)
            .Select(sti => sti.SortOrder)
            .ToList();
        var maxSortOrder = existingSortOrders.Count > 0 ? existingSortOrders.Max() : 0;

        var newItems = new List<SpecimenTypeIndicator>();
        var sortOrder = maxSortOrder;

        foreach (var indicatorId in input.IndicatorIds)
        {
            // Skip if already exists
            if (existingIndicatorIds.Contains(indicatorId))
            {
                continue;
            }

            sortOrder++;
            var entity = new SpecimenTypeIndicator(
                GuidGenerator.Create(),
                specimenTypeId,
                indicatorId
            )
            {
                SortOrder = sortOrder
            };

            newItems.Add(entity);
            existingIndicatorIds.Add(indicatorId);
        }

        // Insert only new items
        if (newItems.Any())
        {
            await _specimenTypeIndicatorRepository.InsertManyAsync(newItems);
        }
    }

    public async Task RemoveIndicatorAsync(Guid specimenTypeId, Guid indicatorId)
    {
        var query = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var entity = query.FirstOrDefault(sti =>
            sti.SpecimenTypeId == specimenTypeId && sti.IndicatorId == indicatorId);

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("SpecimenTypeIndicator");
        }

        await _specimenTypeIndicatorRepository.DeleteAsync(entity);
    }

    public async Task<int> RemoveIndicatorsBatchAsync(Guid specimenTypeId, List<Guid> indicatorIds)
    {
        if (indicatorIds == null || indicatorIds.Count == 0)
        {
            return 0;
        }

        var query = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var entities = query
            .Where(sti => sti.SpecimenTypeId == specimenTypeId && indicatorIds.Contains(sti.IndicatorId))
            .ToList();

        if (entities.Count == 0)
        {
            return 0;
        }

        await _specimenTypeIndicatorRepository.DeleteManyAsync(entities);
        return entities.Count;
    }

    public async Task<SpecimenTypeIndicatorDto> ToggleIndicatorActiveAsync(Guid specimenTypeId, Guid indicatorId)
    {
        var query = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var entity = query.FirstOrDefault(sti =>
            sti.SpecimenTypeId == specimenTypeId && sti.IndicatorId == indicatorId);

        if (entity == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("SpecimenTypeIndicator");
        }

        entity.IsActive = !entity.IsActive;
        await _specimenTypeIndicatorRepository.UpdateAsync(entity);

        // Get indicator and group info for response
        var indicator = await _indicatorRepository.FindAsync(entity.IndicatorId);
        IndicatorGroup? indicatorGroup = null;
        if (indicator != null)
        {
            indicatorGroup = await _indicatorGroupRepository.FindAsync(indicator.IndicatorGroupId);
        }

        return new SpecimenTypeIndicatorDto
        {
            Id = entity.Id,
            SpecimenTypeId = entity.SpecimenTypeId,
            IndicatorId = entity.IndicatorId,
            IndicatorCode = indicator?.Code ?? "",
            IndicatorName = indicator?.Name ?? "",
            IndicatorFormattedName = indicator?.FormattedName,
            IndicatorGroupName = indicatorGroup?.Name,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
    }

    #endregion
}
