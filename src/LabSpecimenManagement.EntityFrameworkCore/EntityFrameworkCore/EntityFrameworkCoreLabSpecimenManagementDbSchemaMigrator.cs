using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LabSpecimenManagement.Data;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.EntityFrameworkCore;

public class EntityFrameworkCoreLabSpecimenManagementDbSchemaMigrator
    : ILabSpecimenManagementDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreLabSpecimenManagementDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the LabSpecimenManagementDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<LabSpecimenManagementDbContext>()
            .Database
            .MigrateAsync();
    }
}
