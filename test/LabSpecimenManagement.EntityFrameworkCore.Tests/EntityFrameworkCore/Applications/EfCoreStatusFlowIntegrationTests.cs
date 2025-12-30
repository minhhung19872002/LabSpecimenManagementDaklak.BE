using LabSpecimenManagement.StatusFlow;
using Xunit;

namespace LabSpecimenManagement.EntityFrameworkCore.Applications;

[Collection(LabSpecimenManagementTestConsts.CollectionDefinitionName)]
public class EfCoreStatusFlowIntegrationTests : StatusFlowIntegration_Tests<LabSpecimenManagementEntityFrameworkCoreTestModule>
{

}
