using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MaterialReceipts;

public class MaterialReceipt : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; }
    public string ReceiverName { get; set; }
    public Guid? ReceiverId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public MaterialReceiptStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid? SyntheticWarehouseReceiptId { get; set; }

    public ICollection<MaterialReceiptItem> Items { get; set; }

    protected MaterialReceipt()
    {
        Items = new List<MaterialReceiptItem>();
    }

    public MaterialReceipt(
        Guid id,
        string code,
        string receiverName,
        Guid? receiverId,
        DateTime receiptDate) : base(id)
    {
        Code = code;
        ReceiverName = receiverName;
        ReceiverId = receiverId;
        ReceiptDate = receiptDate;
        Status = MaterialReceiptStatus.Draft;
        Items = new List<MaterialReceiptItem>();
    }
}
