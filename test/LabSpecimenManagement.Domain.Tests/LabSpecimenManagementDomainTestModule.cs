using Volo.Abp.Modularity;

namespace LabSpecimenManagement;

[DependsOn(
    typeof(LabSpecimenManagementDomainModule),
    typeof(LabSpecimenManagementTestBaseModule)
)]
public class LabSpecimenManagementDomainTestModule : AbpModule
{

}
