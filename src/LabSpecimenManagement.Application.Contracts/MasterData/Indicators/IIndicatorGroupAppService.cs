using System;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.Indicators;

/// <summary>
/// Interface cho IndicatorGroup AppService
/// </summary>
public interface IIndicatorGroupAppService : ICrudAppService<
    IndicatorGroupDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateIndicatorGroupDto>
{
    /// <summary>
    /// Lấy danh sách nhóm chỉ tiêu đang hoạt động
    /// </summary>
    Task<ListResultDto<IndicatorGroupDto>> GetActiveGroupsAsync();

    /// <summary>
    /// Lấy danh sách nhóm chỉ tiêu kèm số lượng chỉ tiêu
    /// </summary>
    Task<ListResultDto<IndicatorGroupDto>> GetGroupsWithCountAsync();

    /// <summary>
    /// Import nhóm chỉ tiêu từ file Excel
    /// </summary>
    Task<ImportIndicatorGroupsResultDto> ImportFromExcelAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Tải template Excel để import nhóm chỉ tiêu
    /// </summary>
    Task<byte[]> GetExcelTemplateAsync();
}
