using System.Collections.Generic;
using System.Linq;
using LabSpecimenManagement.Shared;

namespace LabSpecimenManagement.TemporaryAccess;

public static class TemporaryAccessPermissionMapper
{
    // Groups available in the "Cấp quyền tạm thời" modal (13 groups — excludes MasterData and QuanTriHeThong)
    private static readonly HashSet<string> AllowedGroupNames = new()
    {
        "PhieuYeuCau", "QuanLyCongViec", "QuanLyChatLuong", "QuanLyThuNghiem",
        "QuanLyPhieuThu", "BaoGia", "QuanLyMau", "BaoCao", "NCR",
        "ThongKe", "ThongKePhongLab", "NhaThauPhu", "CoSoDoanhNghiep"
    };

    public static List<string> GetPermissionsForModule(string groupName)
    {
        if (!AllowedGroupNames.Contains(groupName))
            return new();

        var group = PermissionGroupConfigs.All.FirstOrDefault(g => g.Name == groupName);
        return group?.Permissions.Select(p => p.PermissionName).ToList() ?? new();
    }

    public static List<string> GetPermissionsForModules(IEnumerable<string> groupNames)
        => groupNames
            .SelectMany(GetPermissionsForModule)
            .Distinct()
            .ToList();
}
