namespace LabSpecimenManagement.Permissions;

public static class LabSpecimenManagementPermissions
{
    public const string GroupName = "LabSpecimenManagement";

    // TestRequest Permissions
    public static class TestRequests
    {
        public const string Default = GroupName + ".TestRequests";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string SendSample = Default + ".SendSample";
    }

    // Specimen Permissions
    public static class Specimens
    {
        public const string Default = GroupName + ".Specimens";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Test Permissions
    public static class Tests
    {
        public const string Default = GroupName + ".Tests";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Assign = Default + ".Assign";
        public const string Execute = Default + ".Execute";
        public const string Review = Default + ".Review";
        public const string Approve = Default + ".Approve";
    }

    // TestResult Permissions - Key for workflow
    public static class TestResults
    {
        public const string Default = GroupName + ".TestResults";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";

        // Workflow permissions
        public const string Submit = Default + ".Submit";           // KTV submit kết quả
        public const string Review = Default + ".Review";           // Trưởng phòng duyệt B1
        public const string Approve = Default + ".Approve";         // Giám đốc duyệt B2
        public const string Reject = Default + ".Reject";           // Quyền từ chối
    }

    // Report Permissions
    public static class Reports
    {
        public const string Default = GroupName + ".Reports";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Approve = Default + ".Approve";
        public const string Export = Default + ".Export";
        public const string Send = Default + ".Send";
    }

    // NCR Permissions
    public static class NCRs
    {
        public const string Default = GroupName + ".NCRs";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Close = Default + ".Close";
        public const string Assign = Default + ".Assign";
    }

    // Task Assignment Permissions
    public static class TaskAssignments
    {
        public const string Default = GroupName + ".TaskAssignments";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Accept = Default + ".Accept";
        public const string Reject = Default + ".Reject";
        public const string Complete = Default + ".Complete";
    }

    // Laboratory Task Permissions (Quản lý công việc khoa phòng)
    public static class LaboratoryTasks
    {
        public const string Default = GroupName + ".LaboratoryTasks";
        public const string Edit = Default + ".Edit";
        public const string UpdateStatus = Default + ".UpdateStatus";
        public const string ViewAll = Default + ".ViewAll";
    }

    // Subcontractor Permissions
    public static class Subcontractors
    {
        public const string Default = GroupName + ".Subcontractors";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Dashboard Permissions
    public static class Dashboard
    {
        public const string Default = GroupName + ".Dashboard";
    }

    // Statistics Permissions
    public static class Statistics
    {
        public const string Default = GroupName + ".Statistics";
        public const string Export = Default + ".Export";
    }

    // Master Data - Unified Permissions (for all master data features)
    public static class MasterData
    {
        public const string Default = GroupName + ".MasterData";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Import = Default + ".Import"; // For Indicators & IndicatorGroups
    }

    // Suppliers (Quản lý nhà cung cấp)
    public static class Suppliers
    {
        public const string Default = GroupName + ".Suppliers";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Products (Quản lý vật phẩm)
    public static class Products
    {
        public const string Default = GroupName + ".Products";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Department Permissions (DEPRECATED - use MasterData instead)
    public static class Departments
    {
        public const string Default = GroupName + ".Departments";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Specimen Type Permissions
    public static class SpecimenTypes
    {
        public const string Default = GroupName + ".SpecimenTypes";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Test Method Permissions
    public static class TestMethods
    {
        public const string Default = GroupName + ".TestMethods";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Standard Permissions
    public static class Standards
    {
        public const string Default = GroupName + ".Standards";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Storage Location Permissions
    public static class StorageLocations
    {
        public const string Default = GroupName + ".StorageLocations";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Customer Permissions
    public static class Customers
    {
        public const string Default = GroupName + ".Customers";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Indicator Group Permissions
    public static class IndicatorGroups
    {
        public const string Default = GroupName + ".IndicatorGroups";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Import = Default + ".Import";
    }

    // Master Data - Indicator Permissions
    public static class Indicators
    {
        public const string Default = GroupName + ".Indicators";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Import = Default + ".Import";
    }

    // Master Data - Laboratory (Khoa phòng) Permissions
    public static class Laboratories
    {
        public const string Default = GroupName + ".Laboratories";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Audit Log Permissions
    public static class AuditLogs
    {
        public const string Default = GroupName + ".AuditLogs";
    }

    // Import Permissions
    public static class Import
    {
        public const string Default = GroupName + ".Import";
        public const string TestRequests = Default + ".TestRequests";
        public const string Specimens = Default + ".Specimens";
        public const string Customers = Default + ".Customers";
        public const string TestMethods = Default + ".TestMethods";
        public const string Standards = Default + ".Standards";
    }

    // User Management Permissions
    public static class Users
    {
        public const string Default = GroupName + ".Users";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string ManageRoles = Default + ".ManageRoles";
    }

    // Settings Permissions
    public static class Settings
    {
        public const string Default = GroupName + ".Settings";
        public const string Edit = Default + ".Edit";
    }

    // Quotation Permissions (Báo giá)
    public static class Quotations
    {
        public const string Default = GroupName + ".Quotations";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Sync Permissions (Offline data synchronization)
    public static class Sync
    {
        public const string Default = GroupName + ".Sync";
        public const string TestRequests = Default + ".TestRequests";
    }

    // Master Data - Sample Condition Permissions (Tình trạng mẫu)
    public static class SampleConditions
    {
        public const string Default = GroupName + ".SampleConditions";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Sample Type Category Permissions (Danh mục loại mẫu)
    public static class SampleTypeCategories
    {
        public const string Default = GroupName + ".SampleTypeCategories";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Notes Permissions (Quản lý ghi chú)
    public static class Notes
    {
        public const string Default = GroupName + ".Notes";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Test Result Values Permissions (Quản lý kết quả)
    public static class TestResultValues
    {
        public const string Default = GroupName + ".TestResultValues";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    // Master Data - Form Templates Permissions (Form mẫu)
    public static class FormTemplates
    {
        public const string Default = GroupName + ".FormTemplates";
    }

    // Role Management Permissions (Quản lý phân quyền)
    public static class RoleManagement
    {
        public const string Default = GroupName + ".RoleManagement";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string ManagePermissions = Default + ".ManagePermissions";
        public const string ManageUsers = Default + ".ManageUsers";
    }

    // Receipt Permissions (Phiếu thu)
    public static class Receipts
    {
        public const string Default = GroupName + ".Receipts";
        public const string View = Default + ".View";
        public const string Confirm = Default + ".Confirm";
    }
}
