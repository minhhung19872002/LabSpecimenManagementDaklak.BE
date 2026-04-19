using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.GoodsReceipts;

public class GoodsReceiptItem : Entity<Guid>
{
    public Guid GoodsReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid UnitId { get; set; }
    public string UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal AmountBeforeVat { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AmountAfterVat { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }

    protected GoodsReceiptItem()
    {
    }

    public GoodsReceiptItem(
        Guid id,
        Guid goodsReceiptId,
        Guid productId,
        string productName,
        Guid unitId,
        string unitName) : base(id)
    {
        GoodsReceiptId = goodsReceiptId;
        ProductId = productId;
        ProductName = productName;
        UnitId = unitId;
        UnitName = unitName;
    }

    public void Calculate()
    {
        AmountBeforeVat = Quantity * UnitPrice;
        VatAmount = AmountBeforeVat * VatPercent / 100;
        AmountAfterVat = AmountBeforeVat + VatAmount;
    }
}
