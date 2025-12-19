using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/laboratory-tasks")]
[Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
public class LaboratoryTaskController : LabSpecimenManagementController
{
    private readonly ILaboratoryTaskAppService _laboratoryTaskAppService;

    public LaboratoryTaskController(ILaboratoryTaskAppService laboratoryTaskAppService)
    {
        _laboratoryTaskAppService = laboratoryTaskAppService;
    }

    /// <summary>
    /// Get filtered and paged list of laboratory tasks
    /// </summary>
    [HttpGet]
    public virtual async Task<PagedResultDto<LaboratoryTaskDto>> GetListAsync([FromQuery] GetLaboratoryTaskListDto input)
    {
        return await _laboratoryTaskAppService.GetFilteredListAsync(input);
    }

    /// <summary>
    /// Get a single laboratory task by ID
    /// </summary>
    [HttpGet("{id}")]
    public virtual Task<LaboratoryTaskDto> GetAsync(Guid id)
    {
        return _laboratoryTaskAppService.GetAsync(id);
    }

    /// <summary>
    /// Update laboratory task (TestDate, Status, Notes)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual Task<LaboratoryTaskDto> UpdateAsync(Guid id, [FromBody] UpdateLaboratoryTaskDto input)
    {
        return _laboratoryTaskAppService.UpdateAsync(id, input);
    }

    /// <summary>
    /// Update laboratory task status (workflow)
    /// </summary>
    [HttpPost("{id}/status")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.UpdateStatus)]
    public virtual Task<LaboratoryTaskDto> UpdateStatusAsync(Guid id, [FromBody] LaboratoryTaskStatus newStatus)
    {
        return _laboratoryTaskAppService.UpdateStatusAsync(id, newStatus);
    }

    /// <summary>
    /// Set test date for laboratory task
    /// </summary>
    [HttpPost("{id}/test-date")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual Task<LaboratoryTaskDto> SetTestDateAsync(Guid id, [FromBody] DateTime testDate)
    {
        return _laboratoryTaskAppService.SetTestDateAsync(id, testDate);
    }

    /// <summary>
    /// Get tasks for current user (if user is Technical Manager of any laboratory)
    /// </summary>
    [HttpGet("my-tasks")]
    public virtual Task<ListResultDto<LaboratoryTaskDto>> GetMyLaboratoryTasksAsync()
    {
        return _laboratoryTaskAppService.GetMyLaboratoryTasksAsync();
    }

    /// <summary>
    /// Get tasks by laboratory ID
    /// </summary>
    [HttpGet("by-laboratory/{laboratoryId}")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll)]
    public virtual Task<ListResultDto<LaboratoryTaskDto>> GetByLaboratoryAsync(Guid laboratoryId)
    {
        return _laboratoryTaskAppService.GetByLaboratoryAsync(laboratoryId);
    }

    /// <summary>
    /// Delete laboratory task
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _laboratoryTaskAppService.DeleteAsync(id);
    }

    #region Phase 6 - Workflow Endpoints

    /// <summary>
    /// Gán phương pháp và kỹ thuật viên cho các chỉ tiêu của một mẫu
    /// </summary>
    [HttpPost("{taskId}/samples/{sampleId}/assign")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual Task<LaboratoryTaskDto> AssignSampleIndicatorsAsync(
        Guid taskId,
        Guid sampleId,
        [FromBody] List<AssignIndicatorDto> assignments)
    {
        return _laboratoryTaskAppService.AssignSampleIndicatorsAsync(taskId, sampleId, assignments);
    }

    /// <summary>
    /// Cập nhật kết quả cho một chỉ tiêu
    /// </summary>
    [HttpPut("indicators/{indicatorId}/result")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual Task<LaboratoryTaskIndicatorDto> UpdateIndicatorResultAsync(
        Guid indicatorId,
        [FromBody] UpdateIndicatorResultDto input)
    {
        return _laboratoryTaskAppService.UpdateIndicatorResultAsync(indicatorId, input);
    }

    /// <summary>
    /// Phê duyệt hoặc từ chối kết quả chỉ tiêu
    /// </summary>
    [HttpPost("indicators/{indicatorId}/approve")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual Task<LaboratoryTaskIndicatorDto> ApproveIndicatorAsync(
        Guid indicatorId,
        [FromBody] ApproveIndicatorDto input)
    {
        return _laboratoryTaskAppService.ApproveIndicatorAsync(indicatorId, input);
    }

    /// <summary>
    /// Gửi kết quả (chuyển status sang ResultSent)
    /// </summary>
    [HttpPost("{taskId}/send-results")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId)
    {
        return _laboratoryTaskAppService.SendResultsAsync(taskId);
    }

    /// <summary>
    /// Lấy danh sách phương pháp theo chỉ tiêu
    /// </summary>
    [HttpGet("methods-lookup")]
    public virtual Task<ListResultDto<MethodLookupDto>> GetMethodsByIndicatorAsync([FromQuery] Guid indicatorId)
    {
        return _laboratoryTaskAppService.GetMethodsByIndicatorAsync(indicatorId);
    }

    /// <summary>
    /// Lấy danh sách kỹ thuật viên theo khoa phòng
    /// </summary>
    [HttpGet("technicians-lookup")]
    public virtual Task<ListResultDto<UserLookupDto>> GetTechniciansByLaboratoryAsync([FromQuery] Guid laboratoryId)
    {
        return _laboratoryTaskAppService.GetTechniciansByLaboratoryAsync(laboratoryId);
    }

    #endregion
}
