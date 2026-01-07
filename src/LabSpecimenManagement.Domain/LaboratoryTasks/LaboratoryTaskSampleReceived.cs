using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Lưu trạng thái KTV đã nhận mẫu theo (LaboratoryTaskId, SampleId)
/// KTV phải nhận mẫu trước khi có thể thực hiện thí nghiệm
/// </summary>
public class LaboratoryTaskSampleReceived : Entity<Guid>
{
    /// <summary>
    /// LaboratoryTask ID (Foreign Key)
    /// </summary>
    public Guid LaboratoryTaskId { get; set; }

    /// <summary>
    /// TestRequestSample ID (Foreign Key)
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Ngày nhận mẫu
    /// </summary>
    public DateTime ReceivedDate { get; set; }

    /// <summary>
    /// ID KTV nhận mẫu
    /// </summary>
    public Guid ReceivedByUserId { get; set; }

    /// <summary>
    /// Tên KTV nhận mẫu
    /// </summary>
    [MaxLength(256)]
    public string? ReceivedByUserName { get; set; }

    /// <summary>
    /// Navigation property to LaboratoryTask
    /// </summary>
    public virtual LaboratoryTask LaboratoryTask { get; set; } = null!;

    /// <summary>
    /// Navigation property to TestRequestSample
    /// </summary>
    public virtual TestRequestSample Sample { get; set; } = null!;

    protected LaboratoryTaskSampleReceived()
    {
    }

    public LaboratoryTaskSampleReceived(
        Guid id,
        Guid laboratoryTaskId,
        Guid sampleId,
        DateTime receivedDate,
        Guid receivedByUserId,
        string? receivedByUserName) : base(id)
    {
        LaboratoryTaskId = laboratoryTaskId;
        SampleId = sampleId;
        ReceivedDate = receivedDate;
        ReceivedByUserId = receivedByUserId;
        ReceivedByUserName = receivedByUserName;
    }
}
