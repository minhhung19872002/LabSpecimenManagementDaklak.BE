using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData.Notes;

/// <summary>
/// DTO tạo Ghi chú mới (Code sẽ được tự động sinh)
/// </summary>
public class CreateNoteDto
{
    /// <summary>
    /// Nội dung ghi chú
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập nội dung ghi chú")]
    [StringLength(1000, ErrorMessage = "Nội dung ghi chú không được vượt quá 1000 ký tự")]
    public string Content { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Thứ tự sắp xếp
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
