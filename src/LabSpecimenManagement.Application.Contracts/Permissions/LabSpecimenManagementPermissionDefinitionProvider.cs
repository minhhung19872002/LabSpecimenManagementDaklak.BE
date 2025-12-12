using LabSpecimenManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace LabSpecimenManagement.Permissions;

public class LabSpecimenManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LabSpecimenManagementPermissions.GroupName, L("Permission:LabSpecimenManagement"));

        // TestRequest Permissions
        var testRequestPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.TestRequests.Default, L("Permission:TestRequests"));
        testRequestPermission.AddChild(LabSpecimenManagementPermissions.TestRequests.Create, L("Permission:TestRequests.Create"));
        testRequestPermission.AddChild(LabSpecimenManagementPermissions.TestRequests.Edit, L("Permission:TestRequests.Edit"));
        testRequestPermission.AddChild(LabSpecimenManagementPermissions.TestRequests.Delete, L("Permission:TestRequests.Delete"));

        // Specimen Permissions
        var specimenPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Specimens.Default, L("Permission:Specimens"));
        specimenPermission.AddChild(LabSpecimenManagementPermissions.Specimens.Create, L("Permission:Specimens.Create"));
        specimenPermission.AddChild(LabSpecimenManagementPermissions.Specimens.Edit, L("Permission:Specimens.Edit"));
        specimenPermission.AddChild(LabSpecimenManagementPermissions.Specimens.Delete, L("Permission:Specimens.Delete"));

        // Test Permissions
        var testPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Tests.Default, L("Permission:Tests"));
        testPermission.AddChild(LabSpecimenManagementPermissions.Tests.Create, L("Permission:Tests.Create"));
        testPermission.AddChild(LabSpecimenManagementPermissions.Tests.Edit, L("Permission:Tests.Edit"));
        testPermission.AddChild(LabSpecimenManagementPermissions.Tests.Delete, L("Permission:Tests.Delete"));
        testPermission.AddChild(LabSpecimenManagementPermissions.Tests.Assign, L("Permission:Tests.Assign"));
        testPermission.AddChild(LabSpecimenManagementPermissions.Tests.Execute, L("Permission:Tests.Execute"));
        testPermission.AddChild(LabSpecimenManagementPermissions.Tests.Review, L("Permission:Tests.Review"));
        testPermission.AddChild(LabSpecimenManagementPermissions.Tests.Approve, L("Permission:Tests.Approve"));

        // TestResult Permissions - Key for workflow
        var testResultPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.TestResults.Default, L("Permission:TestResults"));
        testResultPermission.AddChild(LabSpecimenManagementPermissions.TestResults.Create, L("Permission:TestResults.Create"));
        testResultPermission.AddChild(LabSpecimenManagementPermissions.TestResults.Edit, L("Permission:TestResults.Edit"));
        testResultPermission.AddChild(LabSpecimenManagementPermissions.TestResults.Delete, L("Permission:TestResults.Delete"));
        testResultPermission.AddChild(LabSpecimenManagementPermissions.TestResults.Submit, L("Permission:TestResults.Submit"));
        testResultPermission.AddChild(LabSpecimenManagementPermissions.TestResults.Review, L("Permission:TestResults.Review"));
        testResultPermission.AddChild(LabSpecimenManagementPermissions.TestResults.Approve, L("Permission:TestResults.Approve"));
        testResultPermission.AddChild(LabSpecimenManagementPermissions.TestResults.Reject, L("Permission:TestResults.Reject"));

        // Report Permissions
        var reportPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Reports.Default, L("Permission:Reports"));
        reportPermission.AddChild(LabSpecimenManagementPermissions.Reports.Create, L("Permission:Reports.Create"));
        reportPermission.AddChild(LabSpecimenManagementPermissions.Reports.Edit, L("Permission:Reports.Edit"));
        reportPermission.AddChild(LabSpecimenManagementPermissions.Reports.Delete, L("Permission:Reports.Delete"));
        reportPermission.AddChild(LabSpecimenManagementPermissions.Reports.Approve, L("Permission:Reports.Approve"));
        reportPermission.AddChild(LabSpecimenManagementPermissions.Reports.Export, L("Permission:Reports.Export"));
        reportPermission.AddChild(LabSpecimenManagementPermissions.Reports.Send, L("Permission:Reports.Send"));

        // NCR Permissions
        var ncrPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.NCRs.Default, L("Permission:NCRs"));
        ncrPermission.AddChild(LabSpecimenManagementPermissions.NCRs.Create, L("Permission:NCRs.Create"));
        ncrPermission.AddChild(LabSpecimenManagementPermissions.NCRs.Edit, L("Permission:NCRs.Edit"));
        ncrPermission.AddChild(LabSpecimenManagementPermissions.NCRs.Delete, L("Permission:NCRs.Delete"));
        ncrPermission.AddChild(LabSpecimenManagementPermissions.NCRs.Close, L("Permission:NCRs.Close"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LabSpecimenManagementResource>(name);
    }
}
