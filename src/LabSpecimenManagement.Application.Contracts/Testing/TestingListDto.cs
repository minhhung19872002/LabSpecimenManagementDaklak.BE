using System;
using System.Collections.Generic;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Testing;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO for Testing list view (Danh sách công việc xét nghiệm - góc nhìn KTV)
/// </summary>
public class TestingListDto : EntityDto<Guid>
{
    /// <summary>
    /// LaboratoryTask ID
    /// </summary>
    public Guid LaboratoryTaskId { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Tổng số mẫu - Total Sample Count
    /// </summary>
    public int TotalSampleCount { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu - Total Indicator Count
    /// </summary>
    public int TotalIndicatorCount { get; set; }

    /// <summary>
    /// Danh sách tên Thử nghiệm viên (comma separated)
    /// </summary>
    public string? TechnicianNames { get; set; }

    /// <summary>
    /// Ngày chuyển giao - Transfer Date
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành - Request Completion Date
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    /// <summary>
    /// Ngày thí nghiệm - Test Date
    /// </summary>
    public DateTime? TestDate { get; set; }

    /// <summary>
    /// Trạng thái công việc xét nghiệm (góc nhìn KTV)
    /// </summary>
    public TestingStatus TestingStatus { get; set; }

    /// <summary>
    /// Khẩn cấp - nếu RequestCompletionDate - Today <= 5 ngày
    /// </summary>
    public bool IsUrgent { get; set; }

    /// <summary>
    /// Tên khoa phòng
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Tên khách hàng
    /// </summary>
    public string? CustomerName { get; set; }
}
