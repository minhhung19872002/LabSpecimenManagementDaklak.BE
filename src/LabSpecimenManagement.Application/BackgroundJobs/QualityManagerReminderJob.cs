using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job để gửi thông báo nhắc nhở cho QLCL:
/// - Case 3: Deadline reminder (deadline <= 2 ngày) - chạy lúc 8h sáng
/// - Case 4: Handover reminder (đã duyệt >= 1 ngày) - chạy lúc 9h sáng
/// </summary>
public class QualityManagerReminderJob : ITransientDependency
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<QualityManagementTask, Guid> _qualityTaskRepository;
    private readonly INotificationCreator _notificationCreator;
    private readonly ILogger<QualityManagerReminderJob> _logger;

    public QualityManagerReminderJob(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<QualityManagementTask, Guid> qualityTaskRepository,
        INotificationCreator notificationCreator,
        ILogger<QualityManagerReminderJob> logger)
    {
        _testRequestRepository = testRequestRepository;
        _qualityTaskRepository = qualityTaskRepository;
        _notificationCreator = notificationCreator;
        _logger = logger;
    }

    /// <summary>
    /// Case 3 QLCL: Gửi thông báo nhắc nhở deadline trả kết quả <= 2 ngày
    /// Chạy lúc 8h sáng hàng ngày
    /// </summary>
    [UnitOfWork]
    public virtual async Task SendDeadlineRemindersAsync()
    {
        _logger.LogInformation("QualityManagerReminderJob: Starting deadline reminder check...");

        var now = DateTime.Now.Date;
        var deadlineThreshold = now.AddDays(2);

        try
        {
            // Lấy các QualityManagementTask đang chờ duyệt và có AssignedQualityManagerId
            var pendingTasks = await _qualityTaskRepository.GetListAsync(
                t => t.Status == QualityManagementStatus.PendingApproval &&
                     t.AssignedQualityManagerId.HasValue);

            _logger.LogInformation("Found {Count} pending tasks to check for deadline reminders", pendingTasks.Count);

            foreach (var task in pendingTasks)
            {
                try
                {
                    var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);
                    if (testRequest == null) continue;

                    // Kiểm tra ResultDeliveryDate (ngày yêu cầu trả kết quả)
                    if (testRequest.ResultDeliveryDate.HasValue)
                    {
                        var deadline = testRequest.ResultDeliveryDate.Value.Date;
                        var daysRemaining = (deadline - now).Days;

                        // Gửi thông báo nếu deadline <= 2 ngày
                        if (daysRemaining <= 2)
                        {
                            await _notificationCreator.NotifyDeadlineReminderAsync(
                                task.AssignedQualityManagerId!.Value,
                                task.ContractNumber,
                                task.TestRequestId,
                                testRequest.ResultDeliveryDate.Value
                            );

                            _logger.LogInformation(
                                "Sent deadline reminder for contract {ContractNumber} to QLCL {QualityManagerId}. Days remaining: {DaysRemaining}",
                                task.ContractNumber,
                                task.AssignedQualityManagerId,
                                daysRemaining);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending deadline reminder for task {TaskId}", task.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QualityManagerReminderJob: Error during deadline reminder check");
            throw;
        }

        _logger.LogInformation("QualityManagerReminderJob: Completed deadline reminder check");
    }

    /// <summary>
    /// Case 4 QLCL: Gửi thông báo nhắc nhở bàn giao kết quả (sau khi duyệt >= 1 ngày)
    /// Chạy lúc 9h sáng hàng ngày
    /// </summary>
    [UnitOfWork]
    public virtual async Task SendHandoverRemindersAsync()
    {
        _logger.LogInformation("QualityManagerReminderJob: Starting handover reminder check...");

        var now = DateTime.Now.Date;
        var approvedThreshold = now.AddDays(-1); // Đã duyệt từ hôm qua trở về trước

        try
        {
            // Lấy các TestRequest đã được duyệt (status = Approved) và chưa gửi kết quả
            var approvedTestRequests = await _testRequestRepository.GetListAsync(
                tr => tr.Status == TestRequestStatus.Approved &&
                      tr.ApprovedDate.HasValue &&
                      tr.ApprovedDate.Value.Date <= approvedThreshold);

            _logger.LogInformation("Found {Count} approved test requests to check for handover reminders", approvedTestRequests.Count);

            foreach (var testRequest in approvedTestRequests)
            {
                try
                {
                    // Tìm QualityManagementTask tương ứng để lấy AssignedQualityManagerId
                    var qualityTask = (await _qualityTaskRepository.GetListAsync(
                        t => t.TestRequestId == testRequest.Id &&
                             t.TaskType == QualityTaskType.ResultApproval &&
                             t.AssignedQualityManagerId.HasValue))
                        .FirstOrDefault();

                    if (qualityTask == null) continue;

                    await _notificationCreator.NotifyHandoverReminderAsync(
                        qualityTask.AssignedQualityManagerId!.Value,
                        testRequest.ContractNumber ?? "",
                        testRequest.Id,
                        testRequest.ApprovedDate!.Value
                    );

                    _logger.LogInformation(
                        "Sent handover reminder for contract {ContractNumber} to QLCL {QualityManagerId}. Approved date: {ApprovedDate}",
                        testRequest.ContractNumber,
                        qualityTask.AssignedQualityManagerId,
                        testRequest.ApprovedDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending handover reminder for TestRequest {TestRequestId}", testRequest.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QualityManagerReminderJob: Error during handover reminder check");
            throw;
        }

        _logger.LogInformation("QualityManagerReminderJob: Completed handover reminder check");
    }
}
