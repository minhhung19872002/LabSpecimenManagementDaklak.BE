using System.Collections.Generic;

namespace LabSpecimenManagement.DataMigration;

public class VnptExcelRow
{
    public int ExcelRowNumber { get; set; }
    public string? IdPhieu { get; set; }
    public string? SoHopDong { get; set; }
    public string? NgayHopDong { get; set; }
    public string? KhachHang { get; set; }
    public string? MaSoMau { get; set; }
    public string? TenMau { get; set; }
    public string? TenChiTieu { get; set; }
    public string? PhuongPhap { get; set; }
    public string? DonGia { get; set; }
    public string? DonViTinh { get; set; }
    public string? KetQua { get; set; }
    public string? QuyDinhKyThuat { get; set; }
    public string? DoKhongDamBaoDoMoRong { get; set; }
    public string? NgayBdKiemNghiem { get; set; }
    public string? NgayLuuKq { get; set; }
    public string? NgayPhanChiTieu { get; set; }
    public string? NgayYcHoanThanh { get; set; }
    public string? TrangThaiDuyet { get; set; }
    public string? NgayQlDuyet { get; set; }
    public string? GuiQl { get; set; }
    public string? NgayGuiQl { get; set; }
    public string? KyThuatVien { get; set; }
    public string? QuanLyKyThuat { get; set; }
}

public class MigrationPreviewDto
{
    public int TotalRows { get; set; }
    public int TestRequestCount { get; set; }
    public int SampleCount { get; set; }
    public int IndicatorCount { get; set; }
    public int SkippedContractCount { get; set; }
    public List<MatchSummaryItem> IndicatorMatches { get; set; } = new();
    public List<MatchSummaryItem> MethodMatches { get; set; } = new();
    public List<MatchSummaryItem> KtvMatches { get; set; } = new();
    public List<MatchSummaryItem> QlktMatches { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<MigrationValidationError> ValidationErrors { get; set; } = new();
}

public class MigrationResultDto
{
    public bool Success { get; set; }
    public int TestRequestCount { get; set; }
    public int SampleCount { get; set; }
    public int IndicatorCount { get; set; }
    public int LabTaskCount { get; set; }
    public int QmTaskCount { get; set; }
    public int SkippedCount { get; set; }
    public List<MigrationValidationError> ValidationErrors { get; set; } = new();
}

public class MatchSummaryItem
{
    public string Name { get; set; } = "";
    public int OccurrenceCount { get; set; }
}

public class MigrationRollbackResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int TestRequestCount { get; set; }
    public int SampleCount { get; set; }
    public int IndicatorCount { get; set; }
    public int LabTaskCount { get; set; }
    public int LabTaskIndicatorCount { get; set; }
    public int QmTaskCount { get; set; }
    public int QmIndicatorCount { get; set; }
}

public class MigrationValidationError
{
    public int ExcelRowNumber { get; set; }
    public string IdPhieu { get; set; } = "";
    public string ErrorType { get; set; } = "";
    public string ErrorValue { get; set; } = "";
    public string Message { get; set; } = "";
}
