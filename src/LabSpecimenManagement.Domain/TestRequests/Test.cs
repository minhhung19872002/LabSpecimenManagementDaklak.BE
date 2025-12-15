using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Phép thử - Test
/// ISO/IEC 17025:2017 compliant
/// </summary>
public class Test : FullAuditedAggregateRoot<Guid>
{
    public Guid SpecimenId { get; set; }

    /// <summary>
    /// Mã phép thử - Test Code (auto-generated)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên phép thử - Test Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Phương pháp thử - Test Method
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// Tiêu chuẩn - Standard (e.g., TCVN, ISO, ASTM)
    /// </summary>
    public string Standard { get; set; }

    /// <summary>
    /// Trạng thái - Status (Pending, Assigned, InProgress, Completed, Approved)
    /// </summary>
    public TestStatus Status { get; set; }

    /// <summary>
    /// Người được giao - Assignee User ID
    /// </summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>
    /// Ngày giao việc - Assignment Date
    /// </summary>
    public DateTime? AssignmentDate { get; set; }

    /// <summary>
    /// Ngày bắt đầu - Start Date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Ngày hoàn thành - Completion Date
    /// </summary>
    public DateTime? CompletionDate { get; set; }

    /// <summary>
    /// Thời gian dự kiến (giờ) - Estimated duration in hours
    /// </summary>
    public decimal? EstimatedDuration { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    public virtual Specimen? Specimen { get; set; }

    /// <summary>
    /// Kết quả thử nghiệm - Test results
    /// </summary>
    public virtual ICollection<TestResult> Results { get; set; }

    protected Test()
    {
        Results = new List<TestResult>();
    }

    public Test(
        Guid id,
        Guid specimenId,
        string code,
        string name,
        string method,
        string standard
    ) : base(id)
    {
        SpecimenId = specimenId;
        Code = code;
        Name = name;
        Method = method;
        Standard = standard;
        Status = TestStatus.Pending;
        Results = new List<TestResult>();
    }
}
