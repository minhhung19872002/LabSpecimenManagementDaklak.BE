using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace LabSpecimenManagement.Helpers;

/// <summary>
/// Helper class for Vietnam location (Province/Ward) lookup
/// </summary>
public static class VietnamLocationHelper
{
    private static List<ProvinceData>? _provinces;
    private static List<WardData>? _wards;
    private static readonly object _lock = new object();

    #region Data Models

    public class ProvinceData
    {
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string slug { get; set; } = string.Empty;
        public string unit { get; set; } = string.Empty;
    }

    public class WardData
    {
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string slug { get; set; } = string.Empty;
        public string unit { get; set; } = string.Empty;
        public string districtCode { get; set; } = string.Empty;
        public string districtName { get; set; } = string.Empty;
        public string provinceCode { get; set; } = string.Empty;
        public string provinceName { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
    }

    #endregion

    #region Data Loading

    private static void EnsureDataLoaded()
    {
        if (_provinces == null || _wards == null)
        {
            lock (_lock)
            {
                if (_provinces == null || _wards == null)
                {
                    LoadData();
                }
            }
        }
    }

    private static void LoadData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = Path.GetDirectoryName(assembly.Location) ?? "";

        // Try to load from embedded resources first, then from file
        var provincesPath = Path.Combine(assemblyPath, "Data", "provinces.json");
        var wardsPath = Path.Combine(assemblyPath, "Data", "wards.json");

