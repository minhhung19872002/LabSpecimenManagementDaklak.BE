using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Ghi chú cho từng chỉ tiêu trong từng mẫu của hợp đồng
/// Lưu khi gửi kết quả đến QLCL
/// </summary>
public class SampleIndicatorNote : Entity<Guid>
{
    /// <summary>
    /// TestRequestSampleIndicator ID
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

    /// <summary>
    /// Nội dung ghi chú (đã được edit bởi user, bao gồm ghi chú từ các chỉ tiêu)
    /// </summary>
    [MaxLength(2000)]
    public string? NoteContent { get; set; }

    /// <summary>
    /// Danh sách Note IDs từ Master Data (JSON array)
    /// Format: ["guid1", "guid2", ...]
    /// </summary>
    [MaxLength(500)]
    public string? SelectedNoteIds { get; set; }

    /// <summary>
    /// Ngày tạo
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Người tạo
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual TestRequestSampleIndicator TestRequestSampleIndicator { get; set; } = null!;

    protected SampleIndicatorNote()
    {
    }

    public SampleIndicatorNote(
        Guid id,
        Guid testRequestSampleIndicatorId,
        string? noteContent,
        string? selectedNoteIds,
        DateTime createdDate,
        Guid createdBy) : base(id)
    {
        TestRequestSampleIndicatorId = testRequestSampleIndicatorId;
        NoteContent = noteContent;
        SelectedNoteIds = selectedNoteIds;
        CreatedDate = createdDate;
        CreatedBy = createdBy;
    }
}
