using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LabSpecimenManagement.Statistics;

/// <summary>
/// Cột "Filter Nền mẫu" của báo cáo chi tiết phòng lab: gom mẫu vào 4 nhóm theo tên mẫu.
/// Dựa trên công thức Excel của BA (đóng chai → đóng bình → bình → chai → nước → còn lại thực phẩm),
/// sửa các trường hợp công thức xếp sai: địa danh "Thanh Bình", món "nước chấm/mắm/lèo", "tương ớt chai",
/// nước khoáng, đá viên.
/// </summary>
public static class SampleGroupClassifier
{
    public const string BottledJug = "Nước đóng bình";
    public const string Bottled = "Nước đóng chai";
    public const string CleanWater = "Nước sạch";
    public const string Food = "Thực phẩm";

    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    private static readonly Regex Whitespace = new(@"\s+", Options);

    // Mẫu lau bề mặt dụng cụ chế biến
    private static readonly Regex SurfaceSwab = new(@"^mẫu bề mặt\b", Options);

    // Đá viên / nước đá thành phẩm (không bắt "nước làm đá", "sản xuất nước đá", "nướng đá")
    private static readonly Regex Ice = new(@"^(mẫu )?(nước )?đá\b", Options);

    // Món ăn / đồ uống có chữ "nước" nhưng không phải mẫu nước
    private static readonly Regex DishWithWater = new(
        @"\bnước (lèo|leo|mắm|sốt|chấm|ép|mía|lẩu|chan|dùng|cốt|tương|dừa|ngọt|giải khát|trái cây|hoa quả|xương|hầm)\b|\btrôi nước\b",
        Options);

    private static readonly Regex Water = new(@"\bnước\b", Options);

    // Nước nguồn để sản xuất (nước giếng dùng sản xuất nước uống đóng chai / nước đá...) → nước sạch
    private static readonly Regex SourceWater = new(@"\bsản xuất (nước|đá)\b|\blàm đá\b|\bnước nguồn\b", Options);

    private static readonly Regex BottledName = new(@"\bđóng chai\b", Options);
    private static readonly Regex BottledJugName = new(@"\bđóng bình\b|^nước bình\b|\bbình \d", Options);
    private static readonly Regex BottleWord = new(@"\bchai\b", Options);
    private static readonly Regex Mineral = new(@"\bkhoáng\b(?! sản)|\bkiềm\b", Options);

    /// <param name="sampleName">Tên mẫu.</param>
    /// <param name="specimenTypeNames">Các nền mẫu (loại mẫu) của các chỉ tiêu trong mẫu — dùng khi tên mẫu không có chữ "nước".</param>
    public static string Classify(string? sampleName, IEnumerable<string?> specimenTypeNames)
    {
        var name = Normalize(sampleName);

        if (SurfaceSwab.IsMatch(name) || Ice.IsMatch(name)) return Food;

        var withoutDishes = DishWithWater.Replace(name, " ");
        if (Water.IsMatch(withoutDishes))
        {
            if (SourceWater.IsMatch(name)) return CleanWater;
            if (BottledName.IsMatch(name)) return Bottled;
            if (BottledJugName.IsMatch(name)) return BottledJug;
            if (BottleWord.IsMatch(name) || Mineral.IsMatch(name)) return Bottled;
            return CleanWater;
        }

        // Tên không cho biết là nước (vd "TP01", "Mẫu sau lọc (1)"): coi là nước nếu mọi nền mẫu đều là nước
        var types = specimenTypeNames.Select(Normalize).Where(t => t.Length > 0).Distinct().ToList();
        if (types.Count == 0 || !types.All(t => t.StartsWith("nước"))) return Food;
        if (types.Any(t => BottledName.IsMatch(t) || Mineral.IsMatch(t))) return Bottled;
        if (types.Any(t => BottledJugName.IsMatch(t))) return BottledJug;
        return CleanWater;
    }

    private static string Normalize(string? s) =>
        string.IsNullOrWhiteSpace(s)
            ? ""
            : Whitespace.Replace(s.Normalize(NormalizationForm.FormC).ToLowerInvariant(), " ").Trim();
}
