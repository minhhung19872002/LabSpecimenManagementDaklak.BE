using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Specimens;

/// <summary>
/// DTO để tạo nhiều mẫu cùng lúc cho một phiếu yêu cầu
/// </summary>
public class CreateManySpecimensDto
{
    /// <summary>
    /// ID của phiếu yêu cầu thử nghiệm (chung cho tất cả mẫu)
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Danh sách các mẫu cần tạo
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Cần ít nhất 1 mẫu")]
    public List<SpecimenItemDto> Specimens { get; set; } = new();

    /// <summary>
    /// Vị trí lưu kho (chung cho tất cả mẫu, tùy chọn)
    /// </summary>
    [StringLength(256)]
    public string? StorageLocation { get; set; }

    /// <summary>
    /// Điều kiện bảo quản (chung cho tất cả mẫu, tùy chọn)
    /// </summary>
    [StringLength(256)]
    public string? StorageCondition { get; set; }

    /// <summary>
    /// Mô tả/ghi chú chung
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }
}

/// <summary>
/// Thông tin chi tiết của từng mẫu
/// </summary>
public class SpecimenItemDto
{
    /// <summary>
    /// Mã mẫu (sẽ được tự sinh nếu để trống)
    /// </summary>
    [StringLength(50)]
    public string? Code { get; set; }

    /// <summary>
    /// Tên mẫu
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Loại mẫu
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Ngày nhận mẫu
    /// </summary>
    [Required]
    public DateTime ReceivedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Số lượng
    /// </summary>
    [Required]
    [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Đơn vị tính
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Unit { get; set; } = "mẫu";

    /// <summary>
    /// Trạng thái ban đầu (mặc định: Received)
    /// </summary>
    public SpecimenStatus Status { get; set; } = SpecimenStatus.Received;
}

/// <summary>
/// Kết quả tạo nhiều mẫu
/// </summary>
public class CreateManySpecimensResultDto
{
    /// <summary>
    /// Tổng số mẫu được yêu cầu tạo
    /// </summary>
    public int TotalRequested { get; set; }

    /// <summary>
    /// Số mẫu tạo thành công
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Số mẫu tạo thất bại
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Danh sách các mẫu đã tạo thành công
    /// </summary>
    public List<SpecimenDto> CreatedSpecimens { get; set; } = new();

    /// <summary>
    /// Danh sách lỗi (nếu có)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Trạng thái tổng thể
    /// </summary>
    public bool Success => FailedCount == 0;
}
