using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job để gửi thông báo nhắc nhở cho Bộ phận Giao nhận mẫu (SampleReceiver):
/// - Case 4: Sample return reminder (Status = ResultSent) - chạy lúc 8h sáng hàng ngày
/// </summary>
public class SampleReceiverReminderJob : ITransientDependency
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly INotificationCreator _notificationCreator;
    private readonly ILogger<SampleReceiverReminderJob> _logger;

    public SampleReceiverReminderJob(
        IRepository<TestRequest, Guid> testRequestRepository,
        INotificationCreator notificationCreator,
        ILogger<SampleReceiverReminderJob> logger)
    {
        _testRequestRepository = testRequestRepository;
        _notificationCreator = notificationCreator;
        _logger = logger;
    }

    /// <summary>
    /// Case 4 SampleReceiver: Gửi thông báo nhắc nhở trả mẫu cho khách hàng
    /// Chạy lúc 8h sáng hàng ngày cho các TestRequest có Status = ResultSent
    /// </summary>
    [UnitOfWork]
    public virtual async Task SendSampleReturnRemindersAsync()
    {
        _logger.LogInformation("SampleReceiverReminderJob: Starting sample return reminder check...");

        try
        {
            // Lấy các TestRequest có status = ResultSent và có CreatorId
            var pendingTestRequests = await _testRequestRepository.GetListAsync(
                tr => tr.Status == TestRequestStatus.ResultSent &&
                      tr.CreatorId.HasValue);

            _logger.LogInformation("Found {Count} test requests with ResultSent status", pendingTestRequests.Count);

            foreach (var testRequest in pendingTestRequests)
            {
                try
                {
                    await _notificationCreator.NotifySampleReturnReminderAsync(
                        testRequest.CreatorId!.Value,
                        testRequest.ContractNumber ?? "",
                        testRequest.Id);

                    _logger.LogInformation(
                        "Sent sample return reminder for contract {ContractNumber} to creator {CreatorId}",
                        testRequest.ContractNumber,
                        testRequest.CreatorId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending sample return reminder for TestRequest {TestRequestId}", testRequest.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SampleReceiverReminderJob: Error during sample return reminder check");
            throw;
        }

        _logger.LogInformation("SampleReceiverReminderJob: Completed sample return reminder check");
    }
}
