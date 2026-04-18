using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class IndicatorAppService : ApplicationService, IIndicatorAppService
{
    private const string CodePrefix = "CT";

    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<TestRequestIndicator, Guid> _testRequestIndicatorRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IDataFilter _dataFilter;
    private readonly ICodeGeneratorService _codeGenerator;

    public IndicatorAppService(
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<TestRequestIndicator, Guid> testRequestIndicatorRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IDataFilter dataFilter,
        ICodeGeneratorService codeGenerator)
    {
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _testRequestIndicatorRepository = testRequestIndicatorRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _testMethodRepository = testMethodRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _dataFilter = dataFilter;
        _codeGenerator = codeGenerator;
    }

    public async Task<PagedResultDto<IndicatorDto>> GetListAsync(IndicatorFilterDto input)
    {
        var query = await _indicatorRepository.GetQueryableAsync();
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();

        // Filter: Only get root indicators (ParentIndicatorId == null)
        query = query.Where(x => x.ParentIndicatorId == null);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var searchLower = input.Search.ToLower();
            var matchingGroupIds = groupQuery
                .Where(g => g.Name.ToLower().Contains(searchLower))
                .Select(g => g.Id)
                .ToList();

            query = query.Where(x =>
                x.Code.ToLower().Contains(searchLower) ||
                x.Name.ToLower().Contains(searchLower) ||
                matchingGroupIds.Contains(x.IndicatorGroupId)
            );
        }

        if (input.IndicatorGroupId.HasValue)
        {
            query = query.Where(x => x.IndicatorGroupId == input.IndicatorGroupId.Value);
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        // Filter by SpecimenType(s) or TestMethod(s) via TestMethodIndicator join
        if (input.SpecimenTypeIds?.Any() == true || input.TestMethodIds?.Any() == true)
        {
            var tmiQuery = _testMethodIndicatorRepository.GetQueryableAsync().Result;

            if (input.SpecimenTypeIds?.Any() == true)
            {
                tmiQuery = tmiQuery.Where(tmi => tmi.SpecimenTypeId.HasValue && input.SpecimenTypeIds.Contains(tmi.SpecimenTypeId.Value));
            }
            if (input.TestMethodIds?.Any() == true)
            {
                tmiQuery = tmiQuery.Where(tmi => input.TestMethodIds.Contains(tmi.TestMethodId));
            }

            var matchingIndicatorIds = tmiQuery.Select(tmi => tmi.IndicatorId).Distinct().ToList();
            query = query.Where(x => matchingIndicatorIds.Contains(x.Id));
        }

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = await MapToDtosWithSubIndicatorsAsync(items);

        return new PagedResultDto<IndicatorDto>(totalCount, dtos);
    }

    public async Task<IndicatorDto> GetAsync(Guid id)
    {
        var entity = await _indicatorRepository.GetAsync(id);
        return await MapToDtoAsync(entity);
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public async Task<IndicatorDto> CreateAsync(CreateIndicatorDto input)
    {
        // Validate group exists
        var group = await _indicatorGroupRepository.GetAsync(input.IndicatorGroupId);

        var code = await GenerateCodeAsync();
        // Normalize name for consistent Unicode representation
        var normalizedName = StringHelper.NormalizeAndTrim(input.Name);
        var entity = new Indicator(GuidGenerator.Create(), code, normalizedName, input.IndicatorGroupId)
        {
            IsActive = input.IsActive,
            SortOrder = input.SortOrder,
            FormattedName = SanitizeHtml(input.FormattedName)
        };

        await _indicatorRepository.InsertAsync(entity);

        return await MapToDtoAsync(entity);
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public async Task<List<IndicatorDto>> CreateBulkAsync(CreateBulkIndicatorsDto input)
    {
        // Validate group exists
        var group = await _indicatorGroupRepository.GetAsync(input.IndicatorGroupId);

        var results = new List<Indicator>();

        // Get all existing codes once (including soft deleted to avoid duplicate code)
        var existingCodes = await GetAllExistingCodesAsync();

        // Find max sequence number
        var nextSequence = 1;
        if (existingCodes.Any())
        {
            nextSequence = existingCodes
                .Select(code =>
                {
                    var numberPart = code.Replace("CT-", "");
                    return int.TryParse(numberPart, out var num) ? num : 0;
                })
                .Max() + 1;
        }

        foreach (var item in input.Indicators)
        {
            // 1. Create parent indicator - normalize name for consistent Unicode
            var parentCode = _codeGenerator.GenerateCode(CodePrefix, nextSequence - 1);
            existingCodes.Add(parentCode);
            nextSequence++;

            var normalizedParentName = StringHelper.NormalizeAndTrim(item.Name);
            var parentEntity = new Indicator(
                GuidGenerator.Create(),
                parentCode,
                normalizedParentName,
                input.IndicatorGroupId,
                parentIndicatorId: null
            )
            {
                IsActive = true,
                FormattedName = !string.IsNullOrWhiteSpace(item.FormattedName)
                    ? SanitizeHtml(item.FormattedName)
                    : normalizedParentName
            };

            await _indicatorRepository.InsertAsync(parentEntity, autoSave: true);
            results.Add(parentEntity);

            // 2. Create sub-indicators if any
            if (item.SubIndicators != null && item.SubIndicators.Count > 0)
            {
                var subSequence = 1;

                foreach (var subItem in item.SubIndicators)
                {
                    var subCode = GenerateSubIndicatorCode(parentCode, subSequence);
                    existingCodes.Add(subCode);
                    subSequence++;

                    // Normalize sub-indicator name for consistent Unicode
                    var normalizedSubName = StringHelper.NormalizeAndTrim(subItem.Name);
                    var subEntity = new Indicator(
                        GuidGenerator.Create(),
                        subCode,
                        normalizedSubName,
                        input.IndicatorGroupId,
                        parentIndicatorId: parentEntity.Id
                    )
                    {
                        IsActive = true,
                        FormattedName = !string.IsNullOrWhiteSpace(subItem.FormattedName)
                            ? SanitizeHtml(subItem.FormattedName)
                            : normalizedSubName,
                        SortOrder = subSequence
                    };

                    await _indicatorRepository.InsertAsync(subEntity, autoSave: true);
                    results.Add(subEntity);
                }
            }
        }

        return await MapToDtosAsync(results);
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public async Task<IndicatorDto> UpdateAsync(Guid id, UpdateIndicatorDto input)
    {
        var entity = await _indicatorRepository.GetAsync(id);

        // Validate group exists
        var group = await _indicatorGroupRepository.GetAsync(input.IndicatorGroupId);

        // Normalize name for consistent Unicode representation
        entity.Name = StringHelper.NormalizeAndTrim(input.Name);
        entity.FormattedName = SanitizeHtml(input.FormattedName);
        entity.IsActive = input.IsActive;
        entity.IndicatorGroupId = input.IndicatorGroupId;
        entity.SortOrder = input.SortOrder;

        await _indicatorRepository.UpdateAsync(entity);

        return await MapToDtoAsync(entity);
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public async Task<IndicatorDto> UpdateWithSubIndicatorsAsync(Guid id, UpdateIndicatorWithSubsDto input)
    {
        // 1. Update parent indicator
        var entity = await _indicatorRepository.GetAsync(id);
        await _indicatorGroupRepository.GetAsync(input.IndicatorGroupId); // validate group exists

        entity.Name = StringHelper.NormalizeAndTrim(input.Name);
        entity.FormattedName = SanitizeHtml(input.FormattedName);
        entity.IsActive = input.IsActive;
        entity.IndicatorGroupId = input.IndicatorGroupId;
        entity.SortOrder = input.SortOrder;
        await _indicatorRepository.UpdateAsync(entity);

        // 2. Load existing sub-indicators
        var subQuery = await _indicatorRepository.GetQueryableAsync();
        var existingSubs = subQuery
            .Where(x => x.ParentIndicatorId == id)
            .ToList();

        // 3. Determine which subs to keep
        var incomingIds = input.SubIndicators
            .Where(s => s.Id.HasValue)
            .Select(s => s.Id!.Value)
            .ToHashSet();

        // 4. Delete removed sub-indicators (check usage first)
        var testRequestQuery = await _testRequestIndicatorRepository.GetQueryableAsync();
        foreach (var existingSub in existingSubs)
        {
            if (!incomingIds.Contains(existingSub.Id))
            {
                var isUsed = testRequestQuery.Any(x => x.IndicatorId == existingSub.Id);
                if (isUsed)
                {
                    throw new UserFriendlyException(
                        $"Không thể xóa chỉ tiêu phụ '{existingSub.Name}' vì đã được sử dụng trong phiếu yêu cầu.");
                }
                await _indicatorRepository.DeleteAsync(existingSub.Id);
            }
        }

        // 5. Update existing and create new sub-indicators
        var existingSubsDict = existingSubs.ToDictionary(s => s.Id);
        var nextSubSequence = await GetNextSubIndicatorSequenceAsync(id);

        foreach (var subDto in input.SubIndicators)
        {
            if (subDto.Id.HasValue && existingSubsDict.TryGetValue(subDto.Id.Value, out var existingSub))
            {
                // Update existing
                existingSub.Name = StringHelper.NormalizeAndTrim(subDto.Name);
                existingSub.FormattedName = SanitizeHtml(subDto.FormattedName);
                await _indicatorRepository.UpdateAsync(existingSub);
            }
            else
            {
                // Create new
                var subCode = GenerateSubIndicatorCode(entity.Code, nextSubSequence);
                nextSubSequence++;

                var normalizedName = StringHelper.NormalizeAndTrim(subDto.Name);
                var newSub = new Indicator(
                    GuidGenerator.Create(),
                    subCode,
                    normalizedName,
                    entity.IndicatorGroupId,
                    parentIndicatorId: id
                )
                {
                    IsActive = true,
                    FormattedName = SanitizeHtml(subDto.FormattedName) ?? normalizedName,
                    SortOrder = nextSubSequence
                };
                await _indicatorRepository.InsertAsync(newSub, autoSave: true);
            }
        }

        return await MapToDtoAsync(entity);
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var indicator = await _indicatorRepository.GetAsync(id);

        // Check if can delete
        var canDelete = await CanDeleteAsync(id);
        if (!canDelete)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Không thể xóa chỉ tiêu này vì đã được sử dụng trong phiếu yêu cầu.",
                "id");
        }

        // If this is a parent, delete all sub-indicators first
        if (indicator.ParentIndicatorId == null)
        {
            var query = await _indicatorRepository.GetQueryableAsync();
            var subIndicators = query.Where(x => x.ParentIndicatorId == id).ToList();

            // Check if any sub-indicator is used
            foreach (var sub in subIndicators)
            {
                var canDeleteSub = await CanDeleteAsync(sub.Id);
                if (!canDeleteSub)
                {
                    throw new UserFriendlyException(
                        $"Không thể xóa vì chỉ tiêu phụ '{sub.Name}' đã được sử dụng trong phiếu yêu cầu.");
                }
            }

            // Delete all sub-indicators
            foreach (var sub in subIndicators)
            {
                await _indicatorRepository.DeleteAsync(sub.Id);
            }
        }

        // Delete parent or standalone indicator
        await _indicatorRepository.DeleteAsync(id);
    }

    public async Task<bool> CanDeleteAsync(Guid id)
    {
        var query = await _testRequestIndicatorRepository.GetQueryableAsync();
        return !query.Any(x => x.IndicatorId == id);
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public async Task<IndicatorDto> ToggleActiveAsync(Guid id)
    {
        var entity = await _indicatorRepository.GetAsync(id);
        entity.IsActive = !entity.IsActive;
        await _indicatorRepository.UpdateAsync(entity);

        return await MapToDtoAsync(entity);
    }

    public async Task<List<IndicatorDto>> GetByGroupAsync(Guid groupId)
    {
        var query = await _indicatorRepository.GetQueryableAsync();
        var items = query
            .Where(x => x.IndicatorGroupId == groupId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return await MapToDtosAsync(items);
    }

    public async Task<List<IndicatorDto>> GetActiveIndicatorsAsync()
    {
        var query = await _indicatorRepository.GetQueryableAsync();
        // Only get parent indicators (no parentIndicatorId)
        var parentIndicators = query
            .Where(x => x.ParentIndicatorId == null)
            .WhereActive()
            .OrderByThenBy(x => x.SortOrder, x => x.Name)
            .ToList();

        // Map with sub-indicators included
        return await MapToDtosWithSubIndicatorsAsync(parentIndicators);
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Import)]
    public async Task<ImportIndicatorsResultDto> ImportFromExcelAsync(Stream fileStream, string fileName)
    {
        var result = new ImportIndicatorsResultDto();

        // Get all groups for lookup - normalize names for consistent comparison (use GroupBy to handle duplicates)
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();
        var groups = groupQuery
            .ToList()
            .GroupBy(g => StringHelper.NormalizeAndTrim(g.Name).ToLower())
            .ToDictionary(g => g.Key, g => g.First().Id);

        // Read Excel using MiniExcel
        var rows = MiniExcelLibs.MiniExcel.Query(fileStream, useHeaderRow: true).ToList();
        result.TotalRows = rows.Count;

        // Get next sequence once before the loop
        var nextSequence = await GetNextSequenceAsync();
        var sequenceOffset = 0;

        foreach (var row in rows.Select((value, index) => new { value, index }))
        {
            var rowNumber = row.index + 2; // +2 because header is row 1, and index starts at 0
            var rowData = row.value as IDictionary<string, object>;

            if (rowData == null)
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorErrorDto
                {
                    RowNumber = rowNumber,
                    IndicatorName = "",
                    ErrorMessage = "Không thể đọc dữ liệu dòng này"
                });
                continue;
            }

            // Get values from row - normalize for consistent Unicode representation
            var name = rowData.ContainsKey("Tên chỉ tiêu")
                ? StringHelper.NormalizeAndTrim(rowData["Tên chỉ tiêu"]?.ToString())
                : null;
            var priceStr = rowData.ContainsKey("Đơn giá") ? rowData["Đơn giá"]?.ToString() : null;
            var groupName = rowData.ContainsKey("Tên nhóm chỉ tiêu")
                ? StringHelper.NormalizeAndTrim(rowData["Tên nhóm chỉ tiêu"]?.ToString())
                : null;

            // Validate name
            if (string.IsNullOrWhiteSpace(name))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorErrorDto
                {
                    RowNumber = rowNumber,
                    IndicatorName = name ?? "",
                    ErrorMessage = "Tên chỉ tiêu không được để trống"
                });
                continue;
            }

            // Validate group
            if (string.IsNullOrWhiteSpace(groupName))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorErrorDto
                {
                    RowNumber = rowNumber,
                    IndicatorName = name,
                    ErrorMessage = "Tên nhóm chỉ tiêu không được để trống"
                });
                continue;
            }

            var groupKey = groupName.ToLower();
            if (!groups.ContainsKey(groupKey))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorErrorDto
                {
                    RowNumber = rowNumber,
                    IndicatorName = name,
                    ErrorMessage = $"Không tìm thấy nhóm chỉ tiêu '{groupName}'"
                });
                continue;
            }

            // Create indicator
            try
            {
                var code = $"CT-{(nextSequence + sequenceOffset).ToString("D4")}";
                var entity = new Indicator(GuidGenerator.Create(), code, name, groups[groupKey])
                {
                    IsActive = true
                };

                await _indicatorRepository.InsertAsync(entity, autoSave: true);
                result.SuccessCount++;
                sequenceOffset++; // Only increment on success
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorErrorDto
                {
                    RowNumber = rowNumber,
                    IndicatorName = name,
                    ErrorMessage = $"Lỗi khi tạo chỉ tiêu: {ex.Message}"
                });
            }
        }

        return result;
    }

    public async Task<byte[]> GetExcelTemplateAsync()
    {
        // Query all indicators with group names
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();

        var query = from ind in indicatorQuery
                    join g in groupQuery on ind.IndicatorGroupId equals g.Id into gj
                    from g in gj.DefaultIfEmpty()
                    where ind.IsActive
                    orderby g.Name, ind.SortOrder, ind.Name
                    select new { ind.Name, ind.IndicatorGroupId, GroupName = g != null ? g.Name : "" };

        var indicators = await AsyncExecuter.ToListAsync(query);

        // Fallback: if no data, return template with sample row
        if (indicators.Count == 0)
        {
            var activeGroups = await AsyncExecuter.ToListAsync(
                from g in groupQuery where g.IsActive orderby g.Name select g.Name);
            var sampleData = new List<Dictionary<string, object>>
            {
                new() { { "Tên chỉ tiêu", "Ví dụ: Coliform" }, { "Đơn giá", 50000 }, { "Tên nhóm chỉ tiêu", activeGroups.FirstOrDefault() ?? "Tên nhóm" } }
            };
            using var s = new MemoryStream();
            MiniExcelLibs.MiniExcel.SaveAs(s, sampleData);
            return s.ToArray();
        }

        var templateData = indicators.Select(ind => new Dictionary<string, object>
        {
            { "Tên chỉ tiêu", ind.Name ?? "" },
            { "Đơn giá", 0 },
            { "Tên nhóm chỉ tiêu", ind.GroupName ?? "" }
        }).ToList();

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return stream.ToArray();
    }

    private async Task<int> GetNextSequenceAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate code)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _indicatorRepository.GetQueryableAsync();

            // Get all codes that start with CT- and extract the max number
            var codes = query
                .Where(x => x.Code.StartsWith("CT-"))
                .Select(x => x.Code)
                .ToList();

            if (!codes.Any())
            {
                return 1;
            }

            // Find the maximum number from all codes
            var maxNumber = codes
                .Select(code =>
                {
                    var numberPart = code.Replace("CT-", "");
                    return int.TryParse(numberPart, out var num) ? num : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return maxNumber + 1;
        }
    }

    private async Task<string> GenerateCodeAsync()
    {
        var nextSequence = await GetNextSequenceAsync();
        return _codeGenerator.GenerateCode(CodePrefix, nextSequence - 1);
    }

    /// <summary>
    /// Generate code for sub-indicator: CT-0001 → CT-0001-01
    /// </summary>
    private string GenerateSubIndicatorCode(string parentCode, int sequenceNumber)
    {
        return $"{parentCode}-{sequenceNumber.ToString("D2")}";
    }

    /// <summary>
    /// Get next sub-indicator sequence for a parent
    /// </summary>
    private async Task<int> GetNextSubIndicatorSequenceAsync(Guid parentIndicatorId)
    {
        var query = await _indicatorRepository.GetQueryableAsync();

        var maxSequence = query
            .Where(x => x.ParentIndicatorId == parentIndicatorId)
            .Select(x => x.Code)
            .ToList()
            .Select(code =>
            {
                var parts = code.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out var num))
                    return num;
                return 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return maxSequence + 1;
    }

    /// <summary>
    /// Get all existing codes including soft deleted records to avoid duplicate code violation
    /// </summary>
    private async Task<HashSet<string>> GetAllExistingCodesAsync()
    {
        // Disable soft delete filter to include deleted records
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _indicatorRepository.GetQueryableAsync();
            return query
                .Where(x => x.Code.StartsWith("CT-"))
                .Select(x => x.Code)
                .ToHashSet();
        }
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        var prefix = $"{CodePrefix}-";
        var query = await _indicatorRepository.GetQueryableAsync();
        var existingCodes = query
            .Where(x => x.Code.StartsWith(prefix))
            .Select(x => x.Code)
            .ToHashSet();

        var nextSequence = 1;
        if (existingCodes.Any())
        {
            nextSequence = existingCodes
                .Select(code =>
                {
                    var numberPart = code.Replace(prefix, "");
                    return int.TryParse(numberPart, out var num) ? num : 0;
                })
                .Max() + 1;
        }

        var newCode = _codeGenerator.GenerateCode(CodePrefix, nextSequence - 1);

        // Double check to ensure uniqueness (in case of race condition)
        while (existingCodes.Contains(newCode))
        {
            nextSequence++;
            newCode = _codeGenerator.GenerateCode(CodePrefix, nextSequence - 1);
        }

        return newCode;
    }

    private async Task<IndicatorDto> MapToDtoAsync(Indicator entity)
    {
        var dto = ObjectMapper.Map<Indicator, IndicatorDto>(entity);

        // Get group name
        var group = await _indicatorGroupRepository.FindAsync(entity.IndicatorGroupId);
        dto.IndicatorGroupName = group?.Name ?? "";

        // Check can delete
        dto.CanDelete = await CanDeleteAsync(entity.Id);

        return dto;
    }

    private async Task<List<IndicatorDto>> MapToDtosAsync(List<Indicator> entities)
    {
        var groupIds = entities.Select(x => x.IndicatorGroupId).Distinct().ToList();
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();
        var groups = groupQuery.Where(x => groupIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Name);

        // Check which indicators can be deleted
        var indicatorIds = entities.Select(x => x.Id).ToList();
        var testRequestIndicatorQuery = await _testRequestIndicatorRepository.GetQueryableAsync();
        var usedIndicatorIds = testRequestIndicatorQuery
            .Where(x => indicatorIds.Contains(x.IndicatorId))
            .Select(x => x.IndicatorId)
            .Distinct()
            .ToList();

        var dtos = new List<IndicatorDto>();
        foreach (var entity in entities)
        {
            var dto = ObjectMapper.Map<Indicator, IndicatorDto>(entity);
            dto.IndicatorGroupName = groups.GetValueOrDefault(entity.IndicatorGroupId, "");
            dto.CanDelete = !usedIndicatorIds.Contains(entity.Id);
            dtos.Add(dto);
        }

        return dtos;
    }

    /// <summary>
    /// Map parent indicators with their sub-indicators
    /// </summary>
    private async Task<List<IndicatorDto>> MapToDtosWithSubIndicatorsAsync(List<Indicator> parentIndicators)
    {
        var parentIds = parentIndicators.Select(x => x.Id).ToList();

        // Load all sub-indicators in one query (avoid N+1)
        var subIndicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var allSubIndicators = subIndicatorQuery
            .Where(x => x.ParentIndicatorId.HasValue && parentIds.Contains(x.ParentIndicatorId.Value))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToList();

        // Group by parent
        var subIndicatorsByParent = allSubIndicators
            .GroupBy(x => x.ParentIndicatorId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Get group names
        var groupIds = parentIndicators.Select(x => x.IndicatorGroupId).Distinct().ToList();
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();
        var groups = groupQuery.Where(x => groupIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Name);

        // Check deletability for all indicators (parents + subs)
        var allIndicatorIds = parentIds.Concat(allSubIndicators.Select(x => x.Id)).ToList();
        var testRequestIndicatorQuery = await _testRequestIndicatorRepository.GetQueryableAsync();
        var usedIndicatorIds = testRequestIndicatorQuery
            .Where(x => allIndicatorIds.Contains(x.IndicatorId))
            .Select(x => x.IndicatorId)
            .Distinct()
            .ToList();

        var dtos = new List<IndicatorDto>();

        foreach (var parent in parentIndicators)
        {
            var dto = ObjectMapper.Map<Indicator, IndicatorDto>(parent);
            dto.IndicatorGroupName = groups.GetValueOrDefault(parent.IndicatorGroupId, "");
            dto.CanDelete = !usedIndicatorIds.Contains(parent.Id);
            dto.HasSubIndicators = subIndicatorsByParent.ContainsKey(parent.Id);

            // Map sub-indicators
            if (subIndicatorsByParent.TryGetValue(parent.Id, out var subs))
            {
                dto.SubIndicators = subs.Select(sub =>
                {
                    var subDto = ObjectMapper.Map<Indicator, IndicatorDto>(sub);
                    subDto.IndicatorGroupName = groups.GetValueOrDefault(sub.IndicatorGroupId, "");
                    subDto.CanDelete = !usedIndicatorIds.Contains(sub.Id);
                    subDto.ParentIndicatorCode = parent.Code;
                    return subDto;
                }).ToList();
            }

            dtos.Add(dto);
        }

        return dtos;
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public async Task<BatchOperationResultDto> DeleteBatchAsync(BatchIndicatorIdsDto input)
    {
        var result = new BatchOperationResultDto
        {
            TotalCount = input.Ids.Count
        };

        // Get all indicators to delete
        var query = await _indicatorRepository.GetQueryableAsync();
        var indicators = query.Where(x => input.Ids.Contains(x.Id)).ToList();

        // Check which indicators are used in test requests
        var testRequestIndicatorQuery = await _testRequestIndicatorRepository.GetQueryableAsync();
        var usedIndicatorIds = testRequestIndicatorQuery
            .Where(x => input.Ids.Contains(x.IndicatorId))
            .Select(x => x.IndicatorId)
            .Distinct()
            .ToList();

        foreach (var indicator in indicators)
        {
            if (usedIndicatorIds.Contains(indicator.Id))
            {
                result.FailedCount++;
                result.Errors.Add(new BatchOperationErrorDto
                {
                    Id = indicator.Id,
                    Name = indicator.Name,
                    ErrorMessage = "Không thể xóa vì đã được sử dụng trong phiếu yêu cầu"
                });
            }
            else
            {
                await _indicatorRepository.DeleteAsync(indicator.Id);
                result.SuccessCount++;
            }
        }

        return result;
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public async Task<BatchOperationResultDto> DeactivateBatchAsync(BatchIndicatorIdsDto input)
    {
        var result = new BatchOperationResultDto
        {
            TotalCount = input.Ids.Count
        };

        var query = await _indicatorRepository.GetQueryableAsync();
        var indicators = query.Where(x => input.Ids.Contains(x.Id)).ToList();

        foreach (var indicator in indicators)
        {
            indicator.IsActive = false;
            await _indicatorRepository.UpdateAsync(indicator);
            result.SuccessCount++;
        }

        return result;
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public async Task<BatchOperationResultDto> ActivateBatchAsync(BatchIndicatorIdsDto input)
    {
        var result = new BatchOperationResultDto
        {
            TotalCount = input.Ids.Count
        };

        var query = await _indicatorRepository.GetQueryableAsync();
        var indicators = query.Where(x => input.Ids.Contains(x.Id)).ToList();

        foreach (var indicator in indicators)
        {
            indicator.IsActive = true;
            await _indicatorRepository.UpdateAsync(indicator);
            result.SuccessCount++;
        }

        return result;
    }

    /// <summary>
    /// Lấy danh sách chỉ tiêu với phương pháp thử (chỉ lấy phương pháp đang hoạt động)
    /// </summary>
    public async Task<List<IndicatorWithMethodsDto>> GetAllWithMethodsAsync()
    {
        // Get all indicators (including inactive) so FE can show methods for all
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var indicators = indicatorQuery
            .OrderByThenBy(x => x.SortOrder, x => x.Name)
            .ToList();

        if (!indicators.Any())
        {
            return new List<IndicatorWithMethodsDto>();
        }

        // Get indicator group names
        var groupIds = indicators.Select(x => x.IndicatorGroupId).Distinct().ToList();
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();
        var groups = groupQuery
            .Where(x => groupIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Name);

        // Get all test method indicators for active indicators
        var indicatorIds = indicators.Select(x => x.Id).ToList();
        var testMethodIndicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var testMethodIndicators = testMethodIndicatorQuery
            .Where(x => indicatorIds.Contains(x.IndicatorId))
            .ToList();

        // Get all test methods (only active ones)
        var testMethodIds = testMethodIndicators.Select(x => x.TestMethodId).Distinct().ToList();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var testMethods = testMethodQuery
            .Where(x => testMethodIds.Contains(x.Id))
            .ToDictionary(x => x.Id);

        // Get specimen types for enrichment
        var specimenTypeIds = testMethodIndicators.Where(x => x.SpecimenTypeId.HasValue).Select(x => x.SpecimenTypeId!.Value).Distinct().ToList();
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeDict = specimenTypes.ToDictionary(st => st.Id);

        // Map to DTOs
        var result = new List<IndicatorWithMethodsDto>();
        foreach (var indicator in indicators)
        {
            var dto = new IndicatorWithMethodsDto
            {
                Id = indicator.Id,
                Code = indicator.Code,
                Name = indicator.Name,
                FormattedName = indicator.FormattedName,
                IndicatorGroupId = indicator.IndicatorGroupId,
                IndicatorGroupName = groups.GetValueOrDefault(indicator.IndicatorGroupId, ""),
                IsActive = indicator.IsActive,
                SortOrder = indicator.SortOrder,
                Methods = new List<IndicatorMethodDto>()
            };

            // Get methods for this indicator
            var indicatorMethods = testMethodIndicators
                .Where(x => x.IndicatorId == indicator.Id)
                .OrderBy(x => x.SortOrder)
                .ToList();

            foreach (var methodIndicator in indicatorMethods)
            {
                if (testMethods.TryGetValue(methodIndicator.TestMethodId, out var testMethod))
                {
                    var specimenType = methodIndicator.SpecimenTypeId.HasValue && specimenTypeDict.TryGetValue(methodIndicator.SpecimenTypeId.Value, out var st) ? st : null;
                    dto.Methods.Add(new IndicatorMethodDto
                    {
                        TestMethodId = testMethod.Id,
                        TestMethodCode = testMethod.Code,
                        TestMethodName = testMethod.Name,
                        TestMethodFormattedName = testMethod.FormattedName,
                        UnitPrice = methodIndicator.UnitPrice,
                        IsActive = methodIndicator.IsActive,
                        SortOrder = methodIndicator.SortOrder,
                        SpecimenTypeId = methodIndicator.SpecimenTypeId,
                        SpecimenTypeCode = specimenType?.Code,
                        SpecimenTypeName = specimenType?.Name,
                        Unit = methodIndicator.Unit ?? testMethod.Unit,
                        FormattedUnit = methodIndicator.FormattedUnit ?? testMethod.FormattedUnit
                    });
                }
            }

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// Sanitize HTML to only allow safe formatting tags (bold, italic, underline, sub, sup)
    /// </summary>
    private static string? SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // Allowed tags for text formatting
        var allowedTags = new[] { "b", "strong", "i", "em", "u", "sub", "sup", "br" };

        // Simple sanitization: remove all tags except allowed ones
        // For production, consider using HtmlSanitizer library
        var result = html;

        // Remove script tags and their content
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"<script[^>]*>[\s\S]*?</script>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove style tags and their content
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"<style[^>]*>[\s\S]*?</style>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove event handlers (onclick, onerror, etc.)
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"\s*on\w+\s*=\s*[""'][^""']*[""']",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove javascript: protocol
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"javascript\s*:",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Only keep allowed tags - remove others but keep their content
        var tagPattern = @"</?([a-zA-Z][a-zA-Z0-9]*)[^>]*>";
        result = System.Text.RegularExpressions.Regex.Replace(result, tagPattern, match =>
        {
            var tagName = match.Groups[1].Value.ToLower();
            if (allowedTags.Contains(tagName))
            {
                // Keep allowed tags but remove any attributes
                var isClosing = match.Value.StartsWith("</");
                return isClosing ? $"</{tagName}>" : $"<{tagName}>";
            }
            return ""; // Remove disallowed tags
        });

        return result.Trim();
    }
}
