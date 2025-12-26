using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO for Laboratory Task (Công việc khoa phòng)
/// </summary>
public class LaboratoryTaskDto : FullAuditedEntityDto<Guid>
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Số phiếu yêu cầu - Test Request Number
    /// </summary>
    public string? TestRequestNumber { get; set; }

    /// <summary>
    /// Laboratory ID
    /// </summary>
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// Mã khoa phòng - Laboratory Code
    /// </summary>
    public string? LaboratoryCode { get; set; }

    /// <summary>
    /// Tên khoa phòng - Laboratory Name
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Tổng số mẫu - Total Sample Count
    /// </summary>
    public int TotalSampleCount { get; set; }

    /// <summary>
    /// ID Quản lý kỹ thuật - Technical Manager ID
    /// </summary>
    public Guid? TechnicalManagerId { get; set; }

    /// <summary>
    /// Tên Quản lý kỹ thuật - Technical Manager Name
    /// </summary>
    public string? TechnicalManagerName { get; set; }

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
    /// Trạng thái - Status
    /// </summary>
    public LaboratoryTaskStatus Status { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Lý do QLCL từ chối kết quả
    /// </summary>
    public string? ResultRejectionReason { get; set; }

    /// <summary>
    /// Tên khách hàng - Customer Name (from TestRequest)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu - Indicators
    /// </summary>
    public List<LaboratoryTaskIndicatorDto> Indicators { get; set; } = new();
}

/// <summary>
/// DTO for Laboratory Task Indicator (Chỉ tiêu trong công việc khoa phòng)
/// </summary>
public class LaboratoryTaskIndicatorDto
{
    /// <summary>
    /// ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TestRequestSampleIndicator ID
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

    /// <summary>
    /// Sample ID
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
    /// Indicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Mã chỉ tiêu - Indicator Code
    /// </summary>
    public string? IndicatorCode { get; set; }

    /// <summary>
    /// Tên chỉ tiêu - Indicator Name
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// Tên nhóm chỉ tiêu - Indicator Group Name
    /// </summary>
    public string? IndicatorGroupName { get; set; }

    /// <summary>
    /// Đơn giá - Unit Price
    /// </summary>
    public decimal UnitPrice { get; set; }

    // ====== Phase 6 - Workflow Fields ======

    /// <summary>
    /// ID phương pháp thử nghiệm - Method ID
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// Tên phương pháp - Method Name
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// ID kỹ thuật viên - Technician ID
    /// </summary>
    public Guid? TechnicianId { get; set; }

    /// <summary>
    /// Tên kỹ thuật viên - Technician Name
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// Kết quả thí nghiệm - Result
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Đơn vị tính - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Ngày nhập kết quả - Entry Date
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// ID người nhập - Entered By ID
    /// </summary>
    public Guid? EnteredById { get; set; }

    /// <summary>
    /// Tên người nhập - Entered By Name
    /// </summary>
    public string? EnteredByName { get; set; }

    /// <summary>
    /// Trạng thái duyệt - Approval Status
    /// </summary>
    public IndicatorApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Ngày duyệt - Approval Date
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// ID người duyệt - Approved By ID
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Tên người duyệt - Approved By Name
    /// </summary>
    public string? ApprovedByName { get; set; }

    /// <summary>
    /// Lý do từ chối - Rejection Reason
    /// </summary>
    public string? RejectionReason { get; set; }
}
