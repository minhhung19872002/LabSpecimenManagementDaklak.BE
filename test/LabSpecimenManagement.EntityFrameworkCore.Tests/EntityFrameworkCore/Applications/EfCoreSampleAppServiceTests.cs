using LabSpecimenManagement.Samples;
using Xunit;

namespace LabSpecimenManagement.EntityFrameworkCore.Applications;

[Collection(LabSpecimenManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<LabSpecimenManagementEntityFrameworkCoreTestModule>
{

}
