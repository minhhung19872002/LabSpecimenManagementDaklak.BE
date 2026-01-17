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
    /// Điều kiện: Task.Status = Approved, IsSharedSampleTask = true, SampleSentToNextLab = false
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> SendSharedSampleAsync(Guid id, SendSharedSampleDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);

        // Validate conditions
        if (!task.IsSharedSampleTask)
        {
            throw new InvalidOpException("Task này không phải là task của mẫu chung", "isSharedSampleTask");
        }

        if (task.Status != LaboratoryTaskStatus.Approved && task.Status != LaboratoryTaskStatus.SentToQuality)
        {
            throw new InvalidOpException("Chỉ có thể gửi mẫu khi task đã được duyệt (Approved) hoặc đã gửi QLCL (SentToQuality)", "status");
        }

        if (task.SampleSentToNextLab)
        {
            throw new InvalidOpException("Mẫu đã được gửi cho phòng lab khác", "sampleSentToNextLab");
        }

        // Check if this is the microbiology lab (priority lab)
        var laboratory = await _laboratoryRepository.GetAsync(task.LaboratoryId);
        var isMicrobiologyLab = laboratory.Code?.Equals("LVS", StringComparison.OrdinalIgnoreCase) == true ||
                                laboratory.Name?.Contains("Vi sinh", StringComparison.OrdinalIgnoreCase) == true ||
                                laboratory.Name?.Contains("Vi Sinh", StringComparison.OrdinalIgnoreCase) == true;

        if (!isMicrobiologyLab)
        {
            throw new InvalidOpException("Chỉ Lab Vi Sinh mới có thể gửi mẫu chung", "laboratory");
        }

        // Update task
        task.SampleSentToNextLab = true;
        task.SampleSentDate = Clock.Now;

        await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);

        // Find waiting tasks (Lab Hóa) that have PriorLabTaskId = this task
        var waitingTasks = await _laboratoryTaskRepository.GetListAsync(
            t => t.PriorLabTaskId == task.Id && t.IsWaitingForPriorLab);

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
    /// Điều kiện: IsWaitingForPriorLab = true, PriorLab đã gửi mẫu
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> ReceiveSharedSampleAsync(Guid id, ReceiveSharedSampleDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);

        // Validate conditions
        if (!task.IsSharedSampleTask)
        {
            throw new InvalidOpException("Task này không phải là task của mẫu chung", "isSharedSampleTask");
        }

        if (!task.IsWaitingForPriorLab)
        {
            throw new InvalidOpException("Task này không đang chờ nhận mẫu từ lab khác", "isWaitingForPriorLab");
        }

        // Check if prior lab has sent the sample
        if (task.PriorLabTaskId.HasValue)
        {
            var priorTask = await _laboratoryTaskRepository.FindAsync(task.PriorLabTaskId.Value);
            if (priorTask == null || !priorTask.SampleSentToNextLab)
            {
                throw new InvalidOpException("Lab Vi Sinh chưa gửi mẫu. Vui lòng chờ Lab Vi Sinh hoàn thành và gửi mẫu.", "priorLabNotSent");
            }
        }

        // Update task
        task.IsWaitingForPriorLab = false;
        task.SampleReceivedDate = input.ReceivedDate ?? Clock.Now;
        task.SampleReceivedNote = input.Note;
        task.Status = LaboratoryTaskStatus.UnderEvaluation; // Now QLKT can evaluate

        await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);

        return await MapToDetailDtoAsync(task);
    }

    /// <summary>
    /// Kiểm tra xem task có thể thực hiện action đánh giá hay không
    /// Nếu IsWaitingForPriorLab = true, không cho phép thực hiện action
    /// </summary>
    private void ValidateSharedSampleAction(LaboratoryTask task, string actionName)
    {
        if (task.IsWaitingForPriorLab)
        {
            throw new InvalidOpException(
                $"Không thể {actionName} khi đang chờ nhận mẫu từ Lab Vi Sinh. Vui lòng xác nhận nhận mẫu trước.",
                "isWaitingForPriorLab");
        }
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

        // Nếu là Lab Vi Sinh (priority lab), không ẩn
        var laboratory = await _laboratoryRepository.GetAsync(task.LaboratoryId);
        var isMicrobiologyLab = laboratory.Code?.Equals("LVS", StringComparison.OrdinalIgnoreCase) == true ||
                                laboratory.Name?.Contains("Vi sinh", StringComparison.OrdinalIgnoreCase) == true ||
                                laboratory.Name?.Contains("Vi Sinh", StringComparison.OrdinalIgnoreCase) == true;

        if (isMicrobiologyLab)
        {
            return false;
        }

        // Lab khác (Hóa) trong shared sample task -> ẩn nút từ chối
        return true;
    }
}
