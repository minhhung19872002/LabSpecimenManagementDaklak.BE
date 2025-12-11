using Volo.Abp.Modularity;

namespace LabSpecimenManagement;

/* Inherit from this class for your domain layer tests. */
public abstract class LabSpecimenManagementDomainTestBase<TStartupModule> : LabSpecimenManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
