using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using Microsoft.AspNetCore.Hosting;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class MasterDataStatisticsAppService : ApplicationService, IMasterDataStatisticsAppService
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StorageLocation, Guid> _storageLocationRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IWebHostEnvironment _environment;

    public MasterDataStatisticsAppService(
        IRepository<Department, Guid> departmentRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Standard, Guid> standardRepository,
        IRepository<StorageLocation, Guid> storageLocationRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IWebHostEnvironment environment)
    {
        _departmentRepository = departmentRepository;
        _laboratoryRepository = laboratoryRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _testMethodRepository = testMethodRepository;
        _standardRepository = standardRepository;
        _storageLocationRepository = storageLocationRepository;
        _customerRepository = customerRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _indicatorRepository = indicatorRepository;
        _environment = environment;
    }

    public async Task<MasterDataStatisticsDto> GetStatisticsAsync()
    {
        var departmentCount = await _departmentRepository.CountAsync();
        var laboratoryCount = await _laboratoryRepository.CountAsync();
        var specimenTypeCount = await _specimenTypeRepository.CountAsync();
        var testMethodCount = await _testMethodRepository.CountAsync();
        var standardCount = await _standardRepository.CountAsync();
        var storageLocationCount = await _storageLocationRepository.CountAsync();
        var customerCount = await _customerRepository.CountAsync();
        var indicatorGroupCount = await _indicatorGroupRepository.CountAsync();
        var indicatorCount = await _indicatorRepository.CountAsync();

        // Count form templates from file system
        var formTemplateCount = 0;
        var templatesPath = Path.Combine(_environment.WebRootPath ?? "", "templates");
        if (Directory.Exists(templatesPath))
        {
            formTemplateCount = Directory.GetFiles(templatesPath)
                .Count(f => !f.Contains(".template.", StringComparison.OrdinalIgnoreCase));
        }

        return new MasterDataStatisticsDto
        {
            DepartmentCount = (int)departmentCount,
            LaboratoryCount = (int)laboratoryCount,
            SpecimenTypeCount = (int)specimenTypeCount,
            TestMethodCount = (int)testMethodCount,
            StandardCount = (int)standardCount,
            StorageLocationCount = (int)storageLocationCount,
            CustomerCount = (int)customerCount,
            IndicatorGroupCount = (int)indicatorGroupCount,
            IndicatorCount = (int)indicatorCount,
            FormTemplateCount = formTemplateCount
        };
    }
}
