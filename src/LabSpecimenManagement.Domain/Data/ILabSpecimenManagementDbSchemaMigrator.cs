using System.Threading.Tasks;

namespace LabSpecimenManagement.Data;

public interface ILabSpecimenManagementDbSchemaMigrator
{
    Task MigrateAsync();
}
