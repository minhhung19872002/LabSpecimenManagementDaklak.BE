using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho Organization + TestRequest
/// Mapping columns: Tên cơ sở, Người liên hệ, Điện thoại, Địa chỉ, Tỉnh/thành, Phường/xã,
/// Mã số thuế, Số hợp đồng, Tên mẫu, Tình trạng, Mã số mẫu, Chỉ tiêu
/// </summary>
public class OrganizationTestRequestImportRow
{
    /// <summary>
    /// Tên cơ sở - dùng để match Organization (upsert)
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Người liên hệ
    /// </summary>
    public string ContactPerson { get; set; } = string.Empty;

    /// <summary>
    /// Điện thoại
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Địa chỉ
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Tỉnh/thành
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Phường/xã
    /// </summary>
    public string Ward { get; set; } = string.Empty;

    /// <summary>
    /// Mã số thuế
    /// </summary>
    public string TaxCode { get; set; } = string.Empty;

    /// <summary>
    /// Số hợp đồng
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Tên mẫu
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Tình trạng mẫu
    /// </summary>
    public string? SampleStatus { get; set; }

    /// <summary>
    /// Mã số mẫu
    /// </summary>
    public string? SampleCode { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu (phân cách bởi dấu phẩy)
    /// </summary>
    public string? Indicators { get; set; }
}

#endregion

#region Preview DTOs

/// <summary>
/// Item preview cho mỗi row import
/// </summary>
public class OrganizationTestRequestPreviewItem
{
    /// <summary>
    /// Số dòng trong Excel (1-based, header = 1)
    /// </summary>
    public int RowNumber { get; set; }

    #region Organization info

    /// <summary>
    /// Tên cơ sở
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// True = Tạo mới Organization, False = Update Organization
    /// </summary>
    public bool IsNewOrganization { get; set; }

    /// <summary>
    /// ID Organization nếu đã tồn tại (IsNewOrganization = false)
    /// </summary>
    public Guid? ExistingOrganizationId { get; set; }

    /// <summary>
    /// Người liên hệ
    /// </summary>
    public string ContactPerson { get; set; } = string.Empty;

    /// <summary>
    /// Điện thoại
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Địa chỉ
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Tỉnh/thành (tên hiển thị)
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Mã tỉnh/thành (code chuẩn từ vn-provinces, null nếu không tìm được)
    /// </summary>
    public string? ProvinceCode { get; set; }

    /// <summary>
    /// Phường/xã (tên hiển thị)
    /// </summary>
    public string Ward { get; set; } = string.Empty;

    /// <summary>
    /// Mã phường/xã (code chuẩn từ vn-provinces, null nếu không tìm được)
    /// </summary>
    public string? WardCode { get; set; }

    /// <summary>
    /// Mã số thuế
    /// </summary>
    public string TaxCode { get; set; } = string.Empty;

    #endregion

    #region TestRequest info

    /// <summary>
    /// Số hợp đồng
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// True = Tạo mới TestRequest, False = Update TestRequest (theo ContractNumber)
    /// </summary>
    public bool IsNewTestRequest { get; set; } = true;

    /// <summary>
    /// ID TestRequest nếu đã tồn tại (IsNewTestRequest = false)
    /// </summary>
    public Guid? ExistingTestRequestId { get; set; }

    /// <summary>
    /// Tên mẫu
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Mã số mẫu
    /// </summary>
    public string? SampleCode { get; set; }

    /// <summary>
    /// Tình trạng mẫu
    /// </summary>
    public string? SampleStatus { get; set; }

    #endregion

    #region Indicators

    /// <summary>
    /// Danh sách tên chỉ tiêu
    /// </summary>
    public List<string> IndicatorNames { get; set; } = new();

    /// <summary>
    /// Danh sách ID chỉ tiêu (chỉ có những chỉ tiêu hợp lệ)
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Danh sách chỉ tiêu không tồn tại trong DB
    /// </summary>
    public List<string> InvalidIndicators { get; set; } = new();

    #endregion

    #region Validation

    /// <summary>
    /// Hợp lệ hay không
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Danh sách lỗi
    /// </summary>
    public List<string> Errors { get; set; } = new();

    #endregion
}

/// <summary>
/// Kết quả preview import Organization + TestRequest
/// </summary>
public class OrganizationTestRequestImportPreviewDto
{
    /// <summary>
    /// Kết quả validation tổng quan
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách items preview
    /// </summary>
    public List<OrganizationTestRequestPreviewItem> Items { get; set; } = new();

    #region Summary

    /// <summary>
    /// Tổng số dòng
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Số dòng hợp lệ
    /// </summary>
    public int ValidRows { get; set; }

    /// <summary>
    /// Số dòng không hợp lệ
    /// </summary>
    public int InvalidRows { get; set; }

    /// <summary>
    /// Số Organization mới sẽ được tạo
    /// </summary>
    public int NewOrganizations { get; set; }

    /// <summary>
    /// Số Organization sẽ được cập nhật
    /// </summary>
    public int UpdateOrganizations { get; set; }

    /// <summary>
    /// Tổng số TestRequest sẽ được xử lý
    /// </summary>
    public int TotalTestRequests { get; set; }

    /// <summary>
    /// Số TestRequest mới sẽ được tạo
    /// </summary>
    public int NewTestRequests { get; set; }

    /// <summary>
    /// Số TestRequest sẽ được cập nhật
    /// </summary>
    public int UpdateTestRequests { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu
    /// </summary>
    public int TotalIndicators { get; set; }

    #endregion

    #region Error file

    /// <summary>
    /// File lỗi dạng base64 (chỉ có khi có lỗi)
    /// </summary>
    public string? ErrorFileBase64 { get; set; }

    /// <summary>
    /// Tên file lỗi
    /// </summary>
    public string? ErrorFileName { get; set; }

    #endregion
}

#endregion
