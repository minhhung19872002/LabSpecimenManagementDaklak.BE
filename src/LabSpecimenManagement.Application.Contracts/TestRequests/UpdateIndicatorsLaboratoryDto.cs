using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO để cập nhật LaboratoryId và RequestDate cho từng chỉ tiêu
/// </summary>
public class UpdateIndicatorLaboratoryDto
{
    /// <summary>
    /// TestRequestSampleIndicator ID (SampleIndicatorId)
    /// </summary>
    [Required]
    public Guid SampleIndicatorId { get; set; }

    /// <summary>
    /// Khoa phòng thực hiện
    /// </summary>
    [Required]
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành (optional - nếu không gửi sẽ giữ nguyên giá trị cũ)
    /// </summary>
    public DateTime? RequestDate { get; set; }
}

/// <summary>
/// Input để cập nhật LaboratoryId cho nhiều chỉ tiêu cùng lúc
/// </summary>
public class UpdateIndicatorsLaboratoryInput
{
    /// <summary>
    /// Danh sách chỉ tiêu cần cập nhật
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 chỉ tiêu")]
    public List<UpdateIndicatorLaboratoryDto> Indicators { get; set; } = new();
}
