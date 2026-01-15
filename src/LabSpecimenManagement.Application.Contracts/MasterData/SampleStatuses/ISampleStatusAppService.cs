using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.SampleConditions;

/// <summary>
/// Interface cho SampleCondition AppService
/// </summary>
public interface ISampleConditionAppService : ICrudAppService<
    SampleConditionDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateSampleConditionDto>
{
    /// <summary>
    /// Lấy danh sách tình trạng mẫu đang hoạt động
    /// </summary>
    Task<ListResultDto<SampleConditionDto>> GetActiveAsync();

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của tình trạng mẫu
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}
