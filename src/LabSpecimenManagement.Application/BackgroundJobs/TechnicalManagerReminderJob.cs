using System;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job để gửi thông báo nhắc nhở cho QLKT:
/// - Case 3: Pending work assignment reminder (chưa phân công >= 2 ngày) - chạy lúc 8h sáng
/// </summary>
public class TechnicalManagerReminderJob : ITransientDependency
{
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly INotificationCreator _notificationCreator;
    private readonly ILogger<TechnicalManagerReminderJob> _logger;

    public TechnicalManagerReminderJob(
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        INotificationCreator notificationCreator,
        ILogger<TechnicalManagerReminderJob> logger)
    {
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _testRequestRepository = testRequestRepository;
        _notificationCreator = notificationCreator;
        _logger = logger;
    }

    /// <summary>
    /// Case 3 QLKT: Gửi thông báo nhắc nhở chưa phân công công việc >= 2 ngày
    /// Chạy lúc 8h sáng hàng ngày
    /// </summary>
    [UnitOfWork]
    public virtual async Task SendPendingWorkAssignmentRemindersAsync()
    {
        _logger.LogInformation("TechnicalManagerReminderJob: Starting pending work assignment reminder check...");

        var now = DateTime.Now.Date;
        var threshold = now.AddDays(-2); // Đã gửi từ 2 ngày trước trở về trước

        try
        {
            // Lấy các LaboratoryTask có status = NotAssigned và TransferDate >= 2 ngày trước
            var pendingTasks = await _laboratoryTaskRepository.GetListAsync(
                t => t.Status == LaboratoryTaskStatus.UnderEvaluation &&
                     t.TechnicalManagerId.HasValue &&
                     t.TransferDate.HasValue &&
                     t.TransferDate.Value.Date <= threshold);

            _logger.LogInformation("Found {Count} pending tasks that need work assignment reminder", pendingTasks.Count);

            foreach (var task in pendingTasks)
            {
                try
                {
                    // Đếm số chỉ tiêu chưa phân công trong task này
                    var pendingIndicatorCount = await GetPendingIndicatorCountAsync(task.Id);

                    if (pendingIndicatorCount > 0)
                    {
                        await _notificationCreator.NotifyPendingWorkAssignmentAsync(
                            task.TechnicalManagerId!.Value,
                            task.ContractNumber ?? "",
                            task.TestRequestId,
                            pendingIndicatorCount,
                            task.TransferDate!.Value
                        );

                        _logger.LogInformation(
                            "Sent pending work assignment reminder for contract {ContractNumber} to QLKT {TechnicalManagerId}. Pending indicators: {Count}",
                            task.ContractNumber,
                            task.TechnicalManagerId,
                            pendingIndicatorCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending pending work assignment reminder for task {TaskId}", task.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TechnicalManagerReminderJob: Error during pending work assignment reminder check");
            throw;
        }

        _logger.LogInformation("TechnicalManagerReminderJob: Completed pending work assignment reminder check");
    }

    /// <summary>
    /// Đếm số chỉ tiêu chưa được phân công trong LaboratoryTask
    /// Chỉ tiêu chưa phân công = chưa có TechnicianId hoặc chưa có MethodId
    /// </summary>
    private async Task<int> GetPendingIndicatorCountAsync(Guid laboratoryTaskId)
    {
        // Get task with indicators
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);

        // Count indicators that are not assigned (no technician or no method)
        var pendingCount = 0;
        foreach (var indicator in task.Indicators)
        {
            // Indicator is pending if no technician assigned or no method assigned
            if (!indicator.TechnicianId.HasValue || !indicator.MethodId.HasValue)
            {
                pendingCount++;
            }
        }

        return pendingCount;
    }
}
