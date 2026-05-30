using System;
using System.Linq;
using LabSpecimenManagement.Common;

namespace LabSpecimenManagement.DataMigration;

public partial class MigrationAppService
{
    private Guid? ResolveLabForIndicator(string? ktvName, string? indicatorName, MasterData masterData)
    {
        if (!string.IsNullOrWhiteSpace(ktvName))
        {
            var ktvKey = StringHelper.NormalizeAndTrim(ktvName).ToLowerInvariant();
            if (masterData.UserByFullName.TryGetValue(ktvKey, out var userId))
            {
                if (masterData.DeptIdByUserId.TryGetValue(userId, out var deptId) && deptId.HasValue)
                {
                    if (masterData.LabByDeptId.TryGetValue(deptId.Value, out var labId))
                    {
                        return labId;
                    }
                }
            }
        }

        return ResolveLabByIndicatorKeyword(indicatorName, masterData);
    }

    private static readonly string[] ViSinhKeywords = new[]
    {
        "coliform", "e.coli", "e. coli", "escherichia", "salmonella", "staphylococcus",
        "clostridium", "pseudomonas", "enterococcus", "streptococcus", "listeria",
        "vibrio", "shigella", "bacillus", "legionella", "vi sinh", "tổng số vi khuẩn",
        "hiếu khí", "kỵ khí", "nấm men", "nấm mốc"
    };

    private static readonly string[] KimLoaiKeywords = new[]
    {
        "asen", "chì", "cadmi", "thủy ngân", "crom", "đồng", "kẽm", "niken",
        "mangan", "sắt", "nhôm", "antimon", "bari", "selen", "molybden",
        "as", "pb", "cd", "hg", "cr", "cu", "zn", "ni", "mn", "fe", "al",
        "kim loại"
    };

    private static readonly string[] HoaLyKeywords = new[]
    {
        "ph", "nitrat", "nitrit", "sunphat", "clorua", "amoni", "florua",
        "độ cứng", "độ đục", "độ màu", "oxy hòa tan", "bod", "cod", "tss",
        "tds", "độ dẫn", "độ kiềm", "cặn", "sunfua", "photphat", "xianua",
        "phenol", "dầu mỡ", "chất rắn", "canxi", "magie", "natri", "kali",
        "bicacbonat", "cacbonat", "silic"
    };

    private static readonly string[] BvtvKeywords = new[]
    {
        "chlorpyrifos", "atrazine", "ddt", "aldrin", "dieldrin", "endrin",
        "heptachlor", "lindane", "methoxychlor", "toxaphene", "bvtv",
        "thuốc bảo vệ", "thuốc trừ sâu", "hóa chất bvtv", "2,4-d", "2,4,5-t",
        "paraquat", "glyphosate", "carbofuran"
    };

    private static readonly string[] ThucPhamKeywords = new[]
    {
        "caffeine", "benzoat", "sorbat", "saccharin", "aspartame", "acesulfame",
        "cyclamate", "tartrazine", "sunset yellow", "amaranth", "brilliant blue",
        "phẩm màu", "chất bảo quản", "chất tạo ngọt", "aflatoxin", "melamine"
    };

    private Guid? ResolveLabByIndicatorKeyword(string? indicatorName, MasterData masterData)
    {
        if (string.IsNullOrWhiteSpace(indicatorName))
            return null;

        var nameLower = indicatorName.ToLowerInvariant();

        if (ViSinhKeywords.Any(k => nameLower.Contains(k)))
            return TryGetLab(masterData, "lvs");

        if (KimLoaiKeywords.Any(k => nameLower.Contains(k)))
            return TryGetLab(masterData, "lhh");

        if (HoaLyKeywords.Any(k => nameLower.Contains(k)))
            return TryGetLab(masterData, "lhl");

        if (BvtvKeywords.Any(k => nameLower.Contains(k)))
            return TryGetLab(masterData, "lhh");

        if (ThucPhamKeywords.Any(k => nameLower.Contains(k)))
            return TryGetLab(masterData, "ltp");

        return null;
    }

    private static Guid? TryGetLab(MasterData masterData, string code)
    {
        if (masterData.LabByCode.TryGetValue(code, out var labId))
            return labId;
        return masterData.LabByCode.Values.Cast<Guid?>().FirstOrDefault();
    }
}
