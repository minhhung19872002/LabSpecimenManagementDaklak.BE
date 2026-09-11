using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class IndicatorGroupAppService :
    CrudAppService<
        IndicatorGroup,
        IndicatorGroupDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateIndicatorGroupDto>,
    IIndicatorGroupAppService
{
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IDataFilter _dataFilter;

    public IndicatorGroupAppService(
        IRepository<IndicatorGroup, Guid> repository,
        IRepository<Indicator, Guid> indicatorRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _indicatorRepository = indicatorRepository;
        _dataFilter = dataFilter;

        GetPolicyName = LabSpecimenManagementPermissions.MasterData.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.MasterData.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.MasterData.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.MasterData.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.MasterData.Delete;
    }

    public async Task<ListResultDto<IndicatorGroupDto>> GetActiveGroupsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<IndicatorGroup>, List<IndicatorGroupDto>>(items);
        await EnrichDtosAsync(dtos);

        return new ListResultDto<IndicatorGroupDto>(dtos);
    }

    public async Task<ListResultDto<IndicatorGroupDto>> GetGroupsWithCountAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<IndicatorGroup>, List<IndicatorGroupDto>>(items);
        await EnrichDtosAsync(dtos);

        return new ListResultDto<IndicatorGroupDto>(dtos);
    }

    public override async Task<PagedResultDto<IndicatorGroupDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.Name)
            .ApplyPaging(input)
            .ToList();

        var dtos = ObjectMapper.Map<List<IndicatorGroup>, List<IndicatorGroupDto>>(items);
        await EnrichDtosAsync(dtos);

        return new PagedResultDto<IndicatorGroupDto>(totalCount, dtos);
    }

    protected override Task<IndicatorGroup> MapToEntityAsync(CreateUpdateIndicatorGroupDto createInput)
    {
        return Task.FromResult(new IndicatorGroup(GuidGenerator.Create(), createInput.Name)
        {
            IsActive = createInput.IsActive
        });
    }

    protected override Task MapToEntityAsync(CreateUpdateIndicatorGroupDto updateInput, IndicatorGroup entity)
    {
        // Normalize name to ensure consistent Unicode representation
        entity.Name = StringHelper.NormalizeAndTrim(updateInput.Name);
        entity.IsActive = updateInput.IsActive;
        return Task.CompletedTask;
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Import)]
    public async Task<ImportIndicatorGroupsResultDto> ImportFromExcelAsync(Stream fileStream, string fileName)
    {
        var result = new ImportIndicatorGroupsResultDto();

        // Disable soft delete filter to include deleted records (avoid duplicate name if there's a unique constraint)
        HashSet<string> existingNames;
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var existingQuery = await Repository.GetQueryableAsync();
            // Normalize existing names for consistent comparison
            existingNames = existingQuery.Select(g => StringHelper.NormalizeAndTrim(g.Name).ToLower()).ToHashSet();
        }

        var rows = MiniExcelLibs.MiniExcel.Query(fileStream, useHeaderRow: true).ToList();
        result.TotalRows = rows.Count;

        foreach (var (rowData, index) in rows.Select((value, index) => (value as IDictionary<string, object>, index)))
        {
            var rowNumber = index + 2;

            if (rowData == null)
            {
                AddImportError(result, rowNumber, "", "Không thể đọc dữ liệu dòng này");
                continue;
            }

            // Normalize name from Excel for consistent Unicode representation
            var name = rowData.ContainsKey("Tên nhóm chỉ tiêu")
                ? StringHelper.NormalizeAndTrim(rowData["Tên nhóm chỉ tiêu"]?.ToString())
                : null;

            if (string.IsNullOrWhiteSpace(name))
            {
                AddImportError(result, rowNumber, name ?? "", "Tên nhóm chỉ tiêu không được để trống");
                continue;
            }

            if (existingNames.Contains(name.ToLower()))
            {
                AddImportError(result, rowNumber, name, "Nhóm chỉ tiêu đã tồn tại");
                continue;
            }

            try
            {
                var entity = new IndicatorGroup(GuidGenerator.Create(), name) { IsActive = true };
                await Repository.InsertAsync(entity, autoSave: true);
                existingNames.Add(name.ToLower());
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                AddImportError(result, rowNumber, name, $"Lỗi khi tạo nhóm chỉ tiêu: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<byte[]> GetExcelTemplateAsync()
    {
        var groups = await Repository.GetListAsync();
        var templateData = groups
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .Select(g => new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", g.Name ?? "" }
            }).ToList();

        // Fallback: if no data, return template with sample rows
        if (templateData.Count == 0)
        {
            templateData = new List<Dictionary<string, object>>
            {
                new() { { "Tên nhóm chỉ tiêu", "Ví dụ: Vi sinh" } },
                new() { { "Tên nhóm chỉ tiêu", "Ví dụ: Hóa lý" } }
            };
        }

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return stream.ToArray();
    }

    private async Task EnrichDtosAsync(List<IndicatorGroupDto> dtos)
    {
        if (dtos.Count == 0) return;

        var groupIds = dtos.Select(d => d.Id).ToList();

        // Load indicator counts
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var counts = indicatorQuery
            .Where(x => groupIds.Contains(x.IndicatorGroupId))
            .GroupBy(x => x.IndicatorGroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.GroupId, x => x.Count);

        foreach (var dto in dtos)
        {
            dto.IndicatorCount = counts.GetValueOrDefault(dto.Id, 0);
        }
    }

    private static void AddImportError(ImportIndicatorGroupsResultDto result, int rowNumber, string groupName, string errorMessage)
    {
        result.ErrorCount++;
        result.Errors.Add(new ImportIndicatorGroupErrorDto
        {
            RowNumber = rowNumber,
            GroupName = groupName,
            ErrorMessage = errorMessage
        });
    }
}
