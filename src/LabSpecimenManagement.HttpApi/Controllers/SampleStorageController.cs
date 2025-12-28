using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.SampleStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// API Controller cho quản lý mẫu lưu kho
/// </summary>
[RemoteService(Name = "Default")]
[Route("api/v1/sample-storage")]
[Authorize(LabSpecimenManagementPermissions.Specimens.Default)]
public class SampleStorageController : LabSpecimenManagementController
{
    private readonly ISampleStorageAppService _sampleStorageAppService;

    public SampleStorageController(ISampleStorageAppService sampleStorageAppService)
    {
        _sampleStorageAppService = sampleStorageAppService;
    }

    /// <summary>
    /// Lấy danh sách HĐ có mẫu
    /// </summary>
    [HttpGet]
    public virtual async Task<PagedResultDto<SampleStorageDto>> GetListAsync([FromQuery] GetSampleStorageListDto input)
    {
        return await _sampleStorageAppService.GetListAsync(input);
    }

    /// <summary>
    /// Lấy chi tiết HĐ + danh sách mẫu
    /// </summary>
    [HttpGet("{id}")]
    public virtual async Task<SampleStorageDetailDto> GetAsync(Guid id)
    {
        return await _sampleStorageAppService.GetAsync(id);
    }

    /// <summary>
    /// Cập nhật thông tin mẫu
    /// </summary>
    [HttpPut("samples/{sampleId}")]
    [Authorize(LabSpecimenManagementPermissions.Specimens.Edit)]
    public virtual async Task UpdateSampleAsync(Guid sampleId, [FromBody] UpdateSampleDto input)
    {
        await _sampleStorageAppService.UpdateSampleAsync(sampleId, input);
    }

    /// <summary>
    /// Hủy 1 mẫu
    /// </summary>
    [HttpPost("samples/{sampleId}/cancel")]
    [Authorize(LabSpecimenManagementPermissions.Specimens.Edit)]
    public virtual async Task CancelSampleAsync(Guid sampleId)
    {
        await _sampleStorageAppService.CancelSampleAsync(sampleId);
    }

    /// <summary>
    /// Hủy tất cả mẫu trong HĐ
    /// </summary>
    [HttpPost("{testRequestId}/cancel-all")]
    [Authorize(LabSpecimenManagementPermissions.Specimens.Edit)]
    public virtual async Task CancelAllSamplesAsync(Guid testRequestId)
    {
        await _sampleStorageAppService.CancelAllSamplesAsync(testRequestId);
    }
}
