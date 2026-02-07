using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using InvalidOpException = LabSpecimenManagement.Common.Exceptions.InvalidOperationException;

namespace LabSpecimenManagement.LaboratoryTasks;

public partial class LaboratoryTaskAppService
{
    /// <summary>
    /// QLKT Vi Sinh gửi mẫu chung cho Lab Hóa
    /// Điều kiện: IsSharedSampleTask = true, tất cả chỉ tiêu đã được duyệt (Approved), SampleSentToNextLab = false
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskDto> SendSharedSampleAsync(Guid id, SendSharedSampleDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);

        // Validate conditions
        if (!task.IsSharedSampleTask)
        {
            throw new InvalidOpException("Task này không phải là task của mẫu chung", "isSharedSampleTask");
        }

        if (task.SampleSentToNextLab)
        {
            throw new InvalidOpException("Mẫu đã được gửi cho phòng lab khác", "sampleSentToNextLab");
        }

        // Check if this is the microbiology lab (priority lab)
        var laboratory = await _laboratoryRepository.GetAsync(task.LaboratoryId);
        var labDept = await _departmentRepository.GetAsync(laboratory.DepartmentId);
        var labName = labDept.Name;
        var isMicrobiologyLab = laboratory.Code?.Equals("LVS", StringComparison.OrdinalIgnoreCase) == true ||
                                labName.Contains("Vi sinh", StringComparison.OrdinalIgnoreCase) ||
                                labName.Contains("Vi Sinh", StringComparison.OrdinalIgnoreCase);

        if (!isMicrobiologyLab)
        {
            throw new InvalidOpException("Chỉ Lab Vi Sinh mới có thể gửi mẫu chung", "laboratory");
        }

        // Check if all indicators are approved (QLKT đã duyệt hết)
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            ti => ti.LaboratoryTaskId == task.Id);
        var allApproved = taskIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        if (!allApproved)
        {
            throw new InvalidOpException("Phải duyệt tất cả chỉ tiêu trước khi gửi mẫu chung", "indicatorsNotApproved");
        }

        // Update task
        task.SampleSentToNextLab = true;
        task.SampleSentDate = Clock.Now;

        await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);

        // Get sample code for notification
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequest.Id && s.IsSharedSample);
        var sampleCodes = string.Join(", ", samples.Select(s => s.SampleCode));

        // TODO: Add notification method for shared sample when INotificationCreator interface is updated
        // Notify QLKT of waiting labs that sample is ready to receive

        return await MapToDetailDtoAsync(task);
    }

    /// <summary>
    /// QLKT Lab Hóa xác nhận nhận mẫu chung từ Lab Vi Sinh
    /// Chỉ để ghi nhận ngày nhận mẫu - Lab Hóa đã có thể làm việc song song
    /// Điều kiện: IsSharedSampleTask = true, PriorLab đã gửi mẫu, chưa nhận mẫu
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskDto> ReceiveSharedSampleAsync(Guid id, ReceiveSharedSampleDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);

        // Validate conditions
        if (!task.IsSharedSampleTask)
        {
            throw new InvalidOpException("Task này không phải là task của mẫu chung", "isSharedSampleTask");
        }

        // Check if already received
        if (task.SampleReceivedDate.HasValue)
        {
            throw new InvalidOpException("Mẫu đã được xác nhận nhận trước đó", "alreadyReceived");
        }

        // Check if prior lab has sent the sample
        if (task.PriorLabTaskId.HasValue)
        {
            var priorTask = await _laboratoryTaskRepository.FindAsync(task.PriorLabTaskId.Value);
            if (priorTask == null || !priorTask.SampleSentToNextLab)
            {
                throw new InvalidOpException("Lab Vi Sinh chưa gửi mẫu. Vui lòng chờ Lab Vi Sinh gửi mẫu.", "priorLabNotSent");
            }
        }

        // Update task - chỉ ghi nhận ngày nhận mẫu
        task.SampleReceivedDate = input.ReceivedDate ?? Clock.Now;
        task.SampleReceivedNote = input.Note;

        await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);

        return await MapToDetailDtoAsync(task);
    }

    /// <summary>
    /// Kiểm tra xem có cần ẩn nút từ chối cho task này không
    /// Task của Lab Hóa trong mẫu chung không được từ chối chỉ tiêu
    /// </summary>
    public async Task<bool> ShouldHideRejectButtonAsync(Guid taskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Nếu không phải shared sample task, không ẩn
        if (!task.IsSharedSampleTask)
        {
            return false;
        }

        // Lab Vi Sinh (lab đầu tiên): PriorLabTaskId = null → hiện nút từ chối
        // Lab Hóa (lab thứ 2): PriorLabTaskId có giá trị → ẩn nút từ chối
        return task.PriorLabTaskId != null;
    }
}
