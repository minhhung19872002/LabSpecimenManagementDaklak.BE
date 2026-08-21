namespace LabSpecimenManagement.GoodsReceipts;

public enum GoodsReceiptOrderStatus
{
    PendingApproval = 0,
    Ordering = 1,
    WaitingReceipt = 2,
    Rejected = 3,
    Completed = 4,
    Cancelled = 5
}
