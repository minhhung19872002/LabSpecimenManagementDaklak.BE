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
[Authorize(LabSpecimenManagementPermissions.Indicators.Default)]
public class IndicatorAppService : ApplicationService, IIndicatorAppService
{
    private const string CodePrefix = "CT";

    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<TestRequestIndicator, Guid> _testRequestIndicatorRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IDataFilter _dataFilter;
    private readonly ICodeGeneratorService _codeGenerator;

    public IndicatorAppService(
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<TestRequestIndicator, Guid> testRequestIndicatorRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IDataFilter dataFilter,
        ICodeGeneratorService codeGenerator)
    {
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _testRequestIndicatorRepository = testRequestIndicatorRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _testMethodRepository = testMethodRepository;
        _dataFilter = dataFilter;
        _codeGenerator = codeGenerator;
    }

    public async Task<PagedResultDto<IndicatorDto>> GetListAsync(IndicatorFilterDto input)
    {
        var query = await _indicatorRepository.GetQueryableAsync();
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();

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

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = await MapToDtosAsync(items);

        return new PagedResultDto<IndicatorDto>(totalCount, dtos);
    }

    public async Task<IndicatorDto> GetAsync(Guid id)
    {
        var entity = await _indicatorRepository.GetAsync(id);
        return await MapToDtoAsync(entity);
    }

    [Authorize(LabSpecimenManagementPermissions.Indicators.Create)]
    public async Task<IndicatorDto> CreateAsync(CreateIndicatorDto input)
    {
        // Validate group exists
        var group = await _indicatorGroupRepository.GetAsync(input.IndicatorGroupId);

        var code = await GenerateCodeAsync();
        var entity = new Indicator(GuidGenerator.Create(), code, input.Name, input.IndicatorGroupId)
        {
            IsActive = input.IsActive,
            SortOrder = input.SortOrder
        };

        await _indicatorRepository.InsertAsync(entity);

        return await MapToDtoAsync(entity);
    }

    [Authorize(LabSpecimenManagementPermissions.Indicators.Create)]
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

        for (int i = 0; i < input.Indicators.Count; i++)
        {
            var item = input.Indicators[i];

            // Generate code and add to existingCodes to prevent duplicates in same batch
            var code = _codeGenerator.GenerateCode(CodePrefix, nextSequence - 1);
            existingCodes.Add(code);
            nextSequence++;

            var entity = new Indicator(GuidGenerator.Create(), code, item.Name, input.IndicatorGroupId)
            {
                IsActive = true
            };

            await _indicatorRepository.InsertAsync(entity, autoSave: true);
            results.Add(entity);
        }

