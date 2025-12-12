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
    }
}
