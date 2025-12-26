using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.IndicatorGroups.Default)]
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

    public IndicatorGroupAppService(
        IRepository<IndicatorGroup, Guid> repository,
        IRepository<Indicator, Guid> indicatorRepository)
        : base(repository)
    {
        _indicatorRepository = indicatorRepository;

        GetPolicyName = LabSpecimenManagementPermissions.IndicatorGroups.Default;
        GetListPolicyName = LabSpecimenManagementPermissions.IndicatorGroups.Default;
        CreatePolicyName = LabSpecimenManagementPermissions.IndicatorGroups.Create;
        UpdatePolicyName = LabSpecimenManagementPermissions.IndicatorGroups.Edit;
        DeletePolicyName = LabSpecimenManagementPermissions.IndicatorGroups.Delete;
    }

    public async Task<ListResultDto<IndicatorGroupDto>> GetActiveGroupsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<IndicatorGroup>, List<IndicatorGroupDto>>(items);

        // Get indicator counts
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        foreach (var dto in dtos)
        {
            dto.IndicatorCount = indicatorQuery.Count(x => x.IndicatorGroupId == dto.Id);
        }

        return new ListResultDto<IndicatorGroupDto>(dtos);
    }

    public async Task<ListResultDto<IndicatorGroupDto>> GetGroupsWithCountAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<IndicatorGroup>, List<IndicatorGroupDto>>(items);

        // Get indicator counts
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        foreach (var dto in dtos)
        {
            dto.IndicatorCount = indicatorQuery.Count(x => x.IndicatorGroupId == dto.Id);
        }

        return new ListResultDto<IndicatorGroupDto>(dtos);
    }

    public override async Task<PagedResultDto<IndicatorGroupDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.Name)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = ObjectMapper.Map<List<IndicatorGroup>, List<IndicatorGroupDto>>(items);

        // Get indicator counts
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        foreach (var dto in dtos)
        {
            dto.IndicatorCount = indicatorQuery.Count(x => x.IndicatorGroupId == dto.Id);
        }

        return new PagedResultDto<IndicatorGroupDto>(totalCount, dtos);
    }

    protected override async Task<IndicatorGroup> MapToEntityAsync(CreateUpdateIndicatorGroupDto createInput)
    {
        return new IndicatorGroup(GuidGenerator.Create(), createInput.Name)
        {
            IsActive = createInput.IsActive
        };
    }

    protected override async Task MapToEntityAsync(CreateUpdateIndicatorGroupDto updateInput, IndicatorGroup entity)
    {
        entity.Name = updateInput.Name;
        entity.IsActive = updateInput.IsActive;
    }

    [Authorize(LabSpecimenManagementPermissions.IndicatorGroups.Import)]
    public async Task<ImportIndicatorGroupsResultDto> ImportFromExcelAsync(Stream fileStream, string fileName)
    {
        var result = new ImportIndicatorGroupsResultDto();

        // Get existing groups for duplicate check
        var existingQuery = await Repository.GetQueryableAsync();
        var existingNames = existingQuery.Select(g => g.Name.Trim().ToLower()).ToHashSet();

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
                result.Errors.Add(new ImportIndicatorGroupErrorDto
                {
                    RowNumber = rowNumber,
                    GroupName = "",
                    ErrorMessage = "Không thể đọc dữ liệu dòng này"
                });
                continue;
            }

            // Get name from row
            var name = rowData.ContainsKey("Tên nhóm chỉ tiêu") ? rowData["Tên nhóm chỉ tiêu"]?.ToString()?.Trim() : null;

            // Validate name
            if (string.IsNullOrWhiteSpace(name))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorGroupErrorDto
                {
                    RowNumber = rowNumber,
                    GroupName = name ?? "",
                    ErrorMessage = "Tên nhóm chỉ tiêu không được để trống"
                });
                continue;
            }

            // Check duplicate
            if (existingNames.Contains(name.ToLower()))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorGroupErrorDto
                {
                    RowNumber = rowNumber,
                    GroupName = name,
                    ErrorMessage = "Nhóm chỉ tiêu đã tồn tại"
                });
                continue;
            }

            // Create indicator group
            try
            {
                var entity = new IndicatorGroup(GuidGenerator.Create(), name)
                {
                    IsActive = true
                };

                await Repository.InsertAsync(entity, autoSave: true);
                existingNames.Add(name.ToLower()); // Add to set to prevent duplicates within same import
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportIndicatorGroupErrorDto
                {
                    RowNumber = rowNumber,
                    GroupName = name,
                    ErrorMessage = $"Lỗi khi tạo nhóm chỉ tiêu: {ex.Message}"
                });
            }
        }

        return result;
    }

    public Task<byte[]> GetExcelTemplateAsync()
    {
        // Create template data with sample rows
        var templateData = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "Ví dụ: Vi sinh" }
            },
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "Ví dụ: Hóa lý" }
            },
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "" }
            },
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "=== HƯỚNG DẪN ===" }
            },
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "- Mỗi dòng là một nhóm chỉ tiêu" }
            },
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "- Tên nhóm chỉ tiêu không được để trống" }
            },
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "- Tên nhóm chỉ tiêu không được trùng" }
            },
            new Dictionary<string, object>
            {
                { "Tên nhóm chỉ tiêu", "- Xóa các dòng hướng dẫn này trước khi import" }
            }
        };

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return Task.FromResult(stream.ToArray());
    }
}
