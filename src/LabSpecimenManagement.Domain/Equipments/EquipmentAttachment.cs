using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Equipments;

public class EquipmentAttachment : Entity<Guid>
{
    public Guid EquipmentId { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public DateTime CreationTime { get; set; }

    protected EquipmentAttachment() { }

    public EquipmentAttachment(Guid id, Guid equipmentId, string fileId, string fileName, string filePath, long fileSize, string? fileType = null) : base(id)
    {
        EquipmentId = equipmentId;
        FileId = fileId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
        FileType = fileType;
        CreationTime = DateTime.UtcNow;
    }
}
