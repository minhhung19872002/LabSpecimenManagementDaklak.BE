using LabSpecimenManagement.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace LabSpecimenManagement.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class LabSpecimenManagementController : AbpControllerBase
{
    protected LabSpecimenManagementController()
    {
        LocalizationResource = typeof(LabSpecimenManagementResource);
    }
}
