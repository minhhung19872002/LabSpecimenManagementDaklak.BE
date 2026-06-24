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

    private const string DefaultWarehouseName = "Kho Tổng Vật Tư";

    public async Task SeedAsync(DataSeedContext context)
    {
        var query = await _warehouseRepository.GetQueryableAsync();
        var hasDefault = query.Any(x => x.IsDefault);
        if (hasDefault)
        {
            return;
        }

        var existing = query.FirstOrDefault(x => x.Name == DefaultWarehouseName);
        if (existing != null)
        {
            existing.IsDefault = true;
            await _warehouseRepository.UpdateAsync(existing);
            return;
        }

        var codeParts = query
            .Where(x => x.Code.StartsWith("KH-"))
            .Select(x => x.Code)
            .ToList();

        int maxNumber = 0;
        foreach (var code in codeParts)
        {
            var numberPart = code.Replace("KH-", "");
            if (int.TryParse(numberPart, out var num) && num > maxNumber)
            {
                maxNumber = num;
            }
        }

        var nextCode = $"KH-{(maxNumber + 1):D3}";

        var warehouse = new Warehouse(_guidGenerator.Create(), nextCode, DefaultWarehouseName)
        {
            IsDefault = true,
            IsActive = true
        };

        await _warehouseRepository.InsertAsync(warehouse);
    }
}
