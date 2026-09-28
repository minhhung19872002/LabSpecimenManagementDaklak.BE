using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LabSpecimenManagement.Common;

public static class InventoryBarcodeCodeBuilder
{
    public static string Build(
        string? identificationCode,
        DateTime importDate,
        string? lotNumber,
        int sequence)
    {
        // Sequence is ASCII digits only, so normalizing it separately yields the
        // same result as normalizing the concatenated string.
        return BuildPrefix(identificationCode, importDate, lotNumber)
            + Sanitize(sequence.ToString("D4"));
    }

    /// <summary>
    /// Common prefix shared by every barcode of the same product / import date / lot.
    /// Used to load all existing codes of a lot in a single query.
    /// </summary>
    public static string BuildPrefix(
        string? identificationCode,
        DateTime importDate,
        string? lotNumber)
    {
        var parts = new[]
        {
            identificationCode,
            importDate.ToString("ddMMyyyy"),
            lotNumber
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        return Sanitize(string.Concat(parts));
    }

    private static string Sanitize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Where(c => c >= 32 && c <= 126)
            .ToArray());
    }
}
