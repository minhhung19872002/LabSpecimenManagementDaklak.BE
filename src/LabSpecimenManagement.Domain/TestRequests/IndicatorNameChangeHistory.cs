using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Lịch sử admin sửa tên chỉ tiêu trên chứng từ (phiếu thử nghiệm / báo giá).
/// Tên sửa chỉ lưu vào snapshot của chứng từ, KHÔNG đổi master data.
/// Hiện chỉ ghi lại để truy vết (ISO 17025), chưa hiển thị.
/// </summary>
public class IndicatorNameChangeHistory : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Loại chứng từ
    /// </summary>
    public IndicatorNameChangeSource Source { get; set; }

    /// <summary>
    /// ID chứng từ (TestRequestId hoặc QuotationId)
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// ID dòng chỉ tiêu (TestRequestSampleIndicatorId hoặc QuotationSampleIndicatorId)
    /// </summary>
    public Guid LineId { get; set; }

    /// <summary>
    /// ID chỉ tiêu master
    /// </summary>
    public Guid IndicatorId { get; set; }

    public string? OldName { get; set; }
    public string? OldFormattedName { get; set; }
    public string? NewName { get; set; }
    public string? NewFormattedName { get; set; }

    protected IndicatorNameChangeHistory() { }

    public IndicatorNameChangeHistory(
        Guid id,
        IndicatorNameChangeSource source,
        Guid documentId,
        Guid lineId,
        Guid indicatorId,
        string? oldName,
        string? oldFormattedName,
        string? newName,
        string? newFormattedName) : base(id)
    {
        Source = source;
        DocumentId = documentId;
        LineId = lineId;
        IndicatorId = indicatorId;
        OldName = oldName;
        OldFormattedName = oldFormattedName;
        NewName = newName;
        NewFormattedName = newFormattedName;
    }
}

/// <summary>
/// Loại chứng từ có tên chỉ tiêu bị sửa
/// </summary>
public enum IndicatorNameChangeSource
{
    TestRequest = 1,
    Quotation = 2
}
