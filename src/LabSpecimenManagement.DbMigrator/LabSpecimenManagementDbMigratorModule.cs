using LabSpecimenManagement.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace LabSpecimenManagement.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(LabSpecimenManagementEntityFrameworkCoreModule),
    typeof(LabSpecimenManagementApplicationContractsModule)
    )]
public class LabSpecimenManagementDbMigratorModule : AbpModule
{
}
