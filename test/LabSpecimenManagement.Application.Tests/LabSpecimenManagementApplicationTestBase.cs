using Volo.Abp.Modularity;

namespace LabSpecimenManagement;

public abstract class LabSpecimenManagementApplicationTestBase<TStartupModule> : LabSpecimenManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
