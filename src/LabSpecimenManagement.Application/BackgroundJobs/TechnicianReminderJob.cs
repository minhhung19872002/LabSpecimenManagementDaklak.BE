using System;
using System.Collections.Generic;
using System.Linq;
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
/// Hangfire job để gửi thông báo nhắc nhở cho KTV (Thử nghiệm viên):
/// - Case 3: Pending indicator reminder (chỉ tiêu được gán >= 2 ngày nhưng chưa thực hiện)
/// - Chạy lúc 8h sáng hàng ngày
/// - Gửi riêng biệt cho từng hợp đồng
/// </summary>
public class TechnicianReminderJob : ITransientDependency
{
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly INotificationCreator _notificationCreator;
    private readonly ILogger<TechnicianReminderJob> _logger;

    public TechnicianReminderJob(
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        INotificationCreator notificationCreator,
        ILogger<TechnicianReminderJob> logger)
    {
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _testRequestRepository = testRequestRepository;
        _notificationCreator = notificationCreator;
        _logger = logger;
    }

    /// <summary>
    /// Case 3 KTV: Gửi thông báo nhắc nhở chỉ tiêu chưa thực hiện >= 2 ngày
    /// - Điều kiện: AssignedDate >= 2 ngày trước VÀ ApprovalStatus = Pending (chưa nhập kết quả)
    /// - Spam hàng ngày cho đến khi tất cả chỉ tiêu không còn trạng thái "Cần làm"
    /// - Gửi riêng biệt cho từng hợp đồng
    /// </summary>
    [UnitOfWork]
    public virtual async Task SendPendingIndicatorRemindersAsync()
    {
        _logger.LogInformation("TechnicianReminderJob: Starting pending indicator reminder check...");

        var now = DateTime.Now.Date;
        var threshold = now.AddDays(-2); // Đã gán từ 2 ngày trước trở về trước

        try
        {
            // Lấy tất cả LaboratoryTaskIndicator:
            // - Đã được gán KTV (TechnicianId != null)
            // - Đã gán >= 2 ngày (AssignedDate <= threshold)
            // - Chưa nhập kết quả (ApprovalStatus = Pending)
            var allIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.TechnicianId.HasValue &&
                      ti.AssignedDate.HasValue &&
                      ti.AssignedDate.Value.Date <= threshold &&
                      ti.ApprovalStatus == IndicatorApprovalStatus.Pending);

            _logger.LogInformation("Found {Count} pending indicators that need reminder (assigned >= 2 days ago)", allIndicators.Count);

            if (!allIndicators.Any())
            {
                _logger.LogInformation("TechnicianReminderJob: No pending indicators found, skipping...");
                return;
            }

            // Group by TechnicianId and TestRequestId (gửi riêng biệt cho từng hợp đồng)
            var indicatorsByTechnicianAndContract = allIndicators
                .GroupBy(ti => new { ti.TechnicianId!.Value, ti.LaboratoryTaskId })
                .ToList();

            // Get all LaboratoryTasks to map to TestRequestId
            var taskIds = indicatorsByTechnicianAndContract.Select(g => g.Key.LaboratoryTaskId).Distinct().ToList();
            var tasks = await _laboratoryTaskRepository.GetListAsync(t => taskIds.Contains(t.Id));
            var taskDict = tasks.ToDictionary(t => t.Id);

            // Regroup by TechnicianId and TestRequestId
            var groupedNotifications = new Dictionary<(Guid TechnicianId, Guid TestRequestId), List<LaboratoryTaskIndicator>>();

            foreach (var group in indicatorsByTechnicianAndContract)
            {
                if (!taskDict.TryGetValue(group.Key.LaboratoryTaskId, out var task))
                    continue;

                var key = (group.Key.Value, task.TestRequestId);
                if (!groupedNotifications.ContainsKey(key))
                {
                    groupedNotifications[key] = new List<LaboratoryTaskIndicator>();
                }
                groupedNotifications[key].AddRange(group);
            }

            // Send notifications
            foreach (var kvp in groupedNotifications)
            {
                var (technicianId, testRequestId) = kvp.Key;
                var pendingIndicators = kvp.Value;
                var pendingCount = pendingIndicators.Count;

                try
                {
                    var testRequest = await _testRequestRepository.FindAsync(testRequestId);
                    var contractNumber = testRequest?.ContractNumber ?? "";

                    await _notificationCreator.NotifyPendingIndicatorReminderAsync(
                        technicianId,
                        contractNumber,
                        testRequestId,
                        pendingCount);

                    _logger.LogInformation(
                        "Sent pending indicator reminder for contract {ContractNumber} to KTV {TechnicianId}. Pending count: {Count}",
                        contractNumber,
                        technicianId,
                        pendingCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sending pending indicator reminder for TestRequest {TestRequestId} to KTV {TechnicianId}",
                        testRequestId, technicianId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TechnicianReminderJob: Error during pending indicator reminder check");
            throw;
        }

        _logger.LogInformation("TechnicianReminderJob: Completed pending indicator reminder check");
    }
}
