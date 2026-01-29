using System.Text;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Helper class for string operations
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Normalizes a string to NFC (Canonical Decomposition, followed by Canonical Composition).
    /// This ensures consistent Unicode representation for comparison and storage.
    /// Example: "Hàm lượng Chì" may have different byte representations but will be normalized to the same form.
    /// </summary>
    /// <param name="input">The input string to normalize</param>
    /// <returns>The normalized string, or the original input if null/empty</returns>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        return input.Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Normalizes and trims a string.
    /// </summary>
    /// <param name="input">The input string to normalize and trim</param>
    /// <returns>The normalized and trimmed string</returns>
    public static string NormalizeAndTrim(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input.Normalize(NormalizationForm.FormC).Trim();
    }
}
