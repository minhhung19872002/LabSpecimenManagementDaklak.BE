using System;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.MaterialReceipts;
using LabSpecimenManagement.WarehouseImports;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.Products;

public abstract class ProductMaterialReceiptHistory_Tests<TStartupModule>
    : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IProductAppService _productAppService;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<MaterialReceipt, Guid> _materialReceiptRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _warehouseImportReceiptRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _warehouseImportItemRepository;

    protected ProductMaterialReceiptHistory_Tests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
        _productRepository = GetRequiredService<IRepository<Product, Guid>>();
        _materialReceiptRepository = GetRequiredService<IRepository<MaterialReceipt, Guid>>();
        _warehouseImportReceiptRepository = GetRequiredService<IRepository<WarehouseImportReceipt, Guid>>();
        _warehouseImportItemRepository = GetRequiredService<IRepository<WarehouseImportReceiptItem, Guid>>();
    }

    [Fact]
    public void Should_Calculate_Barcode_Transfer_For_Whole_And_Split_Quantities()
    {
        var wholeUnits = MaterialReceiptAppService.CalculateBarcodeTransferPlan(2m, 3m);
        wholeUnits.SourceBarcodesToMove.ShouldBe(2);
        wholeUnits.CreateSplitBarcode.ShouldBeFalse();
        wholeUnits.RequiredSourceBarcodeCount.ShouldBe(2);

        var splitWithRemainder = MaterialReceiptAppService.CalculateBarcodeTransferPlan(0.25m, 0.75m);
        splitWithRemainder.SourceBarcodesToMove.ShouldBe(0);
        splitWithRemainder.CreateSplitBarcode.ShouldBeTrue();
        splitWithRemainder.RequiredSourceBarcodeCount.ShouldBe(1);

        var splitTakingAllRemainder = MaterialReceiptAppService.CalculateBarcodeTransferPlan(0.25m, 0m);
        splitTakingAllRemainder.SourceBarcodesToMove.ShouldBe(1);
        splitTakingAllRemainder.CreateSplitBarcode.ShouldBeFalse();
        splitTakingAllRemainder.RequiredSourceBarcodeCount.ShouldBe(1);

        var wholeAndSplit = MaterialReceiptAppService.CalculateBarcodeTransferPlan(1.5m, 0.5m);
        wholeAndSplit.SourceBarcodesToMove.ShouldBe(1);
        wholeAndSplit.CreateSplitBarcode.ShouldBeTrue();
        wholeAndSplit.RequiredSourceBarcodeCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Group_And_Page_Material_Receipt_History_In_Database()
    {
        var product = new Product(Guid.NewGuid(), "PRODUCT-HISTORY-01", "Hóa chất test", ProductType.Chemical, null);
        await _productRepository.InsertAsync(product, autoSave: true);

        var materialReceipt = new MaterialReceipt(
            Guid.NewGuid(), "RO-HISTORY-01", "Người nhận", null, new DateTime(2026, 8, 8))
        {
            Status = MaterialReceiptStatus.Confirmed
        };
        await _materialReceiptRepository.InsertAsync(materialReceipt, autoSave: true);

        var warehouseId = Guid.NewGuid();
        var warehouseReceipt = new WarehouseImportReceipt(
            Guid.NewGuid(), materialReceipt.Code, WarehouseImportReceiptType.MaterialReceipt)
        {
            MaterialReceiptId = materialReceipt.Id,
            ActualImportDate = materialReceipt.ReceiptDate
        };
        await _warehouseImportReceiptRepository.InsertAsync(warehouseReceipt, autoSave: true);

        var importItem = new WarehouseImportReceiptItem(
            Guid.NewGuid(), warehouseReceipt.Id, product.Id, product.Name)
        {
            ActualReceivedQuantity = 2,
            ReceivedBaseQuantity = 2,
            StorageWarehouseId = warehouseId,
            StorageWarehouseName = "Kho nhận test",
            UnitName = "Chai",
            ManufacturerName = "Merck",
            LotNumber = "LOT-01",
            LotExpiryDate = new DateTime(2027, 8, 8),
            StockBeforeBaseQuantity = 3,
            StockAfterBaseQuantity = 5,
            BarcodeValues = JsonSerializer.Serialize(new[] { "BARCODE-01", "BARCODE-02" })
        };
        await _warehouseImportItemRepository.InsertAsync(importItem, autoSave: true);

        var result = await _productAppService.GetMaterialReceiptHistoryAsync(
            product.Id,
            new GetProductMaterialReceiptHistoryInput());

        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].ReceiptCode.ShouldBe(materialReceipt.Code);
        result.Items[0].ReceivedQuantity.ShouldBe(2);
        result.Items[0].Barcodes.Count.ShouldBe(2);
        result.Items[0].StockBeforeBaseQuantity.ShouldBe(3);
        result.Items[0].StockAfterBaseQuantity.ShouldBe(5);
    }
}
