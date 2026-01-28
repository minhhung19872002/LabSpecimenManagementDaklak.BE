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
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;

    public IndicatorGroupAppService(
        IRepository<IndicatorGroup, Guid> repository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Department, Guid> departmentRepository)
        : base(repository)
    {
        _indicatorRepository = indicatorRepository;
        _laboratoryRepository = laboratoryRepository;
        _departmentRepository = departmentRepository;

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
        return Task.FromResult(new IndicatorGroup(GuidGenerator.Create(), createInput.Name, createInput.LaboratoryId)
        {
            IsActive = createInput.IsActive
        });
    }

    protected override Task MapToEntityAsync(CreateUpdateIndicatorGroupDto updateInput, IndicatorGroup entity)
    {
        entity.Name = updateInput.Name;
        entity.IsActive = updateInput.IsActive;
        entity.LaboratoryId = updateInput.LaboratoryId;
        return Task.CompletedTask;
    }

    [Authorize(LabSpecimenManagementPermissions.MasterData.Import)]
    public async Task<ImportIndicatorGroupsResultDto> ImportFromExcelAsync(Stream fileStream, string fileName)
    {
        var result = new ImportIndicatorGroupsResultDto();

        var existingQuery = await Repository.GetQueryableAsync();
        var existingNames = existingQuery.Select(g => g.Name.Trim().ToLower()).ToHashSet();

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

            var name = rowData.ContainsKey("Tên nhóm chỉ tiêu") ? rowData["Tên nhóm chỉ tiêu"]?.ToString()?.Trim() : null;

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

    public Task<byte[]> GetExcelTemplateAsync()
    {
        var templateData = new List<Dictionary<string, object>>
        {
            new() { { "Tên nhóm chỉ tiêu", "Ví dụ: Vi sinh" } },
            new() { { "Tên nhóm chỉ tiêu", "Ví dụ: Hóa lý" } },
            new() { { "Tên nhóm chỉ tiêu", "" } },
            new() { { "Tên nhóm chỉ tiêu", "=== HƯỚNG DẪN ===" } },
            new() { { "Tên nhóm chỉ tiêu", "- Mỗi dòng là một nhóm chỉ tiêu" } },
            new() { { "Tên nhóm chỉ tiêu", "- Tên nhóm chỉ tiêu không được để trống" } },
            new() { { "Tên nhóm chỉ tiêu", "- Tên nhóm chỉ tiêu không được trùng" } },
            new() { { "Tên nhóm chỉ tiêu", "- Xóa các dòng hướng dẫn này trước khi import" } }
        };

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return Task.FromResult(stream.ToArray());
    }

    /// <summary>
    /// Enriches DTOs with indicator counts and laboratory names (batch operation to avoid N+1)
    /// </summary>
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

        // Load laboratory names
        var laboratoryIds = dtos
            .Where(d => d.LaboratoryId.HasValue)
            .Select(d => d.LaboratoryId!.Value)
            .Distinct()
            .ToList();

        var laboratoryNames = new Dictionary<Guid, string>();
        if (laboratoryIds.Count > 0)
        {
            var labs = await _laboratoryRepository.GetListAsync(l => laboratoryIds.Contains(l.Id));
            var deptIds = labs.Select(l => l.DepartmentId).Distinct().ToList();
            var depts = await _departmentRepository.GetListAsync(d => deptIds.Contains(d.Id));
            var deptDict = depts.ToDictionary(d => d.Id, d => d.Name);
            laboratoryNames = labs.ToDictionary(l => l.Id, l => deptDict.TryGetValue(l.DepartmentId, out var name) ? name : "");
        }

        foreach (var dto in dtos)
        {
            dto.IndicatorCount = counts.GetValueOrDefault(dto.Id, 0);
            if (dto.LaboratoryId.HasValue && laboratoryNames.TryGetValue(dto.LaboratoryId.Value, out var name))
            {
                dto.LaboratoryName = name;
            }
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
