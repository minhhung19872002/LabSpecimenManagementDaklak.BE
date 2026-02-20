using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData.Notes;

/// <summary>
/// DTO cập nhật Ghi chú
/// </summary>
public class UpdateNoteDto
{
    /// <summary>
    /// Nội dung ghi chú
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập nội dung ghi chú")]
    public string Content { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự sắp xếp
    /// </summary>
    public int SortOrder { get; set; }
}
