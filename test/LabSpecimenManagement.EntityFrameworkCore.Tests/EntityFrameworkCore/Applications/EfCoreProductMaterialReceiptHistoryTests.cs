using LabSpecimenManagement.Products;
using Xunit;

namespace LabSpecimenManagement.EntityFrameworkCore.Applications;

[Collection(LabSpecimenManagementTestConsts.CollectionDefinitionName)]
public class EfCoreProductMaterialReceiptHistoryTests
    : ProductMaterialReceiptHistory_Tests<LabSpecimenManagementEntityFrameworkCoreTestModule>
{
}
