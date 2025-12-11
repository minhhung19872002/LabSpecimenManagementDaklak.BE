using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Data;

/* This is used if database provider does't define
 * ILabSpecimenManagementDbSchemaMigrator implementation.
 */
public class NullLabSpecimenManagementDbSchemaMigrator : ILabSpecimenManagementDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