        // If not found in output directory, try the source directory
        if (!File.Exists(provincesPath))
        {
            // Fallback: Try to find in various locations
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "provinces.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "provinces.json"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    provincesPath = path;
                    wardsPath = Path.Combine(Path.GetDirectoryName(path)!, "wards.json");
                    break;
                }
            }
        }

        try
        {
            if (File.Exists(provincesPath))
            {
                var provincesJson = File.ReadAllText(provincesPath);
                _provinces = JsonSerializer.Deserialize<List<ProvinceData>>(provincesJson) ?? new List<ProvinceData>();
            }
            else
            {
                _provinces = new List<ProvinceData>();
            }

            if (File.Exists(wardsPath))
            {
                var wardsJson = File.ReadAllText(wardsPath);
                _wards = JsonSerializer.Deserialize<List<WardData>>(wardsJson) ?? new List<WardData>();
            }
            else
            {
                _wards = new List<WardData>();
            }
        }
        catch (Exception)
        {
            _provinces = new List<ProvinceData>();
            _wards = new List<WardData>();
        }
    }

    #endregion

    #region Province Lookup

    /// <summary>
    /// Find province code by name (fuzzy match)
    /// </summary>
    /// <param name="provinceName">Province name from Excel (e.g., "Tỉnh Đắk Lắk", "Đắk Lắk", "ĐakLak")</param>
    /// <returns>Province code or null if not found</returns>
    public static string? FindProvinceCode(string? provinceName)
    {
        if (string.IsNullOrWhiteSpace(provinceName))
            return null;

        EnsureDataLoaded();

        var searchName = NormalizeName(provinceName);

        // Try exact match first
        var exactMatch = _provinces!.FirstOrDefault(p =>
            NormalizeName(p.name).Equals(searchName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch.code;

        // Try contains match (e.g., "Đắk Lắk" in "Tỉnh Đắk Lắk")
        var containsMatch = _provinces!.FirstOrDefault(p =>
            NormalizeName(p.name).Contains(searchName, StringComparison.OrdinalIgnoreCase) ||
            searchName.Contains(NormalizeName(p.name), StringComparison.OrdinalIgnoreCase));
        if (containsMatch != null)
            return containsMatch.code;

        // Try matching without diacritics and common prefixes
        var normalizedSearch = RemoveDiacriticsAndPrefixes(searchName);
        var fuzzyMatch = _provinces!.FirstOrDefault(p =>
            RemoveDiacriticsAndPrefixes(p.name).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
            normalizedSearch.Contains(RemoveDiacriticsAndPrefixes(p.name), StringComparison.OrdinalIgnoreCase));

        return fuzzyMatch?.code;
    }

    /// <summary>
    /// Get province name by code
    /// </summary>
    public static string? GetProvinceNameByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        EnsureDataLoaded();
        return _provinces!.FirstOrDefault(p => p.code == code)?.name;
    }

    #endregion

    #region Ward Lookup

    /// <summary>
    /// Find ward code by name (fuzzy match)
    /// </summary>
    /// <param name="wardName">Ward name from Excel</param>
    /// <param name="provinceCode">Province code to narrow down search (optional)</param>
    /// <returns>Ward code or null if not found</returns>
    public static string? FindWardCode(string? wardName, string? provinceCode = null)
    {
        if (string.IsNullOrWhiteSpace(wardName))
            return null;

        EnsureDataLoaded();

        var searchName = NormalizeName(wardName);
        var candidates = _wards!.AsEnumerable();

        // Filter by province if provided
        if (!string.IsNullOrWhiteSpace(provinceCode))
        {
            candidates = candidates.Where(w => w.provinceCode == provinceCode);
        }

        var candidateList = candidates.ToList();

        // Try exact match first
        var exactMatch = candidateList.FirstOrDefault(w =>
            NormalizeName(w.name).Equals(searchName, StringComparison.OrdinalIgnoreCase) ||
            NormalizeName(w.fullName).Equals(searchName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch.code;

        // Try contains match
        var containsMatch = candidateList.FirstOrDefault(w =>
            NormalizeName(w.name).Contains(searchName, StringComparison.OrdinalIgnoreCase) ||
            searchName.Contains(NormalizeName(w.name), StringComparison.OrdinalIgnoreCase) ||
            NormalizeName(w.fullName).Contains(searchName, StringComparison.OrdinalIgnoreCase) ||
            searchName.Contains(NormalizeName(w.fullName), StringComparison.OrdinalIgnoreCase));
        if (containsMatch != null)
            return containsMatch.code;

        // Try fuzzy match without diacritics
        var normalizedSearch = RemoveDiacriticsAndPrefixes(searchName);
        var fuzzyMatch = candidateList.FirstOrDefault(w =>
            RemoveDiacriticsAndPrefixes(w.name).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
            normalizedSearch.Contains(RemoveDiacriticsAndPrefixes(w.name), StringComparison.OrdinalIgnoreCase));

        return fuzzyMatch?.code;
    }

    /// <summary>
    /// Get ward name by code
    /// </summary>
    public static string? GetWardNameByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        EnsureDataLoaded();
        return _wards!.FirstOrDefault(w => w.code == code)?.name;
    }

    #endregion

    #region Helper Methods

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.Trim()
            .Replace("  ", " ");
    }

    private static string RemoveDiacriticsAndPrefixes(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var result = name.ToLowerInvariant()
            .Replace("tỉnh ", "")
            .Replace("tinh ", "")
            .Replace("thành phố ", "")
            .Replace("thanh pho ", "")
            .Replace("tp. ", "")
            .Replace("tp ", "")
            .Replace("phường ", "")
            .Replace("phuong ", "")
            .Replace("xã ", "")
            .Replace("xa ", "")
            .Replace("thị trấn ", "")
            .Replace("thi tran ", "")
            .Replace("huyện ", "")
            .Replace("huyen ", "")
            .Replace("quận ", "")
            .Replace("quan ", "")
            .Trim();

        // Remove Vietnamese diacritics
        result = RemoveVietnameseDiacritics(result);

        return result;
    }

    private static string RemoveVietnameseDiacritics(string text)
    {
        var diacritics = new Dictionary<char, char>
        {
            { 'à', 'a' }, { 'á', 'a' }, { 'ả', 'a' }, { 'ã', 'a' }, { 'ạ', 'a' },
            { 'ă', 'a' }, { 'ằ', 'a' }, { 'ắ', 'a' }, { 'ẳ', 'a' }, { 'ẵ', 'a' }, { 'ặ', 'a' },
            { 'â', 'a' }, { 'ầ', 'a' }, { 'ấ', 'a' }, { 'ẩ', 'a' }, { 'ẫ', 'a' }, { 'ậ', 'a' },
            { 'đ', 'd' },
            { 'è', 'e' }, { 'é', 'e' }, { 'ẻ', 'e' }, { 'ẽ', 'e' }, { 'ẹ', 'e' },
            { 'ê', 'e' }, { 'ề', 'e' }, { 'ế', 'e' }, { 'ể', 'e' }, { 'ễ', 'e' }, { 'ệ', 'e' },
            { 'ì', 'i' }, { 'í', 'i' }, { 'ỉ', 'i' }, { 'ĩ', 'i' }, { 'ị', 'i' },
            { 'ò', 'o' }, { 'ó', 'o' }, { 'ỏ', 'o' }, { 'õ', 'o' }, { 'ọ', 'o' },
            { 'ô', 'o' }, { 'ồ', 'o' }, { 'ố', 'o' }, { 'ổ', 'o' }, { 'ỗ', 'o' }, { 'ộ', 'o' },
            { 'ơ', 'o' }, { 'ờ', 'o' }, { 'ớ', 'o' }, { 'ở', 'o' }, { 'ỡ', 'o' }, { 'ợ', 'o' },
            { 'ù', 'u' }, { 'ú', 'u' }, { 'ủ', 'u' }, { 'ũ', 'u' }, { 'ụ', 'u' },
            { 'ư', 'u' }, { 'ừ', 'u' }, { 'ứ', 'u' }, { 'ử', 'u' }, { 'ữ', 'u' }, { 'ự', 'u' },
            { 'ỳ', 'y' }, { 'ý', 'y' }, { 'ỷ', 'y' }, { 'ỹ', 'y' }, { 'ỵ', 'y' },
        };

        var chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (diacritics.TryGetValue(chars[i], out char replacement))
            {
                chars[i] = replacement;
            }
        }

        return new string(chars);
    }

    #endregion
}
