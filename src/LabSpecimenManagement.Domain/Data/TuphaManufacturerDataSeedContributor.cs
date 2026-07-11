using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace LabSpecimenManagement.Data;

public class TuphaManufacturerDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string TuphaName = "Tự pha";

    private readonly IRepository<Manufacturer, System.Guid> _manufacturerRepository;
    private readonly IGuidGenerator _guidGenerator;

    public TuphaManufacturerDataSeedContributor(
        IRepository<Manufacturer, System.Guid> manufacturerRepository,
        IGuidGenerator guidGenerator)
    {
        _manufacturerRepository = manufacturerRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var existing = await _manufacturerRepository.FindAsync(x => x.Name == TuphaName);
        if (existing != null)
        {
            if (!existing.IsSystem)
            {
                existing.IsSystem = true;
                await _manufacturerRepository.UpdateAsync(existing);
            }
            return;
        }

        // Tạo mới với code HSX-TPA cố định
        var manufacturer = new Manufacturer(_guidGenerator.Create(), "HSX-TPA", TuphaName)
        {
            IsSystem = true,
            IsActive = true
        };

        await _manufacturerRepository.InsertAsync(manufacturer);
    }
}
