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
