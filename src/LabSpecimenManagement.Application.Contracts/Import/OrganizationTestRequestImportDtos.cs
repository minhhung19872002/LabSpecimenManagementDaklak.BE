using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho Organization (CS/DN)
/// Mapping columns: Tên cơ sở/doanh nghiệp, Địa chỉ, Tỉnh thành, Phường xã, Người liên hệ, Số điện thoại, Số điện thoại phụ, Mã số thuế
/// </summary>
public class OrganizationTestRequestImportRow
{
    public string OrganizationName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
}

#endregion

#region Preview DTOs

/// <summary>
/// Item preview cho mỗi row import Organization
/// </summary>
public class OrganizationTestRequestPreviewItem
{
    public int RowNumber { get; set; }

    #region Organization info

    public string OrganizationName { get; set; } = string.Empty;
    public bool IsNewOrganization { get; set; }
    public Guid? ExistingOrganizationId { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }
    public string Ward { get; set; } = string.Empty;
    public string? WardCode { get; set; }
    public string TaxCode { get; set; } = string.Empty;

    #endregion

    #region Validation

    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    #endregion
}

/// <summary>
/// Kết quả preview import Organization
/// </summary>
public class OrganizationTestRequestImportPreviewDto
{
    public ImportValidationResult ValidationResult { get; set; } = new();
    public List<OrganizationTestRequestPreviewItem> Items { get; set; } = new();

    #region Summary

    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int NewOrganizations { get; set; }
    public int UpdateOrganizations { get; set; }

    #endregion

    #region Error file

    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }

    #endregion
}

#endregion
