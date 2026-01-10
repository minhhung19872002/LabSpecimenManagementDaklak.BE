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
    private readonly IRepository<SpecimenTypeIndicator, Guid> _specimenTypeIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;

    public SpecimenTypeAppService(
        IRepository<SpecimenType, Guid> repository,
        IRepository<SpecimenTypeIndicator, Guid> specimenTypeIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository)
        : base(repository)
    {
        _specimenTypeIndicatorRepository = specimenTypeIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
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

        return new ListResultDto<SpecimenTypeDto>(
            ObjectMapper.Map<List<SpecimenType>, List<SpecimenTypeDto>>(items)
        );
    }

    #region Indicator Management

    public async Task<List<SpecimenTypeWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        var specimenTypes = await Repository.GetListAsync();
        var specimenTypeIndicators = await _specimenTypeIndicatorRepository.GetListAsync();
        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorGroups = await _indicatorGroupRepository.GetListAsync();

        var result = specimenTypes
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(st =>
            {
                var dto = ObjectMapper.Map<SpecimenType, SpecimenTypeWithIndicatorsDto>(st);

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
            IndicatorGroupName = indicatorGroup?.Name,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
    }

    #endregion
}
