using System.Collections.Generic;
using System.Linq;
using LabSpecimenManagement.Permissions;

namespace LabSpecimenManagement.Shared;

/// <summary>
/// Permission group configuration for UI display
/// Shared between RoleManagement and Employee permission management
/// </summary>
public record PermissionGroupConfig(string Name, string DisplayName, (string PermissionName, string DisplayName)[] Permissions);

/// <summary>
/// Static class containing all permission group configurations
/// </summary>
public static class PermissionGroupConfigs
{
    public static readonly List<PermissionGroupConfig> All = new()
    {
        // ==================== NGHIỆP VỤ CHÍNH ====================
        new PermissionGroupConfig("PhieuYeuCau", "Phiếu yêu cầu", new[]
        {
            (LabSpecimenManagementPermissions.TestRequests.Default, "Xem phiếu yêu cầu"),
            (LabSpecimenManagementPermissions.TestRequests.Create, "Tạo phiếu yêu cầu"),
            (LabSpecimenManagementPermissions.TestRequests.Edit, "Chỉnh sửa phiếu yêu cầu"),
            (LabSpecimenManagementPermissions.TestRequests.SendSample, "Gửi mẫu"),
            (LabSpecimenManagementPermissions.TestRequests.Delete, "Hủy yêu cầu"),
        }),
        new PermissionGroupConfig("QuanLyCongViec", "Quản lý công việc", new[]
        {
            (LabSpecimenManagementPermissions.LaboratoryTasks.Default, "Xem danh sách công việc"),
            (LabSpecimenManagementPermissions.LaboratoryTasks.ViewAll, "Xem chi tiết công việc"),
            (LabSpecimenManagementPermissions.Tests.Assign, "Phân công thử nghiệm"),
            (LabSpecimenManagementPermissions.LaboratoryTasks.Edit, "Thêm người hỗ trợ"),
            (LabSpecimenManagementPermissions.Tests.Edit, "Sửa phương pháp"),
            (LabSpecimenManagementPermissions.Tests.Review, "Duyệt kết quả"),
        }),
        new PermissionGroupConfig("QuanLyThuNghiem", "Quản lý thử nghiệm", new[]
        {
            (LabSpecimenManagementPermissions.Tests.Default, "Xem danh sách công việc"),
            (LabSpecimenManagementPermissions.Tests.Execute, "Xem chi tiết công việc"),
            (LabSpecimenManagementPermissions.TestResults.Create, "Nhập kết quả"),
            (LabSpecimenManagementPermissions.TestResults.Edit, "Sửa kết quả"),
            (LabSpecimenManagementPermissions.TestResults.Submit, "Gửi kết quả"),
        }),
        new PermissionGroupConfig("QuanLyChatLuong", "Quản lý chất lượng", new[]
        {
            (LabSpecimenManagementPermissions.TestResults.Review, "Xem kết quả đã duyệt kỹ thuật"),
            (LabSpecimenManagementPermissions.TestResults.Approve, "Phê duyệt chất lượng"),
            (LabSpecimenManagementPermissions.TestResults.Reject, "Trả hồ sơ chỉnh sửa"),
            (LabSpecimenManagementPermissions.Reports.Create, "Tạo report"),
        }),
        new PermissionGroupConfig("QuanLyPhieuThu", "Quản lý phiếu thu", new[]
        {
            (LabSpecimenManagementPermissions.Receipts.View, "Xem phiếu thu"),
            (LabSpecimenManagementPermissions.Receipts.Confirm, "Xác nhận phiếu thu"),
        }),

        // ==================== BÁO GIÁ ====================
        new PermissionGroupConfig("BaoGia", "Báo giá", new[]
        {
            (LabSpecimenManagementPermissions.Quotations.Default, "Xem báo giá"),
            (LabSpecimenManagementPermissions.Quotations.Create, "Tạo báo giá"),
            (LabSpecimenManagementPermissions.Quotations.Edit, "Sửa báo giá"),
            (LabSpecimenManagementPermissions.Quotations.Delete, "Xóa báo giá"),
        }),

        // ==================== QUẢN LÝ MẪU ====================
        new PermissionGroupConfig("QuanLyMau", "Quản lý mẫu", new[]
        {
            (LabSpecimenManagementPermissions.Specimens.Default, "Xem danh sách mẫu"),
            (LabSpecimenManagementPermissions.Specimens.Create, "Tạo mẫu"),
            (LabSpecimenManagementPermissions.Specimens.Edit, "Sửa thông tin mẫu"),
            (LabSpecimenManagementPermissions.Specimens.Delete, "Xóa mẫu"),
        }),

        // ==================== BÁO CÁO ====================
        new PermissionGroupConfig("BaoCao", "Báo cáo", new[]
        {
            (LabSpecimenManagementPermissions.Reports.Default, "Xem báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Create, "Tạo báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Edit, "Sửa báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Delete, "Xóa báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Approve, "Duyệt báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Export, "Xuất báo cáo"),
            (LabSpecimenManagementPermissions.Reports.Send, "Gửi báo cáo"),
        }),

        // ==================== NCR ====================
        new PermissionGroupConfig("NCR", "Báo cáo NCR", new[]
        {
            (LabSpecimenManagementPermissions.NCRs.Default, "Xem NCR"),
            (LabSpecimenManagementPermissions.NCRs.Create, "Tạo NCR"),
            (LabSpecimenManagementPermissions.NCRs.Edit, "Sửa NCR"),
            (LabSpecimenManagementPermissions.NCRs.Delete, "Xóa NCR"),
            (LabSpecimenManagementPermissions.NCRs.Close, "Đóng NCR"),
            (LabSpecimenManagementPermissions.NCRs.Assign, "Gán NCR"),
        }),

        // ==================== THỐNG KÊ ====================
        new PermissionGroupConfig("ThongKe", "Thống kê", new[]
        {
            (LabSpecimenManagementPermissions.Dashboard.Default, "Xem dashboard"),
            (LabSpecimenManagementPermissions.Statistics.Default, "Xem thống kê"),
            (LabSpecimenManagementPermissions.Statistics.Export, "Xuất báo cáo thống kê"),
        }),

        // ==================== NHÀ THẦU PHỤ ====================
        new PermissionGroupConfig("NhaThauPhu", "Nhà thầu phụ", new[]
        {
            (LabSpecimenManagementPermissions.Subcontractors.Default, "Xem nhà thầu phụ"),
            (LabSpecimenManagementPermissions.Subcontractors.Create, "Tạo nhà thầu phụ"),
            (LabSpecimenManagementPermissions.Subcontractors.Edit, "Sửa nhà thầu phụ"),
            (LabSpecimenManagementPermissions.Subcontractors.Delete, "Xóa nhà thầu phụ"),
        }),

        // ==================== CƠ SỞ/DOANH NGHIỆP ====================
        new PermissionGroupConfig("CoSoDoanhNghiep", "Cơ sở/Doanh nghiệp", new[]
        {
            (LabSpecimenManagementPermissions.Organizations.Default, "Xem cơ sở/doanh nghiệp"),
            (LabSpecimenManagementPermissions.Organizations.Create, "Tạo cơ sở/doanh nghiệp"),
            (LabSpecimenManagementPermissions.Organizations.Edit, "Sửa cơ sở/doanh nghiệp"),
            (LabSpecimenManagementPermissions.Organizations.Delete, "Xóa cơ sở/doanh nghiệp"),
        }),

        // ==================== DỮ LIỆU CHỦ (MASTER DATA) ====================
        new PermissionGroupConfig("MasterData", "Dữ liệu chủ", new[]
        {
            (LabSpecimenManagementPermissions.MasterData.Default, "Xem dữ liệu chủ"),
            (LabSpecimenManagementPermissions.MasterData.View, "Xem chi tiết"),
            (LabSpecimenManagementPermissions.MasterData.Create, "Tạo mới"),
            (LabSpecimenManagementPermissions.MasterData.Edit, "Chỉnh sửa"),
            (LabSpecimenManagementPermissions.MasterData.Delete, "Xóa"),
            (LabSpecimenManagementPermissions.MasterData.Import, "Import dữ liệu"),
        }),
        new PermissionGroupConfig("MasterDataDetail", "Dữ liệu chi tiết", new[]
        {
            (LabSpecimenManagementPermissions.Departments.Default, "Phòng ban"),
            (LabSpecimenManagementPermissions.Laboratories.Default, "Khoa phòng"),
            (LabSpecimenManagementPermissions.SampleTypeCategories.Default, "Danh mục loại mẫu"),
            (LabSpecimenManagementPermissions.SampleConditions.Default, "Tình trạng mẫu"),
            (LabSpecimenManagementPermissions.SpecimenTypes.Default, "Loại mẫu"),
            (LabSpecimenManagementPermissions.TestMethods.Default, "Phương pháp thử"),
            (LabSpecimenManagementPermissions.Standards.Default, "Tiêu chuẩn"),
            (LabSpecimenManagementPermissions.StorageLocations.Default, "Vị trí lưu kho"),
            (LabSpecimenManagementPermissions.Indicators.Default, "Nhóm chỉ tiêu"),
            (LabSpecimenManagementPermissions.FormTemplates.Default, "Form mẫu"),
            (LabSpecimenManagementPermissions.TestResultValues.Default, "Quản lý kết quả"),
            (LabSpecimenManagementPermissions.Notes.Default, "Quản lý ghi chú"),
        }),

        // ==================== QUẢN TRỊ HỆ THỐNG ====================
        new PermissionGroupConfig("QuanTriHeThong", "Quản trị hệ thống", new[]
        {
            // Người dùng (Users)
            (LabSpecimenManagementPermissions.Users.Default, "Xem người dùng"),
            (LabSpecimenManagementPermissions.Users.Create, "Tạo người dùng"),
            (LabSpecimenManagementPermissions.Users.Edit, "Sửa người dùng"),
            (LabSpecimenManagementPermissions.Users.Delete, "Xóa người dùng"),
            (LabSpecimenManagementPermissions.Users.ManageRoles, "Quản lý vai trò người dùng"),
            // Phân quyền (Role Management)
            (LabSpecimenManagementPermissions.RoleManagement.Default, "Xem phân quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.Create, "Tạo nhóm quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.Edit, "Sửa nhóm quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.Delete, "Xóa nhóm quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.ManagePermissions, "Quản lý quyền"),
            (LabSpecimenManagementPermissions.RoleManagement.ManageUsers, "Quản lý người dùng trong nhóm"),
            // Cài đặt (Settings)
            (LabSpecimenManagementPermissions.Settings.Default, "Xem cài đặt"),
            (LabSpecimenManagementPermissions.Settings.Edit, "Sửa cài đặt"),
            // Lịch sử thay đổi (Audit Logs)
            (LabSpecimenManagementPermissions.AuditLogs.Default, "Xem lịch sử thay đổi"),
        }),
    };

    /// <summary>
    /// Get all permission names from all groups
    /// </summary>
    public static List<string> GetAllPermissionNames()
    {
        return All
            .SelectMany(g => g.Permissions.Select(p => p.PermissionName))
            .Distinct()
            .ToList();
    }
}
