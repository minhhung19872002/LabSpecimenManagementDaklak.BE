using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// Interface cho service quản lý mẫu lưu kho
/// </summary>
public interface ISampleStorageAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách HĐ có mẫu (status >= SampleSent)
    /// </summary>
    Task<PagedResultDto<SampleStorageDto>> GetListAsync(GetSampleStorageListDto input);

    /// <summary>
    /// Lấy chi tiết HĐ + danh sách mẫu
    /// </summary>
    Task<SampleStorageDetailDto> GetAsync(Guid id);

    /// <summary>
    /// Cập nhật thông tin mẫu
    /// </summary>
    Task UpdateSampleAsync(Guid sampleId, UpdateSampleDto input);

    /// <summary>
    /// Hủy 1 mẫu
    /// </summary>
    Task CancelSampleAsync(Guid sampleId);

    /// <summary>
    /// Hủy tất cả mẫu trong HĐ
    /// </summary>
    Task CancelAllSamplesAsync(Guid testRequestId);
}
