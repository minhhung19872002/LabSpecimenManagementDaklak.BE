using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LabSpecimenManagement.Common;
using Volo.Abp;

namespace LabSpecimenManagement.DataMigration;

public partial class MigrationAppService
{
    [RemoteService(IsEnabled = false)]
    public async Task<byte[]> ExportPreviewReportAsync(byte[] fileBytes)
    {
        var rows = ReadVnptExcelRows(fileBytes);
        var masterData = await LoadMasterDataAsync();
        var validationErrors = ValidateAllRows(rows, masterData);

        using var workbook = new XLWorkbook();

        WriteSummarySheet(workbook, rows, masterData, validationErrors);
        WriteValidationErrorsSheet(workbook, validationErrors);
        WriteUnmatchedSheet(workbook, "Chỉ tiêu không khớp", rows, r => r.TenChiTieu, masterData.IndicatorByName);
        WriteUnmatchedSheet(workbook, "Phương pháp không khớp", rows, r => r.PhuongPhap, masterData.MethodByName);
        WriteUnmatchedSheet(workbook, "KTV không khớp", rows, r => r.KyThuatVien, masterData.UserByFullName);
        WriteUnmatchedSheet(workbook, "QLKT không khớp", rows, r => r.QuanLyKyThuat, masterData.UserByFullName);
        WriteLabNotResolvedSheet(workbook, rows, masterData);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private void WriteSummarySheet(
        XLWorkbook workbook,
        List<VnptExcelRow> rows,
        MasterData masterData,
        List<MigrationValidationError> errors)
    {
        var ws = workbook.Worksheets.Add("Tổng quan");

        ws.Cell(1, 1).Value = "Báo cáo kiểm tra dữ liệu Migration VNPT";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        var contractNumbers = rows
            .Select(r => r.SoHopDong?.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();
        var skippedCount = contractNumbers.Count(c => masterData.ExistingContracts.Contains(c!));

        var summaryData = new (string Label, object Value)[]
        {
            ("Tổng số dòng Excel", rows.Count),
            ("Số phiếu (SoHopDong)", rows.Select(r => r.SoHopDong?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count()),
            ("Số mẫu (MA_SO_MAU)", rows.Select(r => r.MaSoMau?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count()),
            ("Số chỉ tiêu", rows.Count),
            ("Phiếu đã tồn tại (sẽ bỏ qua)", skippedCount),
            ("", ""),
            ("Tổng số lỗi validation", errors.Count),
            ("- Chỉ tiêu không khớp", errors.Count(e => e.ErrorType == "IndicatorNotFound")),
            ("- KTV không khớp", errors.Count(e => e.ErrorType == "KtvNotFound")),
            ("- QLKT không khớp", errors.Count(e => e.ErrorType == "QlktNotFound")),
            ("- Phương pháp không khớp", errors.Count(e => e.ErrorType == "MethodNotFound")),
            ("- Phòng TN không xác định", errors.Count(e => e.ErrorType == "LabNotResolved")),
            ("", ""),
            ("Chỉ tiêu không có phương pháp", rows.Count(r => string.IsNullOrWhiteSpace(r.PhuongPhap))),
            ("Chỉ tiêu không có đơn giá", rows.Count(r => string.IsNullOrWhiteSpace(r.DonGia))),
        };

        int row = 3;
        foreach (var (label, value) in summaryData)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = !string.IsNullOrEmpty(label);
            if (value is int intVal)
                ws.Cell(row, 2).Value = intVal;
            else
                ws.Cell(row, 2).Value = value?.ToString() ?? "";
            row++;
        }

        ws.Column(1).Width = 35;
        ws.Column(2).Width = 15;
    }

    private static void WriteValidationErrorsSheet(
        XLWorkbook workbook,
        List<MigrationValidationError> errors)
    {
        var ws = workbook.Worksheets.Add("Lỗi validation");

        var headers = new[] { "Dòng Excel", "Số hợp đồng", "Loại lỗi", "Giá trị", "Mô tả" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var err in errors.OrderBy(e => e.ExcelRowNumber))
        {
            ws.Cell(row, 1).Value = err.ExcelRowNumber;
            ws.Cell(row, 2).Value = err.IdPhieu;
            ws.Cell(row, 3).Value = TranslateErrorType(err.ErrorType);
            ws.Cell(row, 4).Value = err.ErrorValue;
            ws.Cell(row, 5).Value = err.Message;
            row++;
        }

        ws.Columns().AdjustToContents(1, 1, 50);
    }

    private static void WriteUnmatchedSheet(
        XLWorkbook workbook,
        string sheetName,
        List<VnptExcelRow> rows,
        System.Func<VnptExcelRow, string?> nameSelector,
        Dictionary<string, System.Guid> masterDict)
    {
        var ws = workbook.Worksheets.Add(sheetName);

        var headers = new[] { "STT", "Tên (trong Excel)", "Số lần xuất hiện", "Dòng Excel (ví dụ)" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var unmatched = rows
            .Where(r => !string.IsNullOrWhiteSpace(nameSelector(r)))
            .GroupBy(r => StringHelper.NormalizeAndTrim(nameSelector(r)!).ToLowerInvariant())
            .Where(g => !masterDict.ContainsKey(g.Key))
            .Select(g => new
            {
                Name = g.First().Let(r => nameSelector(r)!),
                Count = g.Count(),
                ExampleRows = string.Join(", ", g.Take(5).Select(r => r.ExcelRowNumber))
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        int row = 2;
        int stt = 1;
        foreach (var item in unmatched)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = item.Name;
            ws.Cell(row, 3).Value = item.Count;
            ws.Cell(row, 4).Value = item.ExampleRows;
            row++;
        }

        if (unmatched.Count == 0)
        {
            ws.Cell(2, 1).Value = "Tất cả đều khớp với dữ liệu hệ thống.";
            ws.Cell(2, 1).Style.Font.Italic = true;
        }

        ws.Columns().AdjustToContents(1, 1, 50);
    }

    private void WriteLabNotResolvedSheet(
        XLWorkbook workbook,
        List<VnptExcelRow> rows,
        MasterData masterData)
    {
        var ws = workbook.Worksheets.Add("PTN không xác định");

        var headers = new[] { "Dòng Excel", "Số hợp đồng", "KTV", "Tên chỉ tiêu" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var r in rows)
        {
            var labId = ResolveLabForIndicator(r.KyThuatVien, r.TenChiTieu, masterData);
            if (labId == null)
            {
                ws.Cell(row, 1).Value = r.ExcelRowNumber;
                ws.Cell(row, 2).Value = r.SoHopDong ?? "";
                ws.Cell(row, 3).Value = r.KyThuatVien ?? "(trống)";
                ws.Cell(row, 4).Value = r.TenChiTieu ?? "(trống)";
                row++;
            }
        }

        if (row == 2)
        {
            ws.Cell(2, 1).Value = "Tất cả đều xác định được phòng thí nghiệm.";
            ws.Cell(2, 1).Style.Font.Italic = true;
        }

        ws.Columns().AdjustToContents(1, 1, 50);
    }

    private static string TranslateErrorType(string errorType) => errorType switch
    {
        "IndicatorNotFound" => "Chỉ tiêu không khớp",
        "KtvNotFound" => "KTV không khớp",
        "QlktNotFound" => "QLKT không khớp",
        "MethodNotFound" => "Phương pháp không khớp",
        "LabNotResolved" => "PTN không xác định",
        _ => errorType
    };
}

internal static class ObjectExtensions
{
    public static TResult Let<T, TResult>(this T obj, System.Func<T, TResult> func) => func(obj);
}
