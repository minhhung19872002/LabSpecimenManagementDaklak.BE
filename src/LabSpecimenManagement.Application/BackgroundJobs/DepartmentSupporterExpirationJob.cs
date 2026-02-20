using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job to remove expired department supporters.
/// Runs daily at 00:00 (Vietnam timezone).
/// </summary>
public class DepartmentSupporterExpirationJob : ITransientDependency
{
    private readonly ILaboratoryTaskAppService _laboratoryTaskAppService;
    private readonly ILogger<DepartmentSupporterExpirationJob> _logger;

    public DepartmentSupporterExpirationJob(
        ILaboratoryTaskAppService laboratoryTaskAppService,
        ILogger<DepartmentSupporterExpirationJob> logger)
    {
        _laboratoryTaskAppService = laboratoryTaskAppService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the job to remove all expired department supporters.
    /// </summary>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting department supporter expiration check job...");

        try
        {
            var removedCount = await _laboratoryTaskAppService.RemoveExpiredDepartmentSupportersAsync();

            if (removedCount > 0)
            {
                _logger.LogInformation("Department supporter expiration job completed. Removed {Count} expired supporters.", removedCount);
            }
            else
            {
                _logger.LogInformation("Department supporter expiration job completed. No expired supporters found.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error executing department supporter expiration job");
            throw;
        }
    }
}
