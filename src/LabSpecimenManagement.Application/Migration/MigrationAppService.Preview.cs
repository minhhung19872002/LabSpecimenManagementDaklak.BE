using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;

namespace LabSpecimenManagement.DataMigration;

public partial class MigrationAppService
{
    [RemoteService(IsEnabled = false)]
    public async Task<MigrationPreviewDto> PreviewVnptMigrationAsync(byte[] fileBytes)
    {
        var rows = ReadVnptExcelRows(fileBytes);
        var masterData = await LoadMasterDataAsync();

        var preview = new MigrationPreviewDto
        {
            TotalRows = rows.Count,
            TestRequestCount = rows.Select(r => r.SoHopDong?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count(),
            SampleCount = rows.Select(r => r.MaSoMau?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count(),
            IndicatorCount = rows.Count
        };

        var validationErrors = ValidateAllRows(rows, masterData);
        preview.ValidationErrors = validationErrors;

        var contractNumbers = rows
            .Select(r => r.SoHopDong?.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();
        preview.SkippedContractCount = contractNumbers.Count(c => masterData.ExistingContracts.Contains(c!));

        preview.IndicatorMatches = BuildMatchSummary(
            rows, r => r.TenChiTieu, masterData.IndicatorByName);

        preview.MethodMatches = BuildMatchSummary(
            rows, r => r.PhuongPhap, masterData.MethodByName);

        preview.KtvMatches = BuildMatchSummary(
            rows, r => r.KyThuatVien, masterData.UserByFullName);

        preview.QlktMatches = BuildMatchSummary(
            rows, r => r.QuanLyKyThuat, masterData.UserByFullName);

        if (preview.SkippedContractCount > 0)
            preview.Warnings.Add($"{preview.SkippedContractCount} phiếu đã tồn tại (ContractNumber trùng), sẽ được bỏ qua khi import.");

        var emptyMethodCount = rows.Count(r => string.IsNullOrWhiteSpace(r.PhuongPhap));
        if (emptyMethodCount > 0)
            preview.Warnings.Add($"{emptyMethodCount}/{rows.Count} chỉ tiêu không có phương pháp thử (sẽ để trống).");

        var emptyPriceCount = rows.Count(r => string.IsNullOrWhiteSpace(r.DonGia));
        if (emptyPriceCount > 0)
            preview.Warnings.Add($"{emptyPriceCount}/{rows.Count} chỉ tiêu không có đơn giá (sẽ đặt = 0).");

        return preview;
    }

    private List<MigrationValidationError> ValidateAllRows(List<VnptExcelRow> rows, MasterData masterData)
    {
        var errors = new List<MigrationValidationError>();

        foreach (var row in rows)
        {
            var tenChiTieu = row.TenChiTieu;
            if (!string.IsNullOrWhiteSpace(tenChiTieu))
            {
                var key = StringHelper.NormalizeAndTrim(tenChiTieu).ToLowerInvariant();
                if (!masterData.IndicatorByName.ContainsKey(key))
                {
                    errors.Add(new MigrationValidationError
                    {
                        ExcelRowNumber = row.ExcelRowNumber,
                        IdPhieu = row.SoHopDong ?? "",
                        ErrorType = "IndicatorNotFound",
                        ErrorValue = tenChiTieu,
                        Message = $"Chỉ tiêu \"{tenChiTieu}\" không tìm thấy trong danh mục."
                    });
                }
            }

            var ktv = row.KyThuatVien;
            if (!string.IsNullOrWhiteSpace(ktv))
            {
                var ktvKey = StringHelper.NormalizeAndTrim(ktv).ToLowerInvariant();
                if (!masterData.UserByFullName.ContainsKey(ktvKey))
                {
                    errors.Add(new MigrationValidationError
                    {
                        ExcelRowNumber = row.ExcelRowNumber,
                        IdPhieu = row.SoHopDong ?? "",
                        ErrorType = "KtvNotFound",
                        ErrorValue = ktv,
                        Message = $"Kỹ thuật viên \"{ktv}\" không tìm thấy trong hệ thống."
                    });
                }
            }

            var qlkt = row.QuanLyKyThuat;
            if (!string.IsNullOrWhiteSpace(qlkt))
            {
                var qlktKey = StringHelper.NormalizeAndTrim(qlkt).ToLowerInvariant();
                if (!masterData.UserByFullName.ContainsKey(qlktKey))
                {
                    errors.Add(new MigrationValidationError
                    {
                        ExcelRowNumber = row.ExcelRowNumber,
                        IdPhieu = row.SoHopDong ?? "",
                        ErrorType = "QlktNotFound",
                        ErrorValue = qlkt,
                        Message = $"Quản lý kỹ thuật \"{qlkt}\" không tìm thấy trong hệ thống."
                    });
                }
            }

            var phuongPhap = row.PhuongPhap;
            if (!string.IsNullOrWhiteSpace(phuongPhap))
            {
                var ppKey = StringHelper.NormalizeAndTrim(phuongPhap).ToLowerInvariant();
                if (!masterData.MethodByName.ContainsKey(ppKey))
                {
                    errors.Add(new MigrationValidationError
                    {
                        ExcelRowNumber = row.ExcelRowNumber,
                        IdPhieu = row.SoHopDong ?? "",
                        ErrorType = "MethodNotFound",
                        ErrorValue = phuongPhap,
                        Message = $"Phương pháp thử \"{phuongPhap}\" không tìm thấy trong danh mục."
                    });
                }
            }

            var resolvedLab = ResolveLabForIndicator(ktv, tenChiTieu, masterData);
            if (resolvedLab == null)
            {
                errors.Add(new MigrationValidationError
                {
                    ExcelRowNumber = row.ExcelRowNumber,
                    IdPhieu = row.SoHopDong ?? "",
                    ErrorType = "LabNotResolved",
                    ErrorValue = $"KTV={ktv ?? "(trống)"}, ChiTieu={tenChiTieu ?? "(trống)"}",
                    Message = $"Không xác định được phòng thí nghiệm cho KTV \"{ktv}\" và chỉ tiêu \"{tenChiTieu}\"."
                });
            }
        }

        return errors;
    }

    private static List<MatchSummaryItem> BuildMatchSummary(
        List<VnptExcelRow> rows,
        System.Func<VnptExcelRow, string?> nameSelector,
        Dictionary<string, System.Guid> masterDict)
    {
        return rows
            .Select(nameSelector)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .GroupBy(n => StringHelper.NormalizeAndTrim(n!).ToLowerInvariant())
            .Where(g => !masterDict.ContainsKey(g.Key))
            .Select(g => new MatchSummaryItem
            {
                Name = g.First()!,
                OccurrenceCount = g.Count()
            })
            .OrderByDescending(m => m.OccurrenceCount)
            .ToList();
    }
}
