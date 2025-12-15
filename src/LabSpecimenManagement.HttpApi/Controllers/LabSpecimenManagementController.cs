using LabSpecimenManagement.Localization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// Base controller for all API controllers
/// API Version: v1.0
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
public abstract class LabSpecimenManagementController : AbpControllerBase
{
    protected LabSpecimenManagementController()
    {
        LocalizationResource = typeof(LabSpecimenManagementResource);
    }
}