        return await MapToDtosAsync(results);
    }

    [Authorize(LabSpecimenManagementPermissions.Indicators.Edit)]
    public async Task<IndicatorDto> UpdateAsync(Guid id, UpdateIndicatorDto input)
    {
        var entity = await _indicatorRepository.GetAsync(id);

        // Validate group exists
        var group = await _indicatorGroupRepository.GetAsync(input.IndicatorGroupId);

        entity.Name = input.Name;
        entity.IsActive = input.IsActive;
        entity.IndicatorGroupId = input.IndicatorGroupId;
        entity.SortOrder = input.SortOrder;

        await _indicatorRepository.UpdateAsync(entity);

        return await MapToDtoAsync(entity);
    }

    [Authorize(LabSpecimenManagementPermissions.Indicators.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var canDelete = await CanDeleteAsync(id);
        if (!canDelete)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Không thể xóa chỉ tiêu này vì đã được sử dụng trong phiếu yêu cầu.",
                "id");
        }

        await _indicatorRepository.DeleteAsync(id);
    }

    public async Task<bool> CanDeleteAsync(Guid id)
    {
        var query = await _testRequestIndicatorRepository.GetQueryableAsync();
        return !query.Any(x => x.IndicatorId == id);
    }

    [Authorize(LabSpecimenManagementPermissions.Indicators.Edit)]
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
        var items = query
            .WhereActive()
            .OrderByThenBy(x => x.SortOrder, x => x.Name)
            .ToList();

        return await MapToDtosAsync(items);
    }

    [Authorize(LabSpecimenManagementPermissions.Indicators.Import)]
    public async Task<ImportIndicatorsResultDto> ImportFromExcelAsync(Stream fileStream, string fileName)
    {
        var result = new ImportIndicatorsResultDto();

        // Get all groups for lookup
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();
        var groups = groupQuery.ToDictionary(g => g.Name.Trim().ToLower(), g => g.Id);

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

            // Get values from row
            var name = rowData.ContainsKey("Tên chỉ tiêu") ? rowData["Tên chỉ tiêu"]?.ToString()?.Trim() : null;
            var priceStr = rowData.ContainsKey("Đơn giá") ? rowData["Đơn giá"]?.ToString() : null;
            var groupName = rowData.ContainsKey("Tên nhóm chỉ tiêu") ? rowData["Tên nhóm chỉ tiêu"]?.ToString()?.Trim() : null;

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
        // Get all active groups for reference
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();
        var activeGroups = groupQuery.Where(g => g.IsActive).OrderBy(g => g.Name).Select(g => g.Name).ToList();

        // Create template data with sample row
        var templateData = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Tên chỉ tiêu", "Ví dụ: Coliform" },
                { "Đơn giá", 50000 },
                { "Tên nhóm chỉ tiêu", activeGroups.FirstOrDefault() ?? "Tên nhóm" }
            }
        };

        // Add instruction rows
        var instructions = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Tên chỉ tiêu", "" },
                { "Đơn giá", "" },
                { "Tên nhóm chỉ tiêu", "" }
            },
            new Dictionary<string, object>
            {
                { "Tên chỉ tiêu", "=== HƯỚNG DẪN ===" },
                { "Đơn giá", "" },
                { "Tên nhóm chỉ tiêu", "" }
            },
            new Dictionary<string, object>
            {
                { "Tên chỉ tiêu", "Các nhóm chỉ tiêu có sẵn:" },
                { "Đơn giá", "" },
                { "Tên nhóm chỉ tiêu", "" }
            }
        };

        // Add group names as reference
        foreach (var groupName in activeGroups)
        {
            instructions.Add(new Dictionary<string, object>
            {
                { "Tên chỉ tiêu", $"  - {groupName}" },
                { "Đơn giá", "" },
                { "Tên nhóm chỉ tiêu", "" }
            });
        }

        templateData.AddRange(instructions);

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return stream.ToArray();
    }

    private async Task<int> GetNextSequenceAsync()
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

    private async Task<string> GenerateCodeAsync()
    {
        var nextSequence = await GetNextSequenceAsync();
        return _codeGenerator.GenerateCode(CodePrefix, nextSequence - 1);
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

    [Authorize(LabSpecimenManagementPermissions.Indicators.Delete)]
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

    [Authorize(LabSpecimenManagementPermissions.Indicators.Edit)]
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

    [Authorize(LabSpecimenManagementPermissions.Indicators.Edit)]
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
        // Get all active indicators
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var indicators = indicatorQuery
            .WhereActive()
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
            .Where(x => testMethodIds.Contains(x.Id) && x.IsActive)
            .ToDictionary(x => x.Id);

        // Map to DTOs
        var result = new List<IndicatorWithMethodsDto>();
        foreach (var indicator in indicators)
        {
            var dto = new IndicatorWithMethodsDto
            {
                Id = indicator.Id,
                Code = indicator.Code,
                Name = indicator.Name,
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
                    dto.Methods.Add(new IndicatorMethodDto
                    {
                        TestMethodId = testMethod.Id,
                        TestMethodCode = testMethod.Code,
                        TestMethodName = testMethod.Name,
                        UnitPrice = methodIndicator.UnitPrice,
                        IsActive = testMethod.IsActive,
                        SortOrder = methodIndicator.SortOrder
                    });
                }
            }

            result.Add(dto);
        }

        return result;
    }
}
