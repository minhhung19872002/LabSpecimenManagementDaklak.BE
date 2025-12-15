using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LabSpecimenManagement.Data;
using Serilog;
using Volo.Abp;
using Volo.Abp.Data;

namespace LabSpecimenManagement.DbMigrator;

public class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _configuration;

    public DbMigratorHostedService(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Log.Information("Starting database migration...");
            
            using (var application = await AbpApplicationFactory.CreateAsync<LabSpecimenManagementDbMigratorModule>(options =>
            {
               options.Services.ReplaceConfiguration(_configuration);
               options.UseAutofac();
               options.Services.AddLogging(c => c.AddSerilog());
               options.AddDataMigrationEnvironment();
            }))
            {
                Log.Information("Application initialized, running migrations...");
                await application.InitializeAsync();

                await application
                    .ServiceProvider
                    .GetRequiredService<LabSpecimenManagementDbMigrationService>()
                    .MigrateAsync();

                Log.Information("Migration completed successfully!");
                await application.ShutdownAsync();

                _hostApplicationLifetime.StopApplication();
            }
        }
        catch (System.Exception ex)
        {
            Log.Fatal(ex, "Migration failed with error: {Message}", ex.Message);
            Log.Fatal("Stack trace: {StackTrace}", ex.StackTrace);
            _hostApplicationLifetime.StopApplication();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
