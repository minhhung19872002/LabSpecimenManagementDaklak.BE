using LabSpecimenManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace LabSpecimenManagement.Permissions;

public class LabSpecimenManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LabSpecimenManagementPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(LabSpecimenManagementPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LabSpecimenManagementResource>(name);
    }
}
