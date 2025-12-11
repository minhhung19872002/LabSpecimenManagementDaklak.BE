using LabSpecimenManagement.Samples;
using Xunit;

namespace LabSpecimenManagement.EntityFrameworkCore.Domains;

[Collection(LabSpecimenManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<LabSpecimenManagementEntityFrameworkCoreTestModule>
{

}
