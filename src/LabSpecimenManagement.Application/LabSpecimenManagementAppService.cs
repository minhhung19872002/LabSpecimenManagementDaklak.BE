using System;
using System.Collections.Generic;
using System.Text;
using LabSpecimenManagement.Localization;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement;

/* Inherit your application services from this class.
 */
public abstract class LabSpecimenManagementAppService : ApplicationService
{
    protected LabSpecimenManagementAppService()
    {
        LocalizationResource = typeof(LabSpecimenManagementResource);
    }
}
