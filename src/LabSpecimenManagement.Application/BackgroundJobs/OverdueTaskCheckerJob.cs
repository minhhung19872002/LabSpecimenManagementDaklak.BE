using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job tự động chuyển các task Pending đã quá hạn sang Overdue.
/// Runs daily at 00:05 AM Vietnam time.
/// </summary>
public class OverdueTaskCheckerJob : ITransientDependency
{
    private readonly IRepository<SupplementaryTasks.SupplementaryTask, Guid> _repository;
    private readonly IRepository<SupplementaryTasks.SupplementaryTaskLog, Guid> _logRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ILogger<OverdueTaskCheckerJob> _logger;

    public OverdueTaskCheckerJob(
        IRepository<SupplementaryTasks.SupplementaryTask, Guid> repository,
        IRepository<SupplementaryTasks.SupplementaryTaskLog, Guid> logRepository,
        IGuidGenerator guidGenerator,
        ILogger<OverdueTaskCheckerJob> logger)
    {
        _repository = repository;
        _logRepository = logRepository;
        _guidGenerator = guidGenerator;
        _logger = logger;
    }

    [UnitOfWork]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting overdue task checker job...");

        try
        {
            var today = DateTime.Now.Date;
            var query = await _repository.GetQueryableAsync();
            var overdueItems = query
                .Where(t => t.DueDate.HasValue
                       && t.DueDate.Value.Date < today
                       && t.Status == SupplementaryTasks.SupplementaryTaskStatus.Pending)
                .ToList();

            if (!overdueItems.Any())
            {
                _logger.LogInformation("Overdue task checker completed. No overdue tasks found.");
                return;
            }

            foreach (var task in overdueItems)
            {
                task.Status = SupplementaryTasks.SupplementaryTaskStatus.Overdue;
                await _repository.UpdateAsync(task);
                await _logRepository.InsertAsync(new SupplementaryTasks.SupplementaryTaskLog(
                    _guidGenerator.Create(),
                    task.Id,
                    SupplementaryTasks.SupplementaryTaskStatus.Pending,
                    SupplementaryTasks.SupplementaryTaskStatus.Overdue,
                    Guid.Empty,
                    "Hệ thống",
                    "Tự động cập nhật quá hạn"));
            }

            _logger.LogInformation("Overdue task checker completed. Updated {Count} tasks to Overdue.", overdueItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing overdue task checker job");
            throw;
        }
    }
}
