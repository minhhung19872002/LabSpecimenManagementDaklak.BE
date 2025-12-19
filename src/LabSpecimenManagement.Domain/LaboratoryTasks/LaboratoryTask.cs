using System;
using System.Collections.Generic;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Công việc của khoa phòng - Laboratory Task
/// Được tạo tự động khi TestRequest được tạo, group theo Laboratory
/// </summary>
public class LaboratoryTask : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// TestRequest ID (Foreign Key)
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Laboratory ID (Foreign Key)
    /// </summary>
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number (copied from TestRequest)
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Tổng số mẫu gửi đến khoa phòng này
    /// Count of samples that have at least one indicator assigned to this lab
    /// </summary>
    public int TotalSampleCount { get; set; }

    /// <summary>
    /// ID Quản lý kỹ thuật - Technical Manager User ID (from Laboratory)
    /// </summary>
    public Guid? TechnicalManagerId { get; set; }

    /// <summary>
    /// Ngày chuyển giao - Transfer Date (from indicator group)
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành - Request Completion Date (from indicator group)
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    /// <summary>
    /// Ngày thí nghiệm - Test Date (entered by Technical Manager later)
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
    /// Navigation property to TestRequest
    /// </summary>
    public virtual TestRequest TestRequest { get; set; } = null!;

    /// <summary>
    /// Navigation property to Laboratory
    /// </summary>
    public virtual Laboratory Laboratory { get; set; } = null!;

    /// <summary>
    /// Collection of indicator IDs assigned to this lab for this task
    /// </summary>
    public virtual ICollection<LaboratoryTaskIndicator> Indicators { get; set; }

    protected LaboratoryTask()
    {
        Indicators = new List<LaboratoryTaskIndicator>();
    }

    public LaboratoryTask(
        Guid id,
        Guid testRequestId,
        Guid laboratoryId,
        string? contractNumber,
        int totalSampleCount,
        Guid? technicalManagerId,
        DateTime? transferDate,
        DateTime? requestCompletionDate) : base(id)
    {
        TestRequestId = testRequestId;
        LaboratoryId = laboratoryId;
        ContractNumber = contractNumber;
        TotalSampleCount = totalSampleCount;
        TechnicalManagerId = technicalManagerId;
        TransferDate = transferDate;
        RequestCompletionDate = requestCompletionDate;
        Status = LaboratoryTaskStatus.NotAssignedMethod;
        Indicators = new List<LaboratoryTaskIndicator>();
    }
}
