using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.Indicators.Default)]
public class IndicatorAppService : ApplicationService, IIndicatorAppService
{
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<TestRequestIndicator, Guid> _testRequestIndicatorRepository;

    public IndicatorAppService(
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<TestRequestIndicator, Guid> testRequestIndicatorRepository)
    {
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _testRequestIndicatorRepository = testRequestIndicatorRepository;
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
            UnitPrice = input.UnitPrice,
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

        foreach (var item in input.Indicators)
        {
            var code = await GenerateCodeAsync();
            var entity = new Indicator(GuidGenerator.Create(), code, item.Name, input.IndicatorGroupId)
            {
                UnitPrice = item.UnitPrice,
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
        entity.UnitPrice = input.UnitPrice;
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
            throw new UserFriendlyException("Không thể xóa chỉ tiêu này vì đã được sử dụng trong phiếu yêu cầu.");
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
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
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

            // Validate and parse price
            decimal unitPrice = 0;
            if (!string.IsNullOrWhiteSpace(priceStr))
            {
                priceStr = priceStr.Replace(",", "").Replace(".", "").Replace("đ", "").Replace("VND", "").Trim();
                if (!decimal.TryParse(priceStr, out unitPrice))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new ImportIndicatorErrorDto
                    {
                        RowNumber = rowNumber,
                        IndicatorName = name,
                        ErrorMessage = "Đơn giá không hợp lệ"
                    });
                    continue;
                }
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
                var code = await GenerateCodeAsync();
                var entity = new Indicator(GuidGenerator.Create(), code, name, groups[groupKey])
                {
                    UnitPrice = unitPrice,
                    IsActive = true
                };

                await _indicatorRepository.InsertAsync(entity, autoSave: true);
                result.SuccessCount++;
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

    private async Task<string> GenerateCodeAsync()
    {
        var query = await _indicatorRepository.GetQueryableAsync();
        var count = query.Count();
        var sequence = (count + 1).ToString("D4");
        return $"CT-{sequence}";
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
}
