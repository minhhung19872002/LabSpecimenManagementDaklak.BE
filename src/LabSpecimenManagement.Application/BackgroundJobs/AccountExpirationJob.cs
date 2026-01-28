using System.Threading.Tasks;
using LabSpecimenManagement.Employees;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job to deactivate expired user accounts.
/// Runs daily at 1:00 AM.
/// </summary>
public class AccountExpirationJob : ITransientDependency
{
    private readonly IEmployeeAppService _employeeAppService;
    private readonly ILogger<AccountExpirationJob> _logger;

    public AccountExpirationJob(
        IEmployeeAppService employeeAppService,
        ILogger<AccountExpirationJob> logger)
    {
        _employeeAppService = employeeAppService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the job to deactivate all expired accounts.
    /// </summary>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting account expiration check job...");

        try
        {
            var deactivatedCount = await _employeeAppService.DeactivateExpiredAccountsAsync();

            if (deactivatedCount > 0)
            {
                _logger.LogInformation("Account expiration job completed. Deactivated {Count} expired accounts.", deactivatedCount);
            }
            else
            {
                _logger.LogInformation("Account expiration job completed. No expired accounts found.");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error executing account expiration job");
            throw;
        }
    }
}
