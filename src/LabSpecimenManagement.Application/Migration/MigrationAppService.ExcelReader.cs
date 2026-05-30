using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace LabSpecimenManagement.DataMigration;

public partial class MigrationAppService
{
    private List<VnptExcelRow> ReadVnptExcelRows(byte[] fileBytes)
    {
        var rows = new List<VnptExcelRow>();

        using var stream = new MemoryStream(fileBytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);

        var headerRow = worksheet.Row(1);
        var columnMap = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var headerName = cell.GetString()?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(headerName))
                columnMap[headerName] = cell.Address.ColumnNumber;
        }

        int colIdPhieu = FindColumnIndex(columnMap, "id_phieu");
        int colSoHopDong = FindColumnIndex(columnMap, "sohopdong");
        int colNgayHopDong = FindColumnIndex(columnMap, "ngayhopdong");
        int colKhachHang = FindColumnIndex(columnMap, "khachhang");
        int colMaSoMau = FindColumnIndex(columnMap, "ma_so_mau");
        int colTenMau = FindColumnIndex(columnMap, "ten_mau");
        int colTenChiTieu = FindColumnIndex(columnMap, "ten_chi_tieu");
        int colPhuongPhap = FindColumnIndex(columnMap, "phuongphap");
        int colDonGia = FindColumnIndex(columnMap, "don_gia");
        int colDonViTinh = FindColumnIndex(columnMap, "donvitinh");
        int colKetQua = FindColumnIndex(columnMap, "ket_qua");
        int colQuyDinh = FindColumnIndex(columnMap, "quy_dinh_ky_thuat");
        int colDoKhongDam = FindColumnIndex(columnMap, "do_khong_dam_bao_do_mo_rong");
        int colNgayBd = FindColumnIndex(columnMap, "ngay_bd_kiem_nghiem");
        int colNgayLuu = FindColumnIndex(columnMap, "ngay_luu_kq");
        int colNgayPhan = FindColumnIndex(columnMap, "ngay_phan_chi_tieu");
        int colNgayYc = FindColumnIndex(columnMap, "ngay_yc_hoan_thanh");
        int colTrangThai = FindColumnIndex(columnMap, "trang_thai_duyet");
        int colNgayQlDuyet = FindColumnIndex(columnMap, "ngay_ql_duyet");
        int colGuiQl = FindColumnIndex(columnMap, "gui_ql");
        int colNgayGuiQl = FindColumnIndex(columnMap, "ngay_gui_ql");
        int colKtv = FindColumnIndex(columnMap, "kythuatvien");
        int colQlkt = FindColumnIndex(columnMap, "quanlykythuat");

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            var soHopDong = GetCellString(row, colSoHopDong);
            if (string.IsNullOrWhiteSpace(soHopDong))
                continue;

            rows.Add(new VnptExcelRow
            {
                ExcelRowNumber = rowNum,
                IdPhieu = GetCellString(row, colIdPhieu),
                SoHopDong = soHopDong,
                NgayHopDong = GetCellString(row, colNgayHopDong),
                KhachHang = GetCellString(row, colKhachHang),
                MaSoMau = GetCellString(row, colMaSoMau),
                TenMau = GetCellString(row, colTenMau),
                TenChiTieu = GetCellString(row, colTenChiTieu),
                PhuongPhap = GetCellString(row, colPhuongPhap),
                DonGia = GetCellString(row, colDonGia),
                DonViTinh = GetCellString(row, colDonViTinh),
                KetQua = GetCellString(row, colKetQua),
                QuyDinhKyThuat = GetCellString(row, colQuyDinh),
                DoKhongDamBaoDoMoRong = GetCellString(row, colDoKhongDam),
                NgayBdKiemNghiem = GetCellString(row, colNgayBd),
                NgayLuuKq = GetCellString(row, colNgayLuu),
                NgayPhanChiTieu = GetCellString(row, colNgayPhan),
                NgayYcHoanThanh = GetCellString(row, colNgayYc),
                TrangThaiDuyet = GetCellString(row, colTrangThai),
                NgayQlDuyet = GetCellString(row, colNgayQlDuyet),
                GuiQl = GetCellString(row, colGuiQl),
                NgayGuiQl = GetCellString(row, colNgayGuiQl),
                KyThuatVien = GetCellString(row, colKtv),
                QuanLyKyThuat = GetCellString(row, colQlkt)
            });
        }

        return rows;
    }

    private static int FindColumnIndex(Dictionary<string, int> columnMap, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            if (columnMap.TryGetValue(name.ToLowerInvariant(), out var index))
                return index;
        }
        return -1;
    }

    private static string? GetCellString(IXLRow row, int colIndex)
    {
        if (colIndex < 1) return null;
        var cell = row.Cell(colIndex);
        if (cell.IsEmpty()) return null;
        return cell.GetString()?.Trim();
    }
}
