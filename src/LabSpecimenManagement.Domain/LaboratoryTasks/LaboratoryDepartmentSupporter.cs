using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Người hỗ trợ khoa phòng - có quyền truy cập TẤT CẢ công việc trong khoa phòng
/// Khác với LaboratoryTaskSupporter (per-task), entity này scope theo LaboratoryId
/// Có thời hạn (ExpirationDate), tự động xóa bằng cronjob khi hết hạn
/// </summary>
public class LaboratoryDepartmentSupporter : Entity<Guid>, IHasCreationTime, IMayHaveCreator
{
    /// <summary>
    /// Laboratory ID - khoa phòng (Foreign Key)
    /// </summary>
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// User ID của người hỗ trợ
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Thời gian hết hạn (UTC).
    /// Ví dụ: chọn ngày 13 → hết hạn 00:00 ngày 14 Vietnam TZ → convert sang UTC
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// ID người tạo (QLKT thêm supporter)
    /// </summary>
    public Guid? CreatorId { get; set; }

    protected LaboratoryDepartmentSupporter()
    {
    }

    public LaboratoryDepartmentSupporter(
        Guid id,
        Guid laboratoryId,
        Guid userId,
        DateTime expirationDate) : base(id)
    {
        LaboratoryId = laboratoryId;
        UserId = userId;
        ExpirationDate = expirationDate;
        CreationTime = DateTime.UtcNow;
    }
}
