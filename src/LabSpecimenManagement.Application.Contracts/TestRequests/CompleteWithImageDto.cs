using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO hoàn thành phiếu yêu cầu với hình ảnh gửi bưu điện
/// </summary>
public class CompleteWithImageDto
{
    /// <summary>
    /// Danh sách FileId của hình ảnh đã upload (tối đa 5 hình)
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 hình ảnh")]
    [MaxLength(5, ErrorMessage = "Tối đa 5 hình ảnh")]
    public List<string> ImageFileIds { get; set; } = new();
}
