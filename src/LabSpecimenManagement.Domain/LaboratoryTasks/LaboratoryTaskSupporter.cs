using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Người hỗ trợ QLKT cho một LaboratoryTask
/// Supporter có quyền như QLKT (duyệt KQ, gửi KQ, phân công)
/// </summary>
public class LaboratoryTaskSupporter : Entity<Guid>, IHasCreationTime, IMayHaveCreator
{
    /// <summary>
    /// LaboratoryTask ID (Foreign Key)
    /// </summary>
    public Guid LaboratoryTaskId { get; set; }

    /// <summary>
    /// User ID của người hỗ trợ
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// ID người tạo (QLKT gốc thêm supporter)
    /// </summary>
    public Guid? CreatorId { get; set; }

    /// <summary>
    /// Navigation property to LaboratoryTask
    /// </summary>
    public virtual LaboratoryTask LaboratoryTask { get; set; } = null!;

    protected LaboratoryTaskSupporter()
    {
    }

    public LaboratoryTaskSupporter(
        Guid id,
        Guid laboratoryTaskId,
        Guid userId) : base(id)
    {
        LaboratoryTaskId = laboratoryTaskId;
        UserId = userId;
        CreationTime = DateTime.UtcNow;
    }
}
