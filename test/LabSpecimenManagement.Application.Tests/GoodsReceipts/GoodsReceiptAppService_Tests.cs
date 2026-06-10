using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.Suppliers;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.GoodsReceipts;

public abstract class GoodsReceiptAppService_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;
    private readonly IRepository<Supplier, Guid> _supplierRepository;

    protected GoodsReceiptAppService_Tests()
    {
        _goodsReceiptAppService = GetRequiredService<IGoodsReceiptAppService>();
        _supplierRepository = GetRequiredService<IRepository<Supplier, Guid>>();
    }

    private async Task<Supplier> SeedSupplierAsync()
    {
        var supplier = new Supplier(Guid.NewGuid(), "SUP-TEST-001", "NCC Test")
        {
            Address = "123 Đường Test, TP.HCM",
            Phone = "0901234567",
            IsActive = true
        };
        return await _supplierRepository.InsertAsync(supplier, autoSave: true);
    }

    // ===== CREATE TESTS =====

    [Fact]
    public async Task Should_Create_GoodsReceipt_Without_Supplier()
    {
        var input = new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierAddress = "456 Đường ABC, Hà Nội",
            SupplierPhone = "0912345678",
            PurchaserName = "Nguyễn Văn A",
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        };

        var result = await _goodsReceiptAppService.CreateAsync(input);

        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Code.ShouldStartWith("PNH-");
        result.SupplierId.ShouldBeNull();
        result.SupplierAddress.ShouldBe("456 Đường ABC, Hà Nội");
        result.SupplierPhone.ShouldBe("0912345678");
        result.PurchaserName.ShouldBe("Nguyễn Văn A");
    }

    [Fact]
    public async Task Should_Create_GoodsReceipt_With_Supplier_And_Custom_Address()
    {
        var supplier = await SeedSupplierAsync();

        var input = new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierId = supplier.Id,
            SupplierAddress = "Địa chỉ tùy chỉnh",
            SupplierPhone = "0999888777",
            VatPercent = 10,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        };

        var result = await _goodsReceiptAppService.CreateAsync(input);

        result.ShouldNotBeNull();
        result.SupplierId.ShouldBe(supplier.Id);
        result.SupplierAddress.ShouldBe("Địa chỉ tùy chỉnh");
        result.SupplierPhone.ShouldBe("0999888777");
    }

    [Fact]
    public async Task Should_Create_GoodsReceipt_With_Null_Address_And_Phone()
    {
        var input = new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierAddress = null,
            SupplierPhone = null,
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        };

        var result = await _goodsReceiptAppService.CreateAsync(input);

        result.ShouldNotBeNull();
        result.SupplierAddress.ShouldBeNull();
        result.SupplierPhone.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Create_Equipment_Receipt_Without_Supplier()
    {
        var input = new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Equipment,
            SupplierAddress = "Địa chỉ thiết bị NCC",
            SupplierPhone = "0911222333",
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        };

        var result = await _goodsReceiptAppService.CreateAsync(input);

        result.ShouldNotBeNull();
        result.ReceiptType.ShouldBe(GoodsReceiptType.Equipment);
        result.SupplierId.ShouldBeNull();
        result.SupplierAddress.ShouldBe("Địa chỉ thiết bị NCC");
        result.SupplierPhone.ShouldBe("0911222333");
    }

    // ===== UPDATE TESTS =====

    [Fact]
    public async Task Should_Update_SupplierAddress_And_Phone()
    {
        var created = await _goodsReceiptAppService.CreateAsync(new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierAddress = "Địa chỉ ban đầu",
            SupplierPhone = "0900000000",
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        });

        var updateInput = new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierAddress = "Địa chỉ cập nhật",
            SupplierPhone = "0911111111",
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        };

        var result = await _goodsReceiptAppService.UpdateAsync(created.Id, updateInput);

        result.SupplierAddress.ShouldBe("Địa chỉ cập nhật");
        result.SupplierPhone.ShouldBe("0911111111");
    }

    [Fact]
    public async Task Should_Clear_SupplierAddress_On_Update()
    {
        var created = await _goodsReceiptAppService.CreateAsync(new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierAddress = "Có địa chỉ",
            SupplierPhone = "0901234567",
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        });

        var updateInput = new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierAddress = null,
            SupplierPhone = null,
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        };

        var result = await _goodsReceiptAppService.UpdateAsync(created.Id, updateInput);

        result.SupplierAddress.ShouldBeNull();
        result.SupplierPhone.ShouldBeNull();
    }

    // ===== GET / LIST TESTS =====

    [Fact]
    public async Task Should_Get_GoodsReceipt_With_SupplierInfo()
    {
        var created = await _goodsReceiptAppService.CreateAsync(new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            SupplierAddress = "Địa chỉ kiểm tra",
            SupplierPhone = "0922233344",
            PurchaserName = "Tester",
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        });

        var result = await _goodsReceiptAppService.GetAsync(created.Id);

        result.ShouldNotBeNull();
        result.SupplierAddress.ShouldBe("Địa chỉ kiểm tra");
        result.SupplierPhone.ShouldBe("0922233344");
    }

    [Fact]
    public async Task Should_Get_List()
    {
        await _goodsReceiptAppService.CreateAsync(new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        });

        var result = await _goodsReceiptAppService.GetListAsync(new GetGoodsReceiptListInput());

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    // ===== DELETE TESTS =====

    [Fact]
    public async Task Should_Delete_GoodsReceipt()
    {
        var created = await _goodsReceiptAppService.CreateAsync(new CreateUpdateGoodsReceiptDto
        {
            ReceiptType = GoodsReceiptType.Goods,
            VatPercent = 8,
            Items = new List<CreateUpdateGoodsReceiptItemDto>()
        });

        await Should.NotThrowAsync(async () =>
        {
            await _goodsReceiptAppService.DeleteAsync(created.Id);
        });
    }
}
