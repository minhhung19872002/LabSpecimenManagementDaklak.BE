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
        var parts = new[]
        {
            identificationCode,
            importDate.ToString("ddMMyyyy"),
            lotNumber,
            sequence.ToString("D4")
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        var normalized = string.Concat(parts).Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Where(c => c >= 32 && c <= 126)
            .ToArray());
    }
}
