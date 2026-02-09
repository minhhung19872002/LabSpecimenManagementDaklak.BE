using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho Specimen trong TestRequestForm
/// Mapping columns: Tên mẫu, Loại mẫu, Địa chỉ lấy mẫu, Lượng mẫu, Tình trạng mẫu, Chỉ tiêu kiểm nghiệm, Phương pháp
/// </summary>
public class SpecimenForTestRequestImportRow
{
    /// <summary>
    /// Số dòng thực tế trong Excel (1-based, header = 1)
    /// </summary>
    public int ExcelRowNumber { get; set; }

    /// <summary>
    /// Tên mẫu - bắt buộc
    /// </summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>
    /// Loại mẫu - optional
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// Địa chỉ lấy mẫu - optional
    /// </summary>
    public string? SamplingAddress { get; set; }

    /// <summary>
    /// Lượng mẫu - optional
    /// </summary>
    public string? SampleQuantity { get; set; }

    /// <summary>
    /// Tình trạng mẫu - optional
    /// </summary>
    public string? StatusText { get; set; }

    /// <summary>
    /// Tên chỉ tiêu kiểm nghiệm - bắt buộc
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp kiểm nghiệm - optional
    /// </summary>
    public string? TestMethodName { get; set; }
}

#endregion

#region Preview DTOs

/// <summary>
/// Chỉ tiêu trong mẫu import
/// </summary>
public class SpecimenIndicatorImportItem
{
    /// <summary>
    /// Số dòng trong Excel (1-based, header = 1)
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Tên chỉ tiêu (từ Excel)
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// ID chỉ tiêu (nếu tìm thấy trong DB)
    /// </summary>
    public Guid? IndicatorId { get; set; }

    /// <summary>
    /// ID phương pháp mặc định
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// Tên phương pháp mặc định
    /// </summary>
    public string? TestMethodName { get; set; }

    /// <summary>
    /// ID nền mẫu (resolved từ TestMethodIndicator)
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Hợp lệ hay không
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Thông báo lỗi (nếu không hợp lệ)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Mẫu import preview (grouped by SampleName)
/// </summary>
public class SpecimenImportPreviewItem
{
    /// <summary>
    /// Tên mẫu
    /// </summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>
    /// Loại mẫu
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// Địa chỉ lấy mẫu
    /// </summary>
    public string? SamplingAddress { get; set; }

    /// <summary>
    /// Lượng mẫu
    /// </summary>
    public string? SampleQuantity { get; set; }

    /// <summary>
    /// Tình trạng mẫu
    /// </summary>
    public string? StatusText { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu trong mẫu
    /// </summary>
    public List<SpecimenIndicatorImportItem> Indicators { get; set; } = new();

    /// <summary>
    /// Mẫu có hợp lệ không (tất cả indicators đều hợp lệ)
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Danh sách lỗi của mẫu
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Kết quả preview import Specimen cho TestRequestForm
/// </summary>
public class SpecimenForTestRequestImportPreviewDto
{
    /// <summary>
    /// Kết quả validation tổng quan
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách mẫu preview (grouped by SampleName)
    /// </summary>
    public List<SpecimenImportPreviewItem> Specimens { get; set; } = new();

    #region Summary

    /// <summary>
    /// Tổng số dòng trong Excel
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Tổng số mẫu (sau khi group by SampleName)
    /// </summary>
    public int TotalSpecimens { get; set; }

    /// <summary>
    /// Số mẫu hợp lệ
    /// </summary>
    public int ValidSpecimens { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu
    /// </summary>
    public int TotalIndicators { get; set; }

    #endregion

    #region Error File

    /// <summary>
    /// File Excel chứa lỗi (base64 encoded)
    /// </summary>
    public string? ErrorFileBase64 { get; set; }

    /// <summary>
    /// Tên file lỗi
    /// </summary>
    public string? ErrorFileName { get; set; }

    #endregion
}

#endregion
