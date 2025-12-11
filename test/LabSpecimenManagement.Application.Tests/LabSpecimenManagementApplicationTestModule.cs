using Volo.Abp.Modularity;

namespace LabSpecimenManagement;

[DependsOn(
    typeof(LabSpecimenManagementApplicationModule),
    typeof(LabSpecimenManagementDomainTestModule)
)]
public class LabSpecimenManagementApplicationTestModule : AbpModule
{

}
