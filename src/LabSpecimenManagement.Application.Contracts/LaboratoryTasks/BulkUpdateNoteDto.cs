using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO để cập nhật ghi chú cho nhiều chỉ tiêu cùng lúc
/// </summary>
public class BulkUpdateNoteDto
{
    /// <summary>
    /// Danh sách ID TẤT CẢ các chỉ tiêu trong sample (cho FreeTextNote)
    /// </summary>
    [Required]
    public List<Guid> AllIndicatorIds { get; set; } = new();

    /// <summary>
    /// Danh sách ID các chỉ tiêu được chọn (cho MasterDataNoteIds)
    /// </summary>
    [Required]
    public List<Guid> SelectedIndicatorIds { get; set; } = new();

    /// <summary>
    /// Nội dung ghi chú tự do (free text, HTML) - lưu cho TẤT CẢ chỉ tiêu trong sample
    /// </summary>
    public string? FreeTextNote { get; set; }

    /// <summary>
    /// Danh sách ID của Master Data Notes - lưu cho CHỈ các chỉ tiêu được chọn
    /// </summary>
    public List<Guid>? MasterDataNoteIds { get; set; }

    /// <summary>
    /// Chế độ thêm ghi chú:
    /// - true: Thêm vào cuối ghi chú cũ (append)
    /// - false: Thay thế ghi chú cũ (replace)
    /// </summary>
    public bool AppendMode { get; set; } = false;

    /// <summary>
    /// Bỏ qua cập nhật MasterDataNoteIds (giữ nguyên giá trị cũ)
    /// - true: Không cập nhật MasterDataNoteIds cho các chỉ tiêu
    /// - false: Cập nhật MasterDataNoteIds bình thường
    /// </summary>
    public bool SkipMasterDataUpdate { get; set; } = false;
}
