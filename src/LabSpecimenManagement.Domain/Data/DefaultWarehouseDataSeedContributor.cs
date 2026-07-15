using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace LabSpecimenManagement.Data;

public class DefaultWarehouseDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IGuidGenerator _guidGenerator;

    public DefaultWarehouseDataSeedContributor(
        IRepository<Warehouse, Guid> warehouseRepository,
        IGuidGenerator guidGenerator)
    {
        _warehouseRepository = warehouseRepository;
        _guidGenerator = guidGenerator;
    }

    private const string DefaultWarehouseName = "Kho Trung Tâm Kiểm Soát";
    private const string LegacyWarehouseName = "Kho Tổng Vật Tư";

    public async Task SeedAsync(DataSeedContext context)
    {
        var query = await _warehouseRepository.GetQueryableAsync();

        var legacy = query.FirstOrDefault(x => x.Name == LegacyWarehouseName && x.IsDefault);
        if (legacy != null)
            await _warehouseRepository.DeleteAsync(legacy);

        var target = query.FirstOrDefault(x => x.Name == DefaultWarehouseName);
        if (target != null)
        {
            if (!target.IsDefault || target.ParentWarehouseId != null)
            {
                target.IsDefault = true;
                target.Level = 0;
                target.ParentWarehouseId = null;
                await _warehouseRepository.UpdateAsync(target);
            }
            return;
        }

        var codes = query.Where(x => x.Code.StartsWith("KH-")).Select(x => x.Code).ToList();
        int max = 0;
        foreach (var code in codes)
        {
            var part = code.Replace("KH-", "");
            if (int.TryParse(part, out var n) && n > max) max = n;
        }

        await _warehouseRepository.InsertAsync(new Warehouse(_guidGenerator.Create(), $"KH-{(max + 1):D3}", DefaultWarehouseName)
        {
            IsDefault = true,
            IsActive = true
        });
    }
}
