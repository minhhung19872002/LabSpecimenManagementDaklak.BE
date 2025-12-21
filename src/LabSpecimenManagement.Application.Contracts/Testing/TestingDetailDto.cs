using System;
using System.Collections.Generic;
using LabSpecimenManagement.LaboratoryTasks;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO for Testing detail view
/// </summary>
public class TestingDetailDto : EntityDto<Guid>
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
    /// Trạng thái công việc xét nghiệm
    /// </summary>
    public TestingStatus TestingStatus { get; set; }

    /// <summary>
    /// Danh sách tên kỹ thuật viên (comma separated)
    /// </summary>
    public string? TechnicianNames { get; set; }

    /// <summary>
    /// Tên khoa phòng
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Laboratory ID
    /// </summary>
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    /// <summary>
    /// Tên khách hàng
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// ID Quản lý kỹ thuật
    /// </summary>
    public Guid? TechnicalManagerId { get; set; }

    /// <summary>
    /// Tên Quản lý kỹ thuật
    /// </summary>
    public string? TechnicalManagerName { get; set; }

    /// <summary>
    /// Số chỉ tiêu đã hoàn thành (Approved)
    /// </summary>
    public int CompletedIndicatorCount { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu
    /// </summary>
    public int TotalIndicatorCount { get; set; }

    /// <summary>
    /// Danh sách mẫu và chỉ tiêu (grouped by sample)
    /// </summary>
    public List<TestingSampleDto> Samples { get; set; } = new();
}

/// <summary>
/// DTO for sample in Testing detail
/// </summary>
public class TestingSampleDto
{
    /// <summary>
    /// Sample ID (TestRequestSample ID)
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Mã mẫu - Sample Code
    /// </summary>
    public string? SampleCode { get; set; }

    /// <summary>
    /// Tên mẫu - Sample Name
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Tình trạng mẫu - Status Text
    /// </summary>
    public string? StatusText { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu của mẫu
    /// </summary>
    public List<TestingIndicatorDto> Indicators { get; set; } = new();
}

/// <summary>
/// DTO for indicator in Testing detail
/// </summary>
public class TestingIndicatorDto
{
    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TestRequestSampleIndicator ID
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

    /// <summary>
    /// Indicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu - Indicator Name
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// ID phương pháp - Method ID
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// Tên phương pháp - Method Name
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Đơn vị tính - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Kết quả - Result
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Ngày nhập kết quả - Entry Date
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// Ngày giao việc - Assigned Date
    /// </summary>
    public DateTime? AssignedDate { get; set; }

    /// <summary>
    /// Ngày hoàn thành (ngày QL duyệt) - Approval Date
    /// </summary>
    public DateTime? CompletionDate { get; set; }

    /// <summary>
    /// Trạng thái duyệt - Approval Status
    /// </summary>
    public IndicatorApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Lý do từ chối mới nhất - Latest Rejection Reason
    /// </summary>
    public string? LatestRejectionReason { get; set; }

    /// <summary>
    /// Số lượng lịch sử từ chối
    /// </summary>
    public int RejectionHistoryCount { get; set; }

    /// <summary>
    /// ID kỹ thuật viên
    /// </summary>
    public Guid? TechnicianId { get; set; }

    /// <summary>
    /// Tên kỹ thuật viên
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// Có thể chỉnh sửa hay không (dựa vào ApprovalStatus và quyền)
    /// </summary>
    public bool CanEdit { get; set; }
}
