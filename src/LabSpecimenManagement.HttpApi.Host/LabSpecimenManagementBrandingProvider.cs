using Microsoft.Extensions.Localization;
using LabSpecimenManagement.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace LabSpecimenManagement;

[Dependency(ReplaceServices = true)]
public class LabSpecimenManagementBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<LabSpecimenManagementResource> _localizer;

    public LabSpecimenManagementBrandingProvider(IStringLocalizer<LabSpecimenManagementResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
