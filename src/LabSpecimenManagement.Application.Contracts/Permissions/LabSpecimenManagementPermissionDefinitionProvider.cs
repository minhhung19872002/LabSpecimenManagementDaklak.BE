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
        testRequestPermission.AddChild(LabSpecimenManagementPermissions.TestRequests.SendSample, L("Permission:TestRequests.SendSample"));

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
        ncrPermission.AddChild(LabSpecimenManagementPermissions.NCRs.Assign, L("Permission:NCRs.Assign"));

        // Task Assignment Permissions
        var taskPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.TaskAssignments.Default, L("Permission:TaskAssignments"));
        taskPermission.AddChild(LabSpecimenManagementPermissions.TaskAssignments.Create, L("Permission:TaskAssignments.Create"));
        taskPermission.AddChild(LabSpecimenManagementPermissions.TaskAssignments.Edit, L("Permission:TaskAssignments.Edit"));
        taskPermission.AddChild(LabSpecimenManagementPermissions.TaskAssignments.Delete, L("Permission:TaskAssignments.Delete"));
        taskPermission.AddChild(LabSpecimenManagementPermissions.TaskAssignments.Accept, L("Permission:TaskAssignments.Accept"));
        taskPermission.AddChild(LabSpecimenManagementPermissions.TaskAssignments.Reject, L("Permission:TaskAssignments.Reject"));
        taskPermission.AddChild(LabSpecimenManagementPermissions.TaskAssignments.Complete, L("Permission:TaskAssignments.Complete"));

        // Laboratory Task Permissions (Quản lý công việc khoa phòng)
        var laboratoryTaskPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.LaboratoryTasks.Default, L("Permission:LaboratoryTasks"));
        laboratoryTaskPermission.AddChild(LabSpecimenManagementPermissions.LaboratoryTasks.Edit, L("Permission:LaboratoryTasks.Edit"));
        laboratoryTaskPermission.AddChild(LabSpecimenManagementPermissions.LaboratoryTasks.UpdateStatus, L("Permission:LaboratoryTasks.UpdateStatus"));
        laboratoryTaskPermission.AddChild(LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll, L("Permission:LaboratoryTasks.ViewAll"));

        // Subcontractor Permissions
        var subcontractorPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Subcontractors.Default, L("Permission:Subcontractors"));
        subcontractorPermission.AddChild(LabSpecimenManagementPermissions.Subcontractors.Create, L("Permission:Subcontractors.Create"));
        subcontractorPermission.AddChild(LabSpecimenManagementPermissions.Subcontractors.Edit, L("Permission:Subcontractors.Edit"));
        subcontractorPermission.AddChild(LabSpecimenManagementPermissions.Subcontractors.Delete, L("Permission:Subcontractors.Delete"));

        // Dashboard Permissions
        myGroup.AddPermission(LabSpecimenManagementPermissions.Dashboard.Default, L("Permission:Dashboard"));

        // Statistics Permissions
        var statisticsPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Statistics.Default, L("Permission:Statistics"));
        statisticsPermission.AddChild(LabSpecimenManagementPermissions.Statistics.Export, L("Permission:Statistics.Export"));

        // Suppliers Permissions (Quản lý nhà cung cấp)
        var supplierPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Suppliers.Default, L("Permission:Suppliers"));
        supplierPermission.AddChild(LabSpecimenManagementPermissions.Suppliers.Create, L("Permission:Suppliers.Create"));
        supplierPermission.AddChild(LabSpecimenManagementPermissions.Suppliers.Edit, L("Permission:Suppliers.Edit"));
        supplierPermission.AddChild(LabSpecimenManagementPermissions.Suppliers.Delete, L("Permission:Suppliers.Delete"));

        // Products Permissions (Quản lý vật phẩm)
        var productPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Products.Default, L("Permission:Products"));
        productPermission.AddChild(LabSpecimenManagementPermissions.Products.Create, L("Permission:Products.Create"));
        productPermission.AddChild(LabSpecimenManagementPermissions.Products.Edit, L("Permission:Products.Edit"));
        productPermission.AddChild(LabSpecimenManagementPermissions.Products.Delete, L("Permission:Products.Delete"));

        // Master Data - Unified Permissions (for all master data features)
        var masterDataPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.MasterData.Default, L("Permission:MasterData"));
        masterDataPermission.AddChild(LabSpecimenManagementPermissions.MasterData.View, L("Permission:MasterData.View"));
        masterDataPermission.AddChild(LabSpecimenManagementPermissions.MasterData.Create, L("Permission:MasterData.Create"));
        masterDataPermission.AddChild(LabSpecimenManagementPermissions.MasterData.Edit, L("Permission:MasterData.Edit"));
        masterDataPermission.AddChild(LabSpecimenManagementPermissions.MasterData.Delete, L("Permission:MasterData.Delete"));
        masterDataPermission.AddChild(LabSpecimenManagementPermissions.MasterData.Import, L("Permission:MasterData.Import"));

        // Master Data - Department Permissions (DEPRECATED - keeping for backward compatibility)
        var departmentPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Departments.Default, L("Permission:Departments"));
        departmentPermission.AddChild(LabSpecimenManagementPermissions.Departments.Create, L("Permission:Departments.Create"));
        departmentPermission.AddChild(LabSpecimenManagementPermissions.Departments.Edit, L("Permission:Departments.Edit"));
        departmentPermission.AddChild(LabSpecimenManagementPermissions.Departments.Delete, L("Permission:Departments.Delete"));

        // Master Data - Specimen Type Permissions
        var specimenTypePermission = myGroup.AddPermission(LabSpecimenManagementPermissions.SpecimenTypes.Default, L("Permission:SpecimenTypes"));
        specimenTypePermission.AddChild(LabSpecimenManagementPermissions.SpecimenTypes.Create, L("Permission:SpecimenTypes.Create"));
        specimenTypePermission.AddChild(LabSpecimenManagementPermissions.SpecimenTypes.Edit, L("Permission:SpecimenTypes.Edit"));
        specimenTypePermission.AddChild(LabSpecimenManagementPermissions.SpecimenTypes.Delete, L("Permission:SpecimenTypes.Delete"));

        // Master Data - Test Method Permissions
        var testMethodPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.TestMethods.Default, L("Permission:TestMethods"));
        testMethodPermission.AddChild(LabSpecimenManagementPermissions.TestMethods.Create, L("Permission:TestMethods.Create"));
        testMethodPermission.AddChild(LabSpecimenManagementPermissions.TestMethods.Edit, L("Permission:TestMethods.Edit"));
        testMethodPermission.AddChild(LabSpecimenManagementPermissions.TestMethods.Delete, L("Permission:TestMethods.Delete"));

        // Master Data - Standard Permissions
        var standardPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Standards.Default, L("Permission:Standards"));
        standardPermission.AddChild(LabSpecimenManagementPermissions.Standards.Create, L("Permission:Standards.Create"));
        standardPermission.AddChild(LabSpecimenManagementPermissions.Standards.Edit, L("Permission:Standards.Edit"));
        standardPermission.AddChild(LabSpecimenManagementPermissions.Standards.Delete, L("Permission:Standards.Delete"));

        // Master Data - Storage Location Permissions
        var storageLocationPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.StorageLocations.Default, L("Permission:StorageLocations"));
        storageLocationPermission.AddChild(LabSpecimenManagementPermissions.StorageLocations.Create, L("Permission:StorageLocations.Create"));
        storageLocationPermission.AddChild(LabSpecimenManagementPermissions.StorageLocations.Edit, L("Permission:StorageLocations.Edit"));
        storageLocationPermission.AddChild(LabSpecimenManagementPermissions.StorageLocations.Delete, L("Permission:StorageLocations.Delete"));

        // Master Data - Customer Permissions
        var customerPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Customers.Default, L("Permission:Customers"));
        customerPermission.AddChild(LabSpecimenManagementPermissions.Customers.Create, L("Permission:Customers.Create"));
        customerPermission.AddChild(LabSpecimenManagementPermissions.Customers.Edit, L("Permission:Customers.Edit"));
        customerPermission.AddChild(LabSpecimenManagementPermissions.Customers.Delete, L("Permission:Customers.Delete"));

        // Master Data - Indicator Group Permissions
        var indicatorGroupPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.IndicatorGroups.Default, L("Permission:IndicatorGroups"));
        indicatorGroupPermission.AddChild(LabSpecimenManagementPermissions.IndicatorGroups.Create, L("Permission:IndicatorGroups.Create"));
        indicatorGroupPermission.AddChild(LabSpecimenManagementPermissions.IndicatorGroups.Edit, L("Permission:IndicatorGroups.Edit"));
        indicatorGroupPermission.AddChild(LabSpecimenManagementPermissions.IndicatorGroups.Delete, L("Permission:IndicatorGroups.Delete"));
        indicatorGroupPermission.AddChild(LabSpecimenManagementPermissions.IndicatorGroups.Import, L("Permission:IndicatorGroups.Import"));

        // Master Data - Indicator Permissions
        var indicatorPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Indicators.Default, L("Permission:Indicators"));
        indicatorPermission.AddChild(LabSpecimenManagementPermissions.Indicators.Create, L("Permission:Indicators.Create"));
        indicatorPermission.AddChild(LabSpecimenManagementPermissions.Indicators.Edit, L("Permission:Indicators.Edit"));
        indicatorPermission.AddChild(LabSpecimenManagementPermissions.Indicators.Delete, L("Permission:Indicators.Delete"));
        indicatorPermission.AddChild(LabSpecimenManagementPermissions.Indicators.Import, L("Permission:Indicators.Import"));

        // Master Data - Laboratory (Khoa phòng) Permissions
        var laboratoryPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Laboratories.Default, L("Permission:Laboratories"));
        laboratoryPermission.AddChild(LabSpecimenManagementPermissions.Laboratories.Create, L("Permission:Laboratories.Create"));
        laboratoryPermission.AddChild(LabSpecimenManagementPermissions.Laboratories.Edit, L("Permission:Laboratories.Edit"));
        laboratoryPermission.AddChild(LabSpecimenManagementPermissions.Laboratories.Delete, L("Permission:Laboratories.Delete"));

        // Audit Log Permissions
        myGroup.AddPermission(LabSpecimenManagementPermissions.AuditLogs.Default, L("Permission:AuditLogs"));

        // Import Permissions
        var importPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Import.Default, L("Permission:Import"));
        importPermission.AddChild(LabSpecimenManagementPermissions.Import.TestRequests, L("Permission:Import.TestRequests"));
        importPermission.AddChild(LabSpecimenManagementPermissions.Import.Specimens, L("Permission:Import.Specimens"));
        importPermission.AddChild(LabSpecimenManagementPermissions.Import.Customers, L("Permission:Import.Customers"));
        importPermission.AddChild(LabSpecimenManagementPermissions.Import.TestMethods, L("Permission:Import.TestMethods"));
        importPermission.AddChild(LabSpecimenManagementPermissions.Import.Standards, L("Permission:Import.Standards"));

        // User Management Permissions
        var userPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Users.Default, L("Permission:Users"));
        userPermission.AddChild(LabSpecimenManagementPermissions.Users.Create, L("Permission:Users.Create"));
        userPermission.AddChild(LabSpecimenManagementPermissions.Users.Edit, L("Permission:Users.Edit"));
        userPermission.AddChild(LabSpecimenManagementPermissions.Users.Delete, L("Permission:Users.Delete"));
        userPermission.AddChild(LabSpecimenManagementPermissions.Users.ManageRoles, L("Permission:Users.ManageRoles"));

        // Settings Permissions
        var settingsPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Settings.Default, L("Permission:Settings"));
        settingsPermission.AddChild(LabSpecimenManagementPermissions.Settings.Edit, L("Permission:Settings.Edit"));

        // Quotation Permissions (Báo giá)
        var quotationPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Quotations.Default, L("Permission:Quotations"));
        quotationPermission.AddChild(LabSpecimenManagementPermissions.Quotations.Create, L("Permission:Quotations.Create"));
        quotationPermission.AddChild(LabSpecimenManagementPermissions.Quotations.Edit, L("Permission:Quotations.Edit"));
        quotationPermission.AddChild(LabSpecimenManagementPermissions.Quotations.Delete, L("Permission:Quotations.Delete"));

        // Sync Permissions (Offline data synchronization)
        var syncPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Sync.Default, L("Permission:Sync"));
        syncPermission.AddChild(LabSpecimenManagementPermissions.Sync.TestRequests, L("Permission:Sync.TestRequests"));

        // Master Data - Sample Condition Permissions (Tình trạng mẫu)
        var sampleConditionPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.SampleConditions.Default, L("Permission:SampleConditions"));
        sampleConditionPermission.AddChild(LabSpecimenManagementPermissions.SampleConditions.Create, L("Permission:SampleConditions.Create"));
        sampleConditionPermission.AddChild(LabSpecimenManagementPermissions.SampleConditions.Edit, L("Permission:SampleConditions.Edit"));
        sampleConditionPermission.AddChild(LabSpecimenManagementPermissions.SampleConditions.Delete, L("Permission:SampleConditions.Delete"));

        // Role Management Permissions (Quản lý phân quyền)
        var roleManagementPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.RoleManagement.Default, L("Permission:RoleManagement"));
        roleManagementPermission.AddChild(LabSpecimenManagementPermissions.RoleManagement.Create, L("Permission:RoleManagement.Create"));
        roleManagementPermission.AddChild(LabSpecimenManagementPermissions.RoleManagement.Edit, L("Permission:RoleManagement.Edit"));
        roleManagementPermission.AddChild(LabSpecimenManagementPermissions.RoleManagement.Delete, L("Permission:RoleManagement.Delete"));
        roleManagementPermission.AddChild(LabSpecimenManagementPermissions.RoleManagement.ManagePermissions, L("Permission:RoleManagement.ManagePermissions"));
        roleManagementPermission.AddChild(LabSpecimenManagementPermissions.RoleManagement.ManageUsers, L("Permission:RoleManagement.ManageUsers"));

        // Receipt Permissions (Phiếu thu)
        var receiptPermission = myGroup.AddPermission(LabSpecimenManagementPermissions.Receipts.Default, L("Permission:Receipts"));
        receiptPermission.AddChild(LabSpecimenManagementPermissions.Receipts.View, L("Permission:Receipts.View"));
        receiptPermission.AddChild(LabSpecimenManagementPermissions.Receipts.Confirm, L("Permission:Receipts.Confirm"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LabSpecimenManagementResource>(name);
    }
}
