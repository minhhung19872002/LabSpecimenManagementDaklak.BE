using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job để đánh dấu mẫu cần hủy:
/// - Khi hợp đồng hoàn thành (Status = Completed) + 5 ngày
/// - Tự động update NeedDisposal = true cho các mẫu trong HĐ đó
/// - Chạy lúc 1h sáng hàng ngày
/// </summary>
public class SpecimenDisposalReminderJob : ITransientDependency
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly ILogger<SpecimenDisposalReminderJob> _logger;

    // Số ngày sau khi hoàn thành để đánh dấu cần hủy mẫu
    private const int DaysAfterCompletionForDisposal = 5;

    public SpecimenDisposalReminderJob(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        ILogger<SpecimenDisposalReminderJob> logger)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Đánh dấu mẫu cần hủy cho các HĐ đã hoàn thành >= 5 ngày
    /// </summary>
    [UnitOfWork]
    public virtual async Task MarkSpecimensForDisposalAsync()
    {
        _logger.LogInformation("SpecimenDisposalReminderJob: Starting specimen disposal marking...");

        var now = DateTime.Now;
        var threshold = now.AddDays(-DaysAfterCompletionForDisposal); // HĐ hoàn thành từ 5 ngày trước trở về

        try
        {
            // Lấy các TestRequest đã hoàn thành (Status = Completed) và CompletedDate >= 5 ngày trước
            var completedTestRequests = await _testRequestRepository.GetListAsync(
                tr => tr.Status == TestRequestStatus.Completed &&
                      tr.CompletedDate.HasValue &&
                      tr.CompletedDate.Value.Date <= threshold.Date);

            _logger.LogInformation(
                "Found {Count} completed test requests (completed >= {DaysAfterCompletion} days ago)",
                completedTestRequests.Count,
                DaysAfterCompletionForDisposal);

            if (!completedTestRequests.Any())
            {
                _logger.LogInformation("SpecimenDisposalReminderJob: No test requests need disposal marking, skipping...");
                return;
            }

            var testRequestIds = completedTestRequests.Select(tr => tr.Id).ToList();

            // Lấy các mẫu chưa bị cancelled và chưa được đánh dấu cần hủy
            var samplesToMark = await _sampleRepository.GetListAsync(
                s => testRequestIds.Contains(s.TestRequestId) &&
                     !s.IsCancelled &&
                     !s.NeedDisposal);

            _logger.LogInformation("Found {Count} samples to mark for disposal", samplesToMark.Count);

            var markedCount = 0;
            foreach (var sample in samplesToMark)
            {
                try
                {
                    sample.NeedDisposal = true;
                    sample.NeedDisposalDate = now;
                    await _sampleRepository.UpdateAsync(sample);
                    markedCount++;

                    _logger.LogDebug(
                        "Marked sample {SampleCode} (TestRequest: {TestRequestId}) for disposal",
                        sample.SampleCode,
                        sample.TestRequestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error marking sample {SampleId} for disposal",
                        sample.Id);
                }
            }

            _logger.LogInformation(
                "SpecimenDisposalReminderJob: Marked {MarkedCount}/{TotalCount} samples for disposal",
                markedCount,
                samplesToMark.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SpecimenDisposalReminderJob: Error during specimen disposal marking");
            throw;
        }

        _logger.LogInformation("SpecimenDisposalReminderJob: Completed specimen disposal marking");
    }
}
