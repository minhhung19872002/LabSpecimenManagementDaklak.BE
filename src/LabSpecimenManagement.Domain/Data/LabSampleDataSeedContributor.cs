using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.Data;

/// <summary>
/// Seed sample data for Lab Specimen Management System
/// Based on frontend mock data
/// </summary>
public class LabSampleDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<StorageLocation, Guid> _storageLocationRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _testRequestSampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _testRequestSampleIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<Specimen, Guid> _specimenRepository;
    private readonly IRepository<Test, Guid> _testRepository;
    private readonly IRepository<TestResult, Guid> _testResultRepository;
    private readonly IRepository<NCR, Guid> _ncrRepository;
    private readonly IRepository<TaskAssignment, Guid> _taskAssignmentRepository;
    private readonly IRepository<Organization, Guid> _organizationRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly LabUsersDataSeedContributor _labUsersDataSeedContributor;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IConfiguration _configuration;

    public LabSampleDataSeedContributor(
        IRepository<Department, Guid> departmentRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<StorageLocation, Guid> storageLocationRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> testRequestSampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> testRequestSampleIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IRepository<TestResult, Guid> testResultRepository,
        IRepository<NCR, Guid> ncrRepository,
        IRepository<TaskAssignment, Guid> taskAssignmentRepository,
        IRepository<Organization, Guid> organizationRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IIdentityUserRepository userRepository,
        LabUsersDataSeedContributor labUsersDataSeedContributor,
        IGuidGenerator guidGenerator,
        IConfiguration configuration)
    {
        _departmentRepository = departmentRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _testMethodRepository = testMethodRepository;
        _storageLocationRepository = storageLocationRepository;
        _customerRepository = customerRepository;
        _testRequestRepository = testRequestRepository;
        _testRequestSampleRepository = testRequestSampleRepository;
        _testRequestSampleIndicatorRepository = testRequestSampleIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _testResultRepository = testResultRepository;
        _ncrRepository = ncrRepository;
        _taskAssignmentRepository = taskAssignmentRepository;
        _organizationRepository = organizationRepository;
        _laboratoryRepository = laboratoryRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _userRepository = userRepository;
        _labUsersDataSeedContributor = labUsersDataSeedContributor;
        _guidGenerator = guidGenerator;
        _configuration = configuration;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // Skip seeding in test environment
        var skipSeeding = _configuration.GetValue<bool>("SkipLabSampleSeed", false);
        if (skipSeeding)
        {
            return;
        }

        await SeedDepartmentsAsync();
        await SeedSpecimenTypesAsync();
        await SeedTestMethodsAsync();
        await SeedStorageLocationsAsync();
        await SeedCustomersAsync();
        await SeedOrganizationsAsync();
        await SeedLaboratoriesAsync();
        await SeedIndicatorGroupsAndIndicatorsAsync();
        await SeedTestMethodIndicatorsAsync();
        await SeedStandardsAsync();
        await SeedStandardIndicatorsAsync();
        // await SeedTestRequestsAndRelatedDataAsync(); // Bỏ seeder phiếu yêu cầu
    }

    private async Task SeedIndicatorGroupsAndIndicatorsAsync()
    {
        if (await _indicatorGroupRepository.GetCountAsync() > 0) return;

        var now = DateTime.Now;

        // Seed Indicator Groups
        var groupViSinh = new IndicatorGroup(_guidGenerator.Create(), "Vi sinh");
        groupViSinh.IsActive = true;
        await _indicatorGroupRepository.InsertAsync(groupViSinh, autoSave: true);

        var groupHoaLy = new IndicatorGroup(_guidGenerator.Create(), "Hóa lý");
        groupHoaLy.IsActive = true;
        await _indicatorGroupRepository.InsertAsync(groupHoaLy, autoSave: true);

        var groupKimLoai = new IndicatorGroup(_guidGenerator.Create(), "Kim loại nặng");
        groupKimLoai.IsActive = true;
        await _indicatorGroupRepository.InsertAsync(groupKimLoai, autoSave: true);

        var groupThuocBVTV = new IndicatorGroup(_guidGenerator.Create(), "Dư lượng thuốc BVTV");
        groupThuocBVTV.IsActive = true;
        await _indicatorGroupRepository.InsertAsync(groupThuocBVTV, autoSave: true);

        // Seed Indicators
        var indicators = new[]
        {
            // Vi sinh
            new { GroupId = groupViSinh.Id, Code = "VS-001", Name = "Tổng vi khuẩn hiếu khí", Price = 150000m, SortOrder = 1 },
            new { GroupId = groupViSinh.Id, Code = "VS-002", Name = "Coliform tổng số", Price = 120000m, SortOrder = 2 },
            new { GroupId = groupViSinh.Id, Code = "VS-003", Name = "E.coli", Price = 180000m, SortOrder = 3 },
            new { GroupId = groupViSinh.Id, Code = "VS-004", Name = "Salmonella", Price = 250000m, SortOrder = 4 },
            new { GroupId = groupViSinh.Id, Code = "VS-005", Name = "Staphylococcus aureus", Price = 200000m, SortOrder = 5 },

            // Hóa lý
            new { GroupId = groupHoaLy.Id, Code = "HL-001", Name = "pH", Price = 50000m, SortOrder = 1 },
            new { GroupId = groupHoaLy.Id, Code = "HL-002", Name = "Độ ẩm", Price = 80000m, SortOrder = 2 },
            new { GroupId = groupHoaLy.Id, Code = "HL-003", Name = "Độ tro", Price = 100000m, SortOrder = 3 },
            new { GroupId = groupHoaLy.Id, Code = "HL-004", Name = "Protein", Price = 150000m, SortOrder = 4 },
            new { GroupId = groupHoaLy.Id, Code = "HL-005", Name = "Chất béo", Price = 120000m, SortOrder = 5 },
            new { GroupId = groupHoaLy.Id, Code = "HL-006", Name = "Caffeine", Price = 200000m, SortOrder = 6 },
            new { GroupId = groupHoaLy.Id, Code = "HL-007", Name = "Nitơ tổng", Price = 180000m, SortOrder = 7 },

            // Kim loại nặng
            new { GroupId = groupKimLoai.Id, Code = "KL-001", Name = "Chì (Pb)", Price = 250000m, SortOrder = 1 },
            new { GroupId = groupKimLoai.Id, Code = "KL-002", Name = "Cadmium (Cd)", Price = 250000m, SortOrder = 2 },
            new { GroupId = groupKimLoai.Id, Code = "KL-003", Name = "Thủy ngân (Hg)", Price = 300000m, SortOrder = 3 },
            new { GroupId = groupKimLoai.Id, Code = "KL-004", Name = "Asen (As)", Price = 280000m, SortOrder = 4 },

            // BVTV
            new { GroupId = groupThuocBVTV.Id, Code = "BVTV-001", Name = "Dư lượng Chlorpyrifos", Price = 350000m, SortOrder = 1 },
            new { GroupId = groupThuocBVTV.Id, Code = "BVTV-002", Name = "Dư lượng Cypermethrin", Price = 350000m, SortOrder = 2 },
            new { GroupId = groupThuocBVTV.Id, Code = "BVTV-003", Name = "Dư lượng Glyphosate", Price = 400000m, SortOrder = 3 },
        };

        foreach (var ind in indicators)
        {
            var indicator = new Indicator(_guidGenerator.Create(), ind.Code, ind.Name, ind.GroupId);
            indicator.SortOrder = ind.SortOrder;
            indicator.IsActive = true;
            await _indicatorRepository.InsertAsync(indicator, autoSave: true);
        }
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _departmentRepository.GetCountAsync() > 0) return;

        var departments = new[]
        {
            new { Code = "QL", Name = "Quản lý" },
            new { Code = "BGD", Name = "Ban Giám đốc" },
            new { Code = "PHH", Name = "Phòng Hóa học" },
            new { Code = "PVS", Name = "Phòng Vi sinh" },
            new { Code = "PVL", Name = "Phòng Vật lý" },
            new { Code = "PMT", Name = "Phòng Môi trường" },
            new { Code = "TNM", Name = "Tiếp nhận mẫu" },
        };

        foreach (var dept in departments)
        {
            var entity = new Department(_guidGenerator.Create(), dept.Code, dept.Name);
            await _departmentRepository.InsertAsync(entity);
        }
    }

    private async Task SeedSpecimenTypesAsync()
    {
        if (await _specimenTypeRepository.GetCountAsync() > 0) return;

        var types = new[]
        {
            new { Code = "NN", Name = "Nước ngầm", Desc = "Mẫu nước ngầm" },
            new { Code = "NM", Name = "Nước mặt", Desc = "Mẫu nước mặt" },
            new { Code = "NT", Name = "Nước thải", Desc = "Mẫu nước thải" },
            new { Code = "DAT", Name = "Đất", Desc = "Mẫu đất các loại" },
            new { Code = "TP", Name = "Thực phẩm", Desc = "Mẫu thực phẩm" },
            new { Code = "PB", Name = "Phân bón", Desc = "Mẫu phân bón" },
            new { Code = "CP", Name = "Cà phê", Desc = "Mẫu cà phê" },
            new { Code = "RC", Name = "Rau củ", Desc = "Mẫu rau củ quả" },
        };

        foreach (var type in types)
        {
            var entity = new SpecimenType(_guidGenerator.Create(), type.Code, type.Name);
            entity.Description = type.Desc;
            await _specimenTypeRepository.InsertAsync(entity);
        }
    }

    private async Task SeedTestMethodsAsync()
    {
        // Skip if already seeded
        if (await _testMethodRepository.GetCountAsync() > 0) return;

        var methods = new[]
        {
            new { Code = "PP241224001", Name = "TCVN 6492:2011 - Xác định pH", Unit = "" },
            new { Code = "PP241224002", Name = "TCVN 4559:1998 - Xác định EC", Unit = "mS/cm" },
            new { Code = "PP241224003", Name = "TCVN 6638:2000 - Xác định Nitơ tổng", Unit = "mg/L" },
            new { Code = "PP241224004", Name = "TCVN 6187-1:2019 - Xác định Coliform", Unit = "CFU/100mL" },
            new { Code = "PP241224005", Name = "AOAC 950.40 - Xác định Caffeine", Unit = "%" },
            new { Code = "PP241224006", Name = "TCVN 6193:1996 - Xác định kim loại nặng", Unit = "mg/L" },
            new { Code = "PP241224007", Name = "TCVN 5979:2007 - Xác định pH đất", Unit = "" },
            new { Code = "PP241224008", Name = "TCVN 6498:1999 - Xác định Nitơ tổng đất", Unit = "%" },
            new { Code = "PP241224009", Name = "TCVN 6928:2007 - Xác định độ ẩm cà phê", Unit = "%" },
            new { Code = "PP241224010", Name = "TCVN 4325:2007 - Xác định chất béo", Unit = "%" },
            new { Code = "PP241224011", Name = "TCVN 5501:2010 - Xác định protein", Unit = "%" },
            new { Code = "PP241224012", Name = "ISO 17025:2017 - Phương pháp chuẩn PTN", Unit = "mg/L" },
            new { Code = "PP241224013", Name = "TCVN 7538-1:2006 - Xác định độ ẩm đất", Unit = "%" },
            new { Code = "PP241224014", Name = "SMEWW 4500-NO3 - Xác định Nitrat", Unit = "mg/L" },
            new { Code = "PP241224015", Name = "SMEWW 5210B - Xác định BOD5", Unit = "mg/L" },
            new { Code = "PP241224016", Name = "SMEWW 5220C - Xác định COD", Unit = "mg/L" },
        };

        foreach (var method in methods)
        {
            var entity = new TestMethod(_guidGenerator.Create(), method.Code, method.Name, method.Unit);
            await _testMethodRepository.InsertAsync(entity, autoSave: true);
        }
    }

    private async Task SeedTestMethodIndicatorsAsync()
    {
        // Skip if already seeded
        if (await _testMethodIndicatorRepository.GetCountAsync() > 0) return;

        // Get all indicators and test methods
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var allIndicators = indicatorQuery.ToList();

        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var allTestMethods = testMethodQuery.ToList();

        // Map method codes to their entities
        var methodByCode = allTestMethods.ToDictionary(m => m.Code, m => m);

        // Map indicator codes to their entities
        var indicatorByCode = allIndicators.ToDictionary(i => i.Code, i => i);

        // Define test method - indicator relationships with pricing
        // Format: MethodCode -> list of (IndicatorCode, UnitPrice)
        var methodIndicatorMappings = new[]
        {
            // PP241224001 - TCVN 6492:2011 - Xác định pH
            new { MethodCode = "PP241224001", Indicators = new[] {
                new { Code = "HL-001", Price = 80000m },  // pH
            }},

            // PP241224002 - TCVN 4559:1998 - Xác định EC
            new { MethodCode = "PP241224002", Indicators = new[] {
                new { Code = "HL-003", Price = 90000m },  // EC
            }},

            // PP241224003 - TCVN 6638:2000 - Xác định Nitơ tổng
            new { MethodCode = "PP241224003", Indicators = new[] {
                new { Code = "HL-007", Price = 180000m }, // Nitơ tổng
            }},

            // PP241224004 - TCVN 6187-1:2019 - Xác định Coliform
            new { MethodCode = "PP241224004", Indicators = new[] {
                new { Code = "VS-001", Price = 150000m }, // Tổng vi khuẩn hiếu khí
                new { Code = "VS-002", Price = 120000m }, // Coliform tổng số
                new { Code = "VS-003", Price = 180000m }, // E.coli
            }},

            // PP241224005 - AOAC 950.40 - Xác định Caffeine
            new { MethodCode = "PP241224005", Indicators = new[] {
                new { Code = "HL-006", Price = 250000m }, // Caffeine
            }},

            // PP241224006 - TCVN 6193:1996 - Xác định kim loại nặng
            new { MethodCode = "PP241224006", Indicators = new[] {
                new { Code = "KL-001", Price = 200000m }, // Chì (Pb)
                new { Code = "KL-002", Price = 220000m }, // Cadmium (Cd)
                new { Code = "KL-003", Price = 250000m }, // Thủy ngân (Hg)
                new { Code = "KL-004", Price = 230000m }, // Asen (As)
            }},

            // PP241224007 - TCVN 5979:2007 - Xác định pH đất
            new { MethodCode = "PP241224007", Indicators = new[] {
                new { Code = "HL-001", Price = 100000m }, // pH (giá khác cho đất)
            }},

            // PP241224008 - TCVN 6498:1999 - Xác định Nitơ tổng đất
            new { MethodCode = "PP241224008", Indicators = new[] {
                new { Code = "HL-007", Price = 200000m }, // Nitơ tổng (giá khác cho đất)
            }},

            // PP241224009 - TCVN 6928:2007 - Xác định độ ẩm cà phê
            new { MethodCode = "PP241224009", Indicators = new[] {
                new { Code = "HL-002", Price = 80000m },  // Độ ẩm
            }},

            // PP241224010 - TCVN 4325:2007 - Xác định chất béo
            new { MethodCode = "PP241224010", Indicators = new[] {
                new { Code = "HL-005", Price = 180000m }, // Chất béo
            }},

            // PP241224011 - TCVN 5501:2010 - Xác định protein
            new { MethodCode = "PP241224011", Indicators = new[] {
                new { Code = "HL-004", Price = 170000m }, // Protein
            }},

            // PP241224012 - ISO 17025:2017 - Phương pháp chuẩn PTN (nhiều chỉ tiêu)
            new { MethodCode = "PP241224012", Indicators = new[] {
                new { Code = "HL-001", Price = 85000m },
                new { Code = "HL-002", Price = 85000m },
                new { Code = "HL-003", Price = 95000m },
                new { Code = "VS-002", Price = 130000m },
            }},

            // PP241224013 - TCVN 7538-1:2006 - Xác định độ ẩm đất
            new { MethodCode = "PP241224013", Indicators = new[] {
                new { Code = "HL-002", Price = 100000m }, // Độ ẩm (giá khác cho đất)
            }},

            // PP241224014 - SMEWW 4500-NO3 - Xác định Nitrat
            new { MethodCode = "PP241224014", Indicators = new[] {
                new { Code = "HL-007", Price = 150000m }, // Nitơ tổng
            }},

            // PP241224015 - SMEWW 5210B - Xác định BOD5
            new { MethodCode = "PP241224015", Indicators = new[] {
                new { Code = "HL-001", Price = 180000m }, // pH (liên quan BOD)
                new { Code = "HL-003", Price = 120000m }, // EC
            }},

            // PP241224016 - SMEWW 5220C - Xác định COD
            new { MethodCode = "PP241224016", Indicators = new[] {
                new { Code = "HL-001", Price = 200000m }, // pH
            }},
        };

        // Seed test method indicators
        foreach (var mapping in methodIndicatorMappings)
        {
            if (!methodByCode.ContainsKey(mapping.MethodCode)) continue;
            var testMethod = methodByCode[mapping.MethodCode];

            var sortOrder = 0;
            foreach (var ind in mapping.Indicators)
            {
                if (!indicatorByCode.ContainsKey(ind.Code)) continue;
                var indicator = indicatorByCode[ind.Code];

                sortOrder++;
                var testMethodIndicator = new TestMethodIndicator(
                    _guidGenerator.Create(),
                    testMethod.Id,
                    indicator.Id,
                    ind.Price
                );
                testMethodIndicator.SortOrder = sortOrder;
                await _testMethodIndicatorRepository.InsertAsync(testMethodIndicator);
            }
        }
    }

    private async Task SeedStandardsAsync()
    {
        // Skip if already seeded
        if (await _standardRepository.GetCountAsync() > 0) return;

        var standards = new[]
        {
            // QCVN - Quy chuẩn Việt Nam
            new { Code = "TC251225001", Name = "QCVN 01-1:2018/BYT - Nước ăn uống", LimitDescription = "Quy chuẩn kỹ thuật quốc gia về chất lượng nước ăn uống", SortOrder = 1 },
            new { Code = "TC251225002", Name = "QCVN 02:2009/BYT - Nước sinh hoạt", LimitDescription = "Quy chuẩn kỹ thuật quốc gia về chất lượng nước sinh hoạt", SortOrder = 2 },
            new { Code = "TC251225003", Name = "QCVN 08-MT:2015/BTNMT - Nước mặt", LimitDescription = "Quy chuẩn kỹ thuật quốc gia về chất lượng nước mặt", SortOrder = 3 },
            new { Code = "TC251225004", Name = "QCVN 09-MT:2015/BTNMT - Nước ngầm", LimitDescription = "Quy chuẩn kỹ thuật quốc gia về chất lượng nước dưới đất", SortOrder = 4 },
            new { Code = "TC251225005", Name = "QCVN 40:2011/BTNMT - Nước thải công nghiệp", LimitDescription = "Quy chuẩn kỹ thuật quốc gia về nước thải công nghiệp", SortOrder = 5 },
            new { Code = "TC251225006", Name = "QCVN 14:2008/BTNMT - Nước thải sinh hoạt", LimitDescription = "Quy chuẩn kỹ thuật quốc gia về nước thải sinh hoạt", SortOrder = 6 },
            new { Code = "TC251225007", Name = "QCVN 03-MT:2015/BTNMT - Đất", LimitDescription = "Quy chuẩn kỹ thuật quốc gia về giới hạn cho phép của kim loại nặng trong đất", SortOrder = 7 },

            // TCVN - Tiêu chuẩn Việt Nam cho thực phẩm
            new { Code = "TC251225008", Name = "TCVN 4193:2014 - Cà phê nhân", LimitDescription = "Tiêu chuẩn về cà phê nhân - yêu cầu kỹ thuật", SortOrder = 8 },
            new { Code = "TC251225009", Name = "TCVN 5253:1990 - Cà phê bột", LimitDescription = "Tiêu chuẩn về cà phê bột - yêu cầu kỹ thuật", SortOrder = 9 },
            new { Code = "TC251225010", Name = "QCVN 8-2:2011/BYT - Vi sinh thực phẩm", LimitDescription = "Quy chuẩn về giới hạn ô nhiễm vi sinh vật trong thực phẩm", SortOrder = 10 },
            new { Code = "TC251225011", Name = "QCVN 8-1:2011/BYT - Kim loại nặng thực phẩm", LimitDescription = "Quy chuẩn về giới hạn ô nhiễm kim loại nặng trong thực phẩm", SortOrder = 11 },

            // Tiêu chuẩn phân bón
            new { Code = "TC251225012", Name = "QCVN 01-189:2019/BNNPTNT - Phân bón", LimitDescription = "Quy chuẩn về chất lượng phân bón", SortOrder = 12 },

            // Tiêu chuẩn rau củ quả
            new { Code = "TC251225013", Name = "QCVN 8-3:2012/BYT - Dư lượng BVTV", LimitDescription = "Quy chuẩn về giới hạn tối đa dư lượng thuốc BVTV trong thực phẩm", SortOrder = 13 },
        };

        foreach (var std in standards)
        {
            var entity = new Standard(_guidGenerator.Create(), std.Code, std.Name);
            entity.LimitDescription = std.LimitDescription;
            entity.SortOrder = std.SortOrder;
            entity.IsActive = true;
            await _standardRepository.InsertAsync(entity, autoSave: true);
        }
    }

    private async Task SeedStandardIndicatorsAsync()
    {
        // Skip if already seeded
        if (await _standardIndicatorRepository.GetCountAsync() > 0) return;

        // Get all standards, indicators, and test methods
        var standardQuery = await _standardRepository.GetQueryableAsync();
        var allStandards = standardQuery.ToList();

        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var allIndicators = indicatorQuery.ToList();

        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var allTestMethods = testMethodQuery.ToList();

        // Map by code
        var standardByName = allStandards.ToDictionary(s => s.Name, s => s);
        var indicatorByCode = allIndicators.ToDictionary(i => i.Code, i => i);
        var methodByCode = allTestMethods.ToDictionary(m => m.Code, m => m);

        // Define standard-indicator relationships with limits
        // Format: StandardName -> list of (IndicatorCode, TestMethodCode, Unit, ComparisonOperator, AllowedLimit)
        // Note: Giới hạn dạng khoảng (vd: 6.5 - 8.5) được gộp thành 1 record với Operator = "range"
        var standardIndicatorMappings = new[]
        {
            // QCVN 01-1:2018/BYT - Nước ăn uống
            new { StandardName = "QCVN 01-1:2018/BYT - Nước ăn uống", Indicators = new[] {
                new { IndicatorCode = "HL-001", MethodCode = "PP241224001", Unit = "", Operator = "range", Limit = "6.5 - 8.5", SortOrder = 1 },
                new { IndicatorCode = "VS-002", MethodCode = "PP241224004", Unit = "CFU/100mL", Operator = "<=", Limit = "0", SortOrder = 2 },
                new { IndicatorCode = "VS-003", MethodCode = "PP241224004", Unit = "CFU/100mL", Operator = "<=", Limit = "0", SortOrder = 3 },
                new { IndicatorCode = "KL-001", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.01", SortOrder = 4 },
                new { IndicatorCode = "KL-002", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.003", SortOrder = 5 },
                new { IndicatorCode = "KL-004", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.01", SortOrder = 6 },
            }},

            // QCVN 08-MT:2015/BTNMT - Nước mặt (Cột A2 - dùng cho mục đích cấp nước sinh hoạt)
            new { StandardName = "QCVN 08-MT:2015/BTNMT - Nước mặt", Indicators = new[] {
                new { IndicatorCode = "HL-001", MethodCode = "PP241224001", Unit = "", Operator = "range", Limit = "6 - 8.5", SortOrder = 1 },
                new { IndicatorCode = "VS-002", MethodCode = "PP241224004", Unit = "CFU/100mL", Operator = "<=", Limit = "5000", SortOrder = 2 },
                new { IndicatorCode = "KL-001", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.02", SortOrder = 3 },
                new { IndicatorCode = "KL-002", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.005", SortOrder = 4 },
                new { IndicatorCode = "KL-004", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.02", SortOrder = 5 },
                new { IndicatorCode = "HL-007", MethodCode = "PP241224003", Unit = "mg/L", Operator = "<=", Limit = "5", SortOrder = 6 },
            }},

            // QCVN 09-MT:2015/BTNMT - Nước ngầm
            new { StandardName = "QCVN 09-MT:2015/BTNMT - Nước ngầm", Indicators = new[] {
                new { IndicatorCode = "HL-001", MethodCode = "PP241224001", Unit = "", Operator = "range", Limit = "5.5 - 8.5", SortOrder = 1 },
                new { IndicatorCode = "VS-002", MethodCode = "PP241224004", Unit = "CFU/100mL", Operator = "<=", Limit = "3", SortOrder = 2 },
                new { IndicatorCode = "KL-001", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.01", SortOrder = 3 },
                new { IndicatorCode = "KL-002", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.005", SortOrder = 4 },
                new { IndicatorCode = "KL-004", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.05", SortOrder = 5 },
                new { IndicatorCode = "KL-003", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.001", SortOrder = 6 },
            }},

            // QCVN 40:2011/BTNMT - Nước thải công nghiệp (Cột B)
            new { StandardName = "QCVN 40:2011/BTNMT - Nước thải công nghiệp", Indicators = new[] {
                new { IndicatorCode = "HL-001", MethodCode = "PP241224001", Unit = "", Operator = "range", Limit = "5.5 - 9", SortOrder = 1 },
                new { IndicatorCode = "VS-002", MethodCode = "PP241224004", Unit = "CFU/100mL", Operator = "<=", Limit = "5000", SortOrder = 2 },
                new { IndicatorCode = "KL-001", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.5", SortOrder = 3 },
                new { IndicatorCode = "KL-002", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.1", SortOrder = 4 },
                new { IndicatorCode = "KL-004", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.1", SortOrder = 5 },
                new { IndicatorCode = "KL-003", MethodCode = "PP241224006", Unit = "mg/L", Operator = "<=", Limit = "0.01", SortOrder = 6 },
                new { IndicatorCode = "HL-007", MethodCode = "PP241224003", Unit = "mg/L", Operator = "<=", Limit = "40", SortOrder = 7 },
            }},

            // QCVN 03-MT:2015/BTNMT - Đất (đất nông nghiệp)
            new { StandardName = "QCVN 03-MT:2015/BTNMT - Đất", Indicators = new[] {
                new { IndicatorCode = "HL-001", MethodCode = "PP241224007", Unit = "", Operator = "range", Limit = "5.5 - 7.5", SortOrder = 1 },
                new { IndicatorCode = "KL-001", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "70", SortOrder = 2 },
                new { IndicatorCode = "KL-002", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "1.5", SortOrder = 3 },
                new { IndicatorCode = "KL-004", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "15", SortOrder = 4 },
                new { IndicatorCode = "KL-003", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "0.5", SortOrder = 5 },
            }},

            // TCVN 4193:2014 - Cà phê nhân
            new { StandardName = "TCVN 4193:2014 - Cà phê nhân", Indicators = new[] {
                new { IndicatorCode = "HL-002", MethodCode = "PP241224009", Unit = "%", Operator = "<=", Limit = "12.5", SortOrder = 1 },
                new { IndicatorCode = "HL-006", MethodCode = "PP241224005", Unit = "%", Operator = "range", Limit = "0.8 - 2.5", SortOrder = 2 },
            }},

            // TCVN 5253:1990 - Cà phê bột
            new { StandardName = "TCVN 5253:1990 - Cà phê bột", Indicators = new[] {
                new { IndicatorCode = "HL-002", MethodCode = "PP241224009", Unit = "%", Operator = "<=", Limit = "5.0", SortOrder = 1 },
                new { IndicatorCode = "HL-006", MethodCode = "PP241224005", Unit = "%", Operator = ">=", Limit = "1.0", SortOrder = 2 },
                new { IndicatorCode = "HL-003", MethodCode = "PP241224012", Unit = "%", Operator = "<=", Limit = "6.0", SortOrder = 3 },
            }},

            // QCVN 8-2:2011/BYT - Vi sinh thực phẩm
            new { StandardName = "QCVN 8-2:2011/BYT - Vi sinh thực phẩm", Indicators = new[] {
                new { IndicatorCode = "VS-001", MethodCode = "PP241224004", Unit = "CFU/g", Operator = "<=", Limit = "10000", SortOrder = 1 },
                new { IndicatorCode = "VS-002", MethodCode = "PP241224004", Unit = "CFU/g", Operator = "<=", Limit = "10", SortOrder = 2 },
                new { IndicatorCode = "VS-003", MethodCode = "PP241224004", Unit = "CFU/g", Operator = "<=", Limit = "0", SortOrder = 3 },
                new { IndicatorCode = "VS-004", MethodCode = "PP241224004", Unit = "/25g", Operator = "=", Limit = "Không phát hiện", SortOrder = 4 },
                new { IndicatorCode = "VS-005", MethodCode = "PP241224004", Unit = "CFU/g", Operator = "<=", Limit = "100", SortOrder = 5 },
            }},

            // QCVN 8-1:2011/BYT - Kim loại nặng thực phẩm
            new { StandardName = "QCVN 8-1:2011/BYT - Kim loại nặng thực phẩm", Indicators = new[] {
                new { IndicatorCode = "KL-001", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "0.2", SortOrder = 1 },
                new { IndicatorCode = "KL-002", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "0.05", SortOrder = 2 },
                new { IndicatorCode = "KL-004", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "1.0", SortOrder = 3 },
                new { IndicatorCode = "KL-003", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "0.05", SortOrder = 4 },
            }},

            // QCVN 01-189:2019/BNNPTNT - Phân bón
            new { StandardName = "QCVN 01-189:2019/BNNPTNT - Phân bón", Indicators = new[] {
                new { IndicatorCode = "HL-007", MethodCode = "PP241224008", Unit = "%", Operator = ">=", Limit = "16", SortOrder = 1 },
                new { IndicatorCode = "HL-002", MethodCode = "PP241224013", Unit = "%", Operator = "<=", Limit = "5", SortOrder = 2 },
                new { IndicatorCode = "HL-001", MethodCode = "PP241224007", Unit = "", Operator = "range", Limit = "4 - 8", SortOrder = 3 },
                new { IndicatorCode = "KL-001", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "200", SortOrder = 4 },
                new { IndicatorCode = "KL-002", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "5", SortOrder = 5 },
                new { IndicatorCode = "KL-004", MethodCode = "PP241224006", Unit = "mg/kg", Operator = "<=", Limit = "40", SortOrder = 6 },
            }},

            // QCVN 8-3:2012/BYT - Dư lượng BVTV
            new { StandardName = "QCVN 8-3:2012/BYT - Dư lượng BVTV", Indicators = new[] {
                new { IndicatorCode = "BVTV-001", MethodCode = "PP241224012", Unit = "mg/kg", Operator = "<=", Limit = "0.01", SortOrder = 1 },
                new { IndicatorCode = "BVTV-002", MethodCode = "PP241224012", Unit = "mg/kg", Operator = "<=", Limit = "0.05", SortOrder = 2 },
                new { IndicatorCode = "BVTV-003", MethodCode = "PP241224012", Unit = "mg/kg", Operator = "<=", Limit = "0.1", SortOrder = 3 },
            }},
        };

        // Seed standard indicators
        foreach (var mapping in standardIndicatorMappings)
        {
            if (!standardByName.ContainsKey(mapping.StandardName)) continue;
            var standard = standardByName[mapping.StandardName];

            foreach (var ind in mapping.Indicators)
            {
                if (!indicatorByCode.ContainsKey(ind.IndicatorCode)) continue;
                if (!methodByCode.ContainsKey(ind.MethodCode)) continue;

                var indicator = indicatorByCode[ind.IndicatorCode];
                var testMethod = methodByCode[ind.MethodCode];

                var standardIndicator = new StandardIndicator(
                    _guidGenerator.Create(),
                    standard.Id,
                    indicator.Id,
                    testMethod.Id,
                    ind.Unit
                );
                standardIndicator.ComparisonOperator = ind.Operator;
                standardIndicator.AllowedLimit = ind.Limit;
                standardIndicator.SortOrder = ind.SortOrder;
                standardIndicator.IsActive = true;

                await _standardIndicatorRepository.InsertAsync(standardIndicator);
            }
        }
    }

    private async Task SeedStorageLocationsAsync()
    {
        if (await _storageLocationRepository.GetCountAsync() > 0) return;

        var locations = new[]
        {
            new { Code = "KLA1", Name = "Tủ lạnh 2-8°C - Phòng A1", Temp = "2-8°C" },
            new { Code = "KLA2", Name = "Tủ đông -20°C - Phòng A2", Temp = "-20°C" },
            new { Code = "KLB1", Name = "Tủ đông -80°C - Lưu trữ dài hạn", Temp = "-80°C" },
            new { Code = "PMT1", Name = "Phòng mẫu nhiệt độ thường", Temp = "25°C" },
            new { Code = "KHC1", Name = "Kho hóa chất", Temp = "20-25°C" },
        };

        foreach (var loc in locations)
        {
            var entity = new StorageLocation(_guidGenerator.Create(), loc.Code, loc.Name);
            entity.Temperature = loc.Temp;
            await _storageLocationRepository.InsertAsync(entity);
        }
    }

    private async Task SeedCustomersAsync()
    {
        if (await _customerRepository.GetCountAsync() > 0) return;

        var customers = new[]
        {
            new { Code = "CUS-001", Name = "Công ty TNHH Cà phê Tây Nguyên", Email = "info@capheTN.com.vn", Phone = "0262 3852 115", Address = "123 Lê Duẩn, TP. Buôn Ma Thuột, Đắk Lắk" },
            new { Code = "CUS-002", Name = "HTX Nông nghiệp Ea H'leo", Email = "htxeahleo@gmail.com", Phone = "0262 3777 222", Address = "Xã Ea Hiao, Huyện Ea H'leo, Đắk Lắk" },
            new { Code = "CUS-003", Name = "Viện Nghiên cứu Khoa học Tây Nguyên", Email = "viennc@taynguyen.gov.vn", Phone = "0262 3856 789", Address = "45 Nguyễn Tất Thành, TP. Buôn Ma Thuột, Đắk Lắk" },
            new { Code = "CUS-004", Name = "Công ty CP Phân bón Đắk Lắk", Email = "phanbondaklak@gmail.com", Phone = "0262 3859 999", Address = "KCN Hòa Phú, TP. Buôn Ma Thuột, Đắk Lắk" },
            new { Code = "CUS-005", Name = "Sở Tài nguyên và Môi trường Đắk Lắk", Email = "stnmt@daklak.gov.vn", Phone = "0262 3851 234", Address = "18 Lê Duẩn, TP. Buôn Ma Thuột, Đắk Lắk" },
        };

        foreach (var cust in customers)
        {
            var entity = new Customer(_guidGenerator.Create(), cust.Code, cust.Name);
            entity.Email = cust.Email;
            entity.Phone = cust.Phone;
            entity.Address = cust.Address;
            await _customerRepository.InsertAsync(entity);
        }
    }

    private async Task SeedLaboratoriesAsync()
    {
        if (await _laboratoryRepository.GetCountAsync() > 0) return;

        // Đảm bảo users đã được seed trước khi seed Laboratory
        // (vì thứ tự seed của ABP không đảm bảo)
        await _labUsersDataSeedContributor.SeedAsync(new DataSeedContext());

        // Lấy danh sách users để làm TechnicalManager
        var labVisinh = await _userRepository.FindByNormalizedUserNameAsync("LABVISINH");
        var labHoa = await _userRepository.FindByNormalizedUserNameAsync("LABHOA");
        var quanlychatluong = await _userRepository.FindByNormalizedUserNameAsync("QUANLYCHATLUONG");

        // Gán TechnicalManager cho từng phòng lab
        // labVisinh = Lab Vi Sinh - QLKT
        // labHoa = Lab Hóa - QLKT
        // quanlychatluong = Quản lý chất lượng
        var laboratories = new[]
        {
            new { Code = "TTKN", Name = "Trung tâm kiểm nghiệm", SortOrder = 1, TechnicalManagerId = quanlychatluong?.Id },
            new { Code = "HLSK", Name = "Lab Hóa Lý sắc ký", SortOrder = 2, TechnicalManagerId = labHoa?.Id },
            new { Code = "LVS", Name = "Lab vi sinh", SortOrder = 3, TechnicalManagerId = labVisinh?.Id },
            new { Code = "PNM", Name = "Phòng Nhận mẫu", SortOrder = 4, TechnicalManagerId = quanlychatluong?.Id },
            new { Code = "KVK", Name = "Khoa vi khuẩn", SortOrder = 5, TechnicalManagerId = labVisinh?.Id },
            new { Code = "HDP", Name = "Hợp đồng phụ", SortOrder = 6, TechnicalManagerId = quanlychatluong?.Id },
            new { Code = "KKT", Name = "Khoa Kế Toán", SortOrder = 7, TechnicalManagerId = quanlychatluong?.Id },
            new { Code = "QTN-KLN", Name = "Quầy thu ngân - Kim loại nặng", SortOrder = 8, TechnicalManagerId = labHoa?.Id },
            new { Code = "LH", Name = "Lab Hóa", SortOrder = 9, TechnicalManagerId = labHoa?.Id },
        };

        foreach (var lab in laboratories)
        {
            var entity = new Laboratory(_guidGenerator.Create(), lab.Code, lab.Name);
            entity.SortOrder = lab.SortOrder;
            entity.IsActive = true;
            entity.TechnicalManagerId = lab.TechnicalManagerId;
            await _laboratoryRepository.InsertAsync(entity);
        }
    }

    private async Task SeedOrganizationsAsync()
    {
        if (await _organizationRepository.GetCountAsync() > 0) return;

        // ProvinceCode: 66 = Đắk Lắk
        // WardCodes: 24124 = Phường Tân Lợi, 24127 = Phường Thắng Lợi, 24130 = Phường Tân An, etc.
        var organizations = new[]
        {
            new { Name = "Công ty TNHH Cà phê Tây Nguyên", ProvinceCode = "66", WardCode = "24124", Address = "123 Lê Duẩn, TP. Buôn Ma Thuột", ContactPerson = "Nguyễn Văn A", Phone = "0262385211", TaxCode = "6001234567", SortOrder = 1 },
            new { Name = "HTX Nông nghiệp Ea H'leo", ProvinceCode = "66", WardCode = "24364", Address = "Xã Ea Hiao, Huyện Ea H'leo", ContactPerson = "Trần Thị B", Phone = "0262377722", TaxCode = "6001234568", SortOrder = 2 },
            new { Name = "Viện Nghiên cứu Khoa học Tây Nguyên", ProvinceCode = "66", WardCode = "24127", Address = "45 Nguyễn Tất Thành, TP. Buôn Ma Thuột", ContactPerson = "Lê Văn C", Phone = "0262385678", TaxCode = "6001234569", SortOrder = 3 },
            new { Name = "Công ty CP Phân bón Đắk Lắk", ProvinceCode = "66", WardCode = "24130", Address = "KCN Hòa Phú, TP. Buôn Ma Thuột", ContactPerson = "Phạm Văn D", Phone = "0262385999", TaxCode = "6001234570", SortOrder = 4 },
            new { Name = "Sở Tài nguyên và Môi trường Đắk Lắk", ProvinceCode = "66", WardCode = "24124", Address = "18 Lê Duẩn, TP. Buôn Ma Thuột", ContactPerson = "Hoàng Thị E", Phone = "0262385123", TaxCode = (string?)null, SortOrder = 5 },
            new { Name = "Công ty TNHH MTV Cao su Đắk Lắk", ProvinceCode = "66", WardCode = "24133", Address = "Số 5 Phan Chu Trinh, TP. Buôn Ma Thuột", ContactPerson = "Võ Văn F", Phone = "0262385456", TaxCode = "6001234571", SortOrder = 6 },
            new { Name = "HTX Nông nghiệp xanh Krông Năng", ProvinceCode = "66", WardCode = "24454", Address = "Thị trấn Krông Năng", ContactPerson = "Đặng Thị G", Phone = "0262378901", TaxCode = (string?)null, SortOrder = 7 },
            new { Name = "Chi cục Bảo vệ môi trường Đắk Lắk", ProvinceCode = "66", WardCode = "24127", Address = "20 Nguyễn Tất Thành, TP. Buôn Ma Thuột", ContactPerson = "Bùi Văn H", Phone = "0262385234", TaxCode = (string?)null, SortOrder = 8 },
            new { Name = "Trung tâm Khuyến nông Đắk Lắk", ProvinceCode = "66", WardCode = "24130", Address = "56 Lê Duẩn, TP. Buôn Ma Thuột", ContactPerson = "Ngô Thị I", Phone = "0262385345", TaxCode = (string?)null, SortOrder = 9 },
            new { Name = "Công ty CP Chế biến Nông sản Đắk Lắk", ProvinceCode = "66", WardCode = "24136", Address = "KCN Tân An, TP. Buôn Ma Thuột", ContactPerson = "Trương Văn K", Phone = "0262385567", TaxCode = "6001234572", SortOrder = 10 },
            new { Name = "HTX Dịch vụ Nông nghiệp Buôn Ma Thuột", ProvinceCode = "66", WardCode = "24139", Address = "Xã Hòa Thuận, TP. Buôn Ma Thuột", ContactPerson = "Lý Thị L", Phone = "0262385678", TaxCode = (string?)null, SortOrder = 11 },
            new { Name = "Công ty TNHH XNK Cà phê Tây Nguyên", ProvinceCode = "66", WardCode = "24124", Address = "89 Lê Duẩn, TP. Buôn Ma Thuột", ContactPerson = "Mai Văn M", Phone = "0262385789", TaxCode = "6001234573", SortOrder = 12 },
            new { Name = "Trung tâm Quan trắc Môi trường Đắk Lắk", ProvinceCode = "66", WardCode = "24127", Address = "30 Nguyễn Tất Thành, TP. Buôn Ma Thuột", ContactPerson = "Đinh Thị N", Phone = "0262385890", TaxCode = (string?)null, SortOrder = 13 },
            new { Name = "Viện Khoa học Kỹ thuật Nông lâm nghiệp Tây Nguyên", ProvinceCode = "66", WardCode = "24142", Address = "Xã Ea Tu, TP. Buôn Ma Thuột", ContactPerson = "Phan Văn O", Phone = "0262385901", TaxCode = "6001234574", SortOrder = 14 },
            new { Name = "Công ty TNHH Thực phẩm sạch Đắk Lắk", ProvinceCode = "66", WardCode = "24133", Address = "15 Phan Chu Trinh, TP. Buôn Ma Thuột", ContactPerson = "Hồ Thị P", Phone = "0262386012", TaxCode = "6001234575", SortOrder = 15 },
        };

        foreach (var org in organizations)
        {
            var entity = new Organization(_guidGenerator.Create(), org.Name);
            entity.ProvinceCode = org.ProvinceCode;
            entity.WardCode = org.WardCode;
            entity.Address = org.Address;
            entity.ContactPerson = org.ContactPerson;
            entity.Phone = org.Phone;
            entity.TaxCode = org.TaxCode;
            entity.SortOrder = org.SortOrder;
            entity.IsActive = true;
            await _organizationRepository.InsertAsync(entity);
        }
    }

    private async Task SeedTestRequestsAndRelatedDataAsync()
    {
        if (await _testRequestRepository.GetCountAsync() > 0) return;

        var now = DateTime.Now;
        var userId = Guid.NewGuid(); // Placeholder for user reference

        // Get indicators for use in samples
        var allIndicators = await _indicatorRepository.GetListAsync();

        // Test Request 1 - Completed
        var req1Id = _guidGenerator.Create();
        var req1 = new TestRequest(
            req1Id,
            "REQ-20251201-0001",
            now.AddDays(-12),
            "Công ty TNHH Cà phê Tây Nguyên"
        );
        req1.Phone = "0262 3852 115";
        req1.Address = "123 Lê Duẩn, TP. Buôn Ma Thuột, Đắk Lắk";
        req1.Province = "Đắk Lắk";
        req1.District = "TP. Buôn Ma Thuột";
        req1.Status = TestRequestStatus.Completed;
        req1.ExpectedCompletionDate = now.AddDays(-5);
        req1.Notes = "Khách hàng yêu cầu kết quả gấp";
        req1.TotalSampleCount = 2;
        req1.TotalCost = 830000;
        await _testRequestRepository.InsertAsync(req1);

        // Sample 1 for Request 1
        var sample1Id = _guidGenerator.Create();
        var sample1 = new TestRequestSample(sample1Id, req1Id, "M001", "Mẫu cà phê nhân - Lô A001");
        sample1.SortOrder = 1;
        sample1.SamplingAddress = "Nông trại Tây Nguyên, Xã Ea Tu, TP. Buôn Ma Thuột";
        sample1.SampleQuantity = "2 kg";
        sample1.Status = SampleStatus.Completed;
        sample1.ProcessingStatus = "Hoàn thành";
        sample1.StorageLocation = "PMT1";
        sample1.Notes = "Mẫu cà phê nhân sống, lô thu hoạch tháng 11/2025";
        sample1.TotalIndicatorCost = 430000;
        await _testRequestSampleRepository.InsertAsync(sample1);

        // Add indicators to Sample 1
        var indDoAm = allIndicators.FirstOrDefault(i => i.Code == "HL-002");
        var indCaffeine = allIndicators.FirstOrDefault(i => i.Code == "HL-006");
        var indPH = allIndicators.FirstOrDefault(i => i.Code == "HL-001");

        if (indDoAm != null)
        {
            var si1 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample1Id, indDoAm.Id);
            si1.TransferDate = now.AddDays(-11);
            si1.RequestDate = now.AddDays(-12);
            si1.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si1);
        }
        if (indCaffeine != null)
        {
            var si2 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample1Id, indCaffeine.Id);
            si2.TransferDate = now.AddDays(-11);
            si2.RequestDate = now.AddDays(-12);
            si2.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si2);
        }
        if (indPH != null)
        {
            var si3 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample1Id, indPH.Id);
            si3.TransferDate = now.AddDays(-11);
            si3.RequestDate = now.AddDays(-12);
            si3.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si3);
        }

        // Sample 2 for Request 1
        var sample2Id = _guidGenerator.Create();
        var sample2 = new TestRequestSample(sample2Id, req1Id, "M002", "Mẫu cà phê bột - Lô A001");
        sample2.SortOrder = 2;
        sample2.SamplingAddress = "Xưởng chế biến Tây Nguyên";
        sample2.SampleQuantity = "500 g";
        sample2.Status = SampleStatus.Completed;
        sample2.ProcessingStatus = "Hoàn thành";
        sample2.StorageLocation = "PMT1";
        sample2.Notes = "Mẫu cà phê bột rang xay";
        sample2.TotalIndicatorCost = 400000;
        await _testRequestSampleRepository.InsertAsync(sample2);

        // Add indicators to Sample 2
        if (indCaffeine != null)
        {
            var si4 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample2Id, indCaffeine.Id);
            si4.TransferDate = now.AddDays(-11);
            si4.RequestDate = now.AddDays(-12);
            si4.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si4);
        }
        if (indDoAm != null)
        {
            var si5 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample2Id, indDoAm.Id);
            si5.TransferDate = now.AddDays(-11);
            si5.RequestDate = now.AddDays(-12);
            si5.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si5);
        }

        // Specimen 1 for Request 1
        var spe1Id = _guidGenerator.Create();
        var spe1 = new Specimen(spe1Id, req1Id, "SPE-20251201-0001", "Mẫu cà phê nhân - Lô A001", "Cà phê", now.AddDays(-12));
        spe1.Quantity = 2;
        spe1.Unit = "kg";
        spe1.Status = SpecimenStatus.Storage;
        spe1.StorageLocation = "Phòng mẫu nhiệt độ thường";
        spe1.Notes = "Mẫu cà phê nhân sống, lô thu hoạch tháng 11/2025";
        await _specimenRepository.InsertAsync(spe1);

        // Specimen 2 for Request 1
        var spe2Id = _guidGenerator.Create();
        var spe2 = new Specimen(spe2Id, req1Id, "SPE-20251201-0002", "Mẫu cà phê bột - Lô A001", "Cà phê", now.AddDays(-12));
        spe2.Quantity = 500;
        spe2.Unit = "g";
        spe2.Status = SpecimenStatus.Storage;
        spe2.StorageLocation = "Phòng mẫu nhiệt độ thường";
        spe2.Notes = "Mẫu cà phê bột rang xay";
        await _specimenRepository.InsertAsync(spe2);

        // Test 1 for Specimen 1
        var test1Id = _guidGenerator.Create();
        var test1 = new Test(test1Id, spe1Id, "TEST-20251201-0001", "Xác định độ ẩm cà phê nhân", "TCVN 6928:2007", "TCVN 4193:2014");
        test1.Status = TestStatus.Approved;
        test1.AssigneeId = userId;
        test1.StartDate = now.AddDays(-11);
        test1.CompletionDate = now.AddDays(-10);
        await _testRepository.InsertAsync(test1);

        // Test Result 1
        var result1 = new TestResult(_guidGenerator.Create(), test1Id, "Độ ẩm", "11.5", "%");
        result1.LowerLimit = 0;
        result1.UpperLimit = 12.5m;
        result1.IsPass = true;
        result1.ApprovalStatus = ResultApprovalStatus.Step2Approved;
        result1.EnteredById = userId;
        result1.EntryDate = now.AddDays(-10);
        result1.ReviewerId = userId;
        result1.ReviewDate = now.AddDays(-9);
        result1.ApproverId = userId;
        result1.ApprovalDate = now.AddDays(-8);
        await _testResultRepository.InsertAsync(result1);

        // Test 2 for Specimen 2
        var test2Id = _guidGenerator.Create();
        var test2 = new Test(test2Id, spe2Id, "TEST-20251201-0002", "Xác định hàm lượng Caffeine", "AOAC 950.40", "TCVN 5253:1990");
        test2.Status = TestStatus.Approved;
        test2.AssigneeId = userId;
        test2.StartDate = now.AddDays(-11);
        test2.CompletionDate = now.AddDays(-9);
        await _testRepository.InsertAsync(test2);

        // Test Result 2
        var result2 = new TestResult(_guidGenerator.Create(), test2Id, "Caffeine", "1.35", "%");
        result2.LowerLimit = 0.8m;
        result2.UpperLimit = 2.5m;
        result2.IsPass = true;
        result2.ApprovalStatus = ResultApprovalStatus.Step2Approved;
        result2.EnteredById = userId;
        result2.EntryDate = now.AddDays(-9);
        result2.ReviewerId = userId;
        result2.ReviewDate = now.AddDays(-8);
        result2.ApproverId = userId;
        result2.ApprovalDate = now.AddDays(-7);
        await _testResultRepository.InsertAsync(result2);

        // Test Request 2 - In Progress
        var req2Id = _guidGenerator.Create();
        var req2 = new TestRequest(
            req2Id,
            "REQ-20251203-0002",
            now.AddDays(-10),
            "HTX Nông nghiệp Ea H'leo"
        );
        req2.Phone = "0262 3777 222";
        req2.Address = "Xã Ea Hiao, Huyện Ea H'leo, Đắk Lắk";
        req2.Province = "Đắk Lắk";
        req2.District = "Ea H'leo";
        req2.Status = TestRequestStatus.InProgress;
        req2.ExpectedCompletionDate = now.AddDays(-3);
        req2.TotalSampleCount = 2;
        req2.TotalCost = 1080000;
        await _testRequestRepository.InsertAsync(req2);

        // Sample 3 for Request 2
        var sample3Id = _guidGenerator.Create();
        var sample3 = new TestRequestSample(sample3Id, req2Id, "M003", "Mẫu đất nông nghiệp - Khu vực A");
        sample3.SortOrder = 1;
        sample3.SamplingAddress = "Xã Ea Hiao, Huyện Ea H'leo";
        sample3.SampleQuantity = "3 kg";
        sample3.Status = SampleStatus.Testing;
        sample3.ProcessingStatus = "Đang thử nghiệm";
        sample3.StorageLocation = "PMT1";
        sample3.Notes = "Mẫu đất trồng cà phê, độ sâu 0-30cm";
        sample3.TotalIndicatorCost = 540000;
        await _testRequestSampleRepository.InsertAsync(sample3);

        // Add indicators to Sample 3
        var indNito = allIndicators.FirstOrDefault(i => i.Code == "HL-007");
        var indChi = allIndicators.FirstOrDefault(i => i.Code == "KL-001");
        var indCadmium = allIndicators.FirstOrDefault(i => i.Code == "KL-002");

        if (indPH != null)
        {
            var si6 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample3Id, indPH.Id);
            si6.TransferDate = now.AddDays(-9);
            si6.RequestDate = now.AddDays(-10);
            si6.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si6);
        }
        if (indNito != null)
        {
            var si7 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample3Id, indNito.Id);
            si7.TransferDate = now.AddDays(-9);
            si7.RequestDate = now.AddDays(-10);
            si7.SentToDepartment = false; // Chưa gửi
            await _testRequestSampleIndicatorRepository.InsertAsync(si7);
        }
        if (indChi != null)
        {
            var si8 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample3Id, indChi.Id);
            si8.RequestDate = now.AddDays(-10);
            si8.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si8);
        }

        // Sample 4 for Request 2
        var sample4Id = _guidGenerator.Create();
        var sample4 = new TestRequestSample(sample4Id, req2Id, "M004", "Mẫu đất nông nghiệp - Khu vực B");
        sample4.SortOrder = 2;
        sample4.SamplingAddress = "Xã Ea Hiao, Huyện Ea H'leo";
        sample4.SampleQuantity = "3 kg";
        sample4.Status = SampleStatus.Testing;
        sample4.ProcessingStatus = "Đang thử nghiệm";
        sample4.StorageLocation = "PMT1";
        sample4.Notes = "Mẫu đất trồng hồ tiêu, độ sâu 0-30cm";
        sample4.TotalIndicatorCost = 540000;
        await _testRequestSampleRepository.InsertAsync(sample4);

        if (indPH != null)
        {
            var si9 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample4Id, indPH.Id);
            si9.TransferDate = now.AddDays(-8);
            si9.RequestDate = now.AddDays(-10);
            si9.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si9);
        }
        if (indCadmium != null)
        {
            var si10 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample4Id, indCadmium.Id);
            si10.RequestDate = now.AddDays(-10);
            si10.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si10);
        }

        // Specimen 3 for Request 2
        var spe3Id = _guidGenerator.Create();
        var spe3 = new Specimen(spe3Id, req2Id, "SPE-20251203-0003", "Mẫu đất nông nghiệp - Khu vực A", "Đất", now.AddDays(-10));
        spe3.Quantity = 3;
        spe3.Unit = "kg";
        spe3.Status = SpecimenStatus.Testing;
        spe3.StorageLocation = "Phòng mẫu nhiệt độ thường";
        spe3.Notes = "Mẫu đất trồng cà phê, độ sâu 0-30cm";
        await _specimenRepository.InsertAsync(spe3);

        // Specimen 4 for Request 2
        var spe4Id = _guidGenerator.Create();
        var spe4 = new Specimen(spe4Id, req2Id, "SPE-20251203-0004", "Mẫu đất nông nghiệp - Khu vực B", "Đất", now.AddDays(-10));
        spe4.Quantity = 3;
        spe4.Unit = "kg";
        spe4.Status = SpecimenStatus.Testing;
        spe4.StorageLocation = "Phòng mẫu nhiệt độ thường";
        spe4.Notes = "Mẫu đất trồng hồ tiêu, độ sâu 0-30cm";
        await _specimenRepository.InsertAsync(spe4);

        // Test 3 for Specimen 3
        var test3Id = _guidGenerator.Create();
        var test3 = new Test(test3Id, spe3Id, "TEST-20251203-0003", "Xác định pH đất", "TCVN 5979:2007", "TCVN 5979:2007");
        test3.Status = TestStatus.Completed;
        test3.AssigneeId = userId;
        test3.StartDate = now.AddDays(-9);
        test3.CompletionDate = now.AddDays(-8);
        await _testRepository.InsertAsync(test3);

        // Test Result 3
        var result3 = new TestResult(_guidGenerator.Create(), test3Id, "pH", "5.8", "");
        result3.LowerLimit = 5.5m;
        result3.UpperLimit = 7.0m;
        result3.IsPass = true;
        result3.ApprovalStatus = ResultApprovalStatus.Step1Reviewed;
        result3.EnteredById = userId;
        result3.EntryDate = now.AddDays(-8);
        result3.ReviewerId = userId;
        result3.ReviewDate = now.AddDays(-7);
        result3.Notes = "pH phù hợp cho trồng cà phê";
        await _testResultRepository.InsertAsync(result3);

        // Test 4 for Specimen 3 - In Progress
        var test4Id = _guidGenerator.Create();
        var test4 = new Test(test4Id, spe3Id, "TEST-20251203-0004", "Xác định Nitơ tổng trong đất", "TCVN 6498:1999", "TCVN 6498:1999");
        test4.Status = TestStatus.InProgress;
        test4.AssigneeId = userId;
        test4.StartDate = now.AddDays(-7);
        test4.Notes = "Đang chờ kết quả phân tích";
        await _testRepository.InsertAsync(test4);

        // Test Request 3 - In Progress (Water samples)
        var req3Id = _guidGenerator.Create();
        var req3 = new TestRequest(
            req3Id,
            "REQ-20251205-0003",
            now.AddDays(-8),
            "Viện Nghiên cứu Khoa học Tây Nguyên"
        );
        req3.Phone = "0262 3856 789";
        req3.Address = "45 Nguyễn Tất Thành, TP. Buôn Ma Thuột, Đắk Lắk";
        req3.Province = "Đắk Lắk";
        req3.District = "TP. Buôn Ma Thuột";
        req3.Status = TestRequestStatus.InProgress;
        req3.ExpectedCompletionDate = now.AddDays(-1);
        req3.Notes = "Mẫu nghiên cứu đặc biệt";
        req3.TotalSampleCount = 2;
        req3.TotalCost = 920000;
        await _testRequestRepository.InsertAsync(req3);

        // Sample 5 for Request 3
        var sample5Id = _guidGenerator.Create();
        var sample5 = new TestRequestSample(sample5Id, req3Id, "M005", "Mẫu nước ngầm - Giếng khoan GK01");
        sample5.SortOrder = 1;
        sample5.SamplingAddress = "Giếng khoan GK01, Xã Ea Tu";
        sample5.SampleQuantity = "2 L";
        sample5.Status = SampleStatus.Testing;
        sample5.ProcessingStatus = "Đang thử nghiệm";
        sample5.StorageLocation = "KLA1";
        sample5.Notes = "Mẫu nước ngầm lấy từ giếng khoan sâu 50m";
        sample5.TotalIndicatorCost = 520000;
        await _testRequestSampleRepository.InsertAsync(sample5);

        // Add indicators to Sample 5
        var indColiform = allIndicators.FirstOrDefault(i => i.Code == "VS-002");
        var indEcoli = allIndicators.FirstOrDefault(i => i.Code == "VS-003");
        var indAsen = allIndicators.FirstOrDefault(i => i.Code == "KL-004");

        if (indPH != null)
        {
            var si11 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample5Id, indPH.Id);
            si11.TransferDate = now.AddDays(-7);
            si11.RequestDate = now.AddDays(-8);
            si11.SentToDepartment = true;
            await _testRequestSampleIndicatorRepository.InsertAsync(si11);
        }
        if (indColiform != null)
        {
            var si12 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample5Id, indColiform.Id);
            si12.RequestDate = now.AddDays(-8);
            si12.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si12);
        }
        if (indAsen != null)
        {
            var si13 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample5Id, indAsen.Id);
            si13.RequestDate = now.AddDays(-8);
            si13.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si13);
        }

        // Sample 6 for Request 3
        var sample6Id = _guidGenerator.Create();
        var sample6 = new TestRequestSample(sample6Id, req3Id, "M006", "Mẫu nước mặt - Suối Ea Kao");
        sample6.SortOrder = 2;
        sample6.SamplingAddress = "Suối Ea Kao, Điểm lấy mẫu QT-01";
        sample6.SampleQuantity = "2 L";
        sample6.Status = SampleStatus.Normal;
        sample6.ProcessingStatus = "Đã nhận";
        sample6.StorageLocation = "KLA1";
        sample6.Notes = "Mẫu nước suối Ea Kao, điểm lấy mẫu QT-01";
        sample6.TotalIndicatorCost = 400000;
        await _testRequestSampleRepository.InsertAsync(sample6);

        if (indPH != null)
        {
            var si14 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample6Id, indPH.Id);
            si14.RequestDate = now.AddDays(-8);
            si14.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si14);
        }
        if (indChi != null)
        {
            var si15 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample6Id, indChi.Id);
            si15.RequestDate = now.AddDays(-8);
            si15.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si15);
        }

        // Specimen 5 - Groundwater
        var spe5Id = _guidGenerator.Create();
        var spe5 = new Specimen(spe5Id, req3Id, "SPE-20251205-0005", "Mẫu nước ngầm - Giếng khoan GK01", "Nước ngầm", now.AddDays(-8));
        spe5.Quantity = 2;
        spe5.Unit = "L";
        spe5.Status = SpecimenStatus.Testing;
        spe5.StorageLocation = "Tủ lạnh 2-8°C - Phòng A1";
        spe5.Notes = "Mẫu nước ngầm lấy từ giếng khoan sâu 50m";
        await _specimenRepository.InsertAsync(spe5);

        // Specimen 6 - Surface water
        var spe6Id = _guidGenerator.Create();
        var spe6 = new Specimen(spe6Id, req3Id, "SPE-20251205-0006", "Mẫu nước mặt - Suối Ea Kao", "Nước mặt", now.AddDays(-8));
        spe6.Quantity = 2;
        spe6.Unit = "L";
        spe6.Status = SpecimenStatus.Testing;
        spe6.StorageLocation = "Tủ lạnh 2-8°C - Phòng A1";
        spe6.Notes = "Mẫu nước suối Ea Kao, điểm lấy mẫu QT-01";
        await _specimenRepository.InsertAsync(spe6);

        // Test 5 - pH for groundwater
        var test5Id = _guidGenerator.Create();
        var test5 = new Test(test5Id, spe5Id, "TEST-20251205-0005", "Xác định pH nước ngầm", "TCVN 6492:2011", "QCVN 09-MT:2015/BTNMT");
        test5.Status = TestStatus.InProgress;
        test5.AssigneeId = userId;
        test5.StartDate = now.AddDays(-7);
        await _testRepository.InsertAsync(test5);

        // Test Result 4 - Draft
        var result4 = new TestResult(_guidGenerator.Create(), test5Id, "pH", "6.8", "");
        result4.LowerLimit = 5.5m;
        result4.UpperLimit = 8.5m;
        result4.IsPass = true;
        result4.ApprovalStatus = ResultApprovalStatus.Draft;
        result4.EnteredById = userId;
        result4.EntryDate = now.AddDays(-3);
        result4.Notes = "Kết quả sơ bộ, cần kiểm tra lại";
        await _testResultRepository.InsertAsync(result4);

        // Test 6 - Coliform for groundwater
        var test6Id = _guidGenerator.Create();
        var test6 = new Test(test6Id, spe5Id, "TEST-20251205-0006", "Xác định Coliform nước ngầm", "TCVN 6187-1:2019", "QCVN 09-MT:2015/BTNMT");
        test6.Status = TestStatus.Assigned;
        test6.AssigneeId = userId;
        await _testRepository.InsertAsync(test6);

        // Test 7 - pH for surface water - Pending
        var test7 = new Test(_guidGenerator.Create(), spe6Id, "TEST-20251205-0007", "Xác định pH nước mặt", "TCVN 6492:2011", "QCVN 08-MT:2015/BTNMT");
        test7.Status = TestStatus.Pending;
        await _testRepository.InsertAsync(test7);

        // Test 8 - Heavy metals - Pending
        var test8 = new Test(_guidGenerator.Create(), spe6Id, "TEST-20251205-0008", "Xác định kim loại nặng nước mặt", "TCVN 6193:1996", "QCVN 08-MT:2015/BTNMT");
        test8.Status = TestStatus.Pending;
        await _testRepository.InsertAsync(test8);

        // Test Request 4 - Pending (Fertilizer)
        var req4Id = _guidGenerator.Create();
        var req4 = new TestRequest(
            req4Id,
            "REQ-20251208-0004",
            now.AddDays(-5),
            "Công ty CP Phân bón Đắk Lắk"
        );
        req4.Phone = "0262 3859 999";
        req4.Address = "KCN Hòa Phú, TP. Buôn Ma Thuột, Đắk Lắk";
        req4.Province = "Đắk Lắk";
        req4.District = "TP. Buôn Ma Thuột";
        req4.Status = TestRequestStatus.Pending;
        req4.ExpectedCompletionDate = now.AddDays(2);
        req4.TotalSampleCount = 1;
        req4.TotalCost = 380000;
        await _testRequestRepository.InsertAsync(req4);

        // Sample 7 for Request 4
        var sample7Id = _guidGenerator.Create();
        var sample7 = new TestRequestSample(sample7Id, req4Id, "M007", "Mẫu phân bón NPK 16-16-8");
        sample7.SortOrder = 1;
        sample7.SamplingAddress = "Nhà máy Phân bón Đắk Lắk";
        sample7.SampleQuantity = "1 kg";
        sample7.Status = SampleStatus.Normal;
        sample7.ProcessingStatus = "Đã nhận";
        sample7.StorageLocation = "PMT1";
        sample7.Notes = "Mẫu phân bón NPK lô sản xuất PB-2025-1208";
        sample7.TotalIndicatorCost = 380000;
        await _testRequestSampleRepository.InsertAsync(sample7);

        // Add indicators to Sample 7
        var indProtein = allIndicators.FirstOrDefault(i => i.Code == "HL-004");
        if (indNito != null)
        {
            var si16 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample7Id, indNito.Id);
            si16.RequestDate = now.AddDays(-5);
            si16.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si16);
        }
        if (indPH != null)
        {
            var si17 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample7Id, indPH.Id);
            si17.RequestDate = now.AddDays(-5);
            si17.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si17);
        }

        // Specimen 7 - Fertilizer
        var spe7 = new Specimen(_guidGenerator.Create(), req4Id, "SPE-20251208-0007", "Mẫu phân bón NPK 16-16-8", "Phân bón", now.AddDays(-5));
        spe7.Quantity = 1;
        spe7.Unit = "kg";
        spe7.Status = SpecimenStatus.Received;
        spe7.StorageLocation = "Phòng mẫu nhiệt độ thường";
        spe7.Notes = "Mẫu phân bón NPK lô sản xuất PB-2025-1208";
        await _specimenRepository.InsertAsync(spe7);

        // Test Request 5 - Pending (Wastewater)
        var req5Id = _guidGenerator.Create();
        var req5 = new TestRequest(
            req5Id,
            "REQ-20251210-0005",
            now.AddDays(-3),
            "Sở Tài nguyên và Môi trường Đắk Lắk"
        );
        req5.Phone = "0262 3851 234";
        req5.Address = "18 Lê Duẩn, TP. Buôn Ma Thuột, Đắk Lắk";
        req5.Province = "Đắk Lắk";
        req5.District = "TP. Buôn Ma Thuột";
        req5.Status = TestRequestStatus.Pending;
        req5.ExpectedCompletionDate = now.AddDays(4);
        req5.Notes = "Yêu cầu quan trắc định kỳ";
        req5.TotalSampleCount = 1;
        req5.TotalCost = 620000;
        await _testRequestRepository.InsertAsync(req5);

        // Sample 8 for Request 5
        var sample8Id = _guidGenerator.Create();
        var sample8 = new TestRequestSample(sample8Id, req5Id, "M008", "Mẫu nước thải - KCN Hòa Phú");
        sample8.SortOrder = 1;
        sample8.SamplingAddress = "Nhà máy XLNT KCN Hòa Phú, Đầu ra";
        sample8.SampleQuantity = "5 L";
        sample8.Status = SampleStatus.Normal;
        sample8.ProcessingStatus = "Đã nhận";
        sample8.StorageLocation = "KLA1";
        sample8.Notes = "Mẫu nước thải đầu ra nhà máy xử lý nước thải KCN";
        sample8.TotalIndicatorCost = 620000;
        await _testRequestSampleRepository.InsertAsync(sample8);

        // Add indicators to Sample 8
        if (indPH != null)
        {
            var si18 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample8Id, indPH.Id);
            si18.RequestDate = now.AddDays(-3);
            si18.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si18);
        }
        if (indColiform != null)
        {
            var si19 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample8Id, indColiform.Id);
            si19.RequestDate = now.AddDays(-3);
            si19.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si19);
        }
        if (indChi != null)
        {
            var si20 = new TestRequestSampleIndicator(_guidGenerator.Create(), sample8Id, indChi.Id);
            si20.RequestDate = now.AddDays(-3);
            si20.SentToDepartment = false;
            await _testRequestSampleIndicatorRepository.InsertAsync(si20);
        }

        // Specimen 8 - Wastewater
        var spe8 = new Specimen(_guidGenerator.Create(), req5Id, "SPE-20251210-0008", "Mẫu nước thải - KCN Hòa Phú", "Nước thải", now.AddDays(-3));
        spe8.Quantity = 5;
        spe8.Unit = "L";
        spe8.Status = SpecimenStatus.Received;
        spe8.StorageLocation = "Tủ lạnh 2-8°C - Phòng A1";
        spe8.Notes = "Mẫu nước thải đầu ra nhà máy xử lý nước thải KCN";
        await _specimenRepository.InsertAsync(spe8);

        // NCR - Open
        var ncr1 = new NCR(
            _guidGenerator.Create(),
            "NCR-20251205-0001",
            "Mẫu nước ngầm vượt ngưỡng kim loại nặng",
            "Kết quả thử nghiệm cho thấy hàm lượng As trong mẫu nước ngầm vượt QCVN cho phép",
            NCRType.TestResult,
            NCRSeverity.Major,
            userId,
            now.AddDays(2)
        );
        ncr1.Status = NCRStatus.InProgress;
        ncr1.RelatedEntityType = "TestResult";
        ncr1.Department = "Phòng Hóa học";
        ncr1.AssignedToId = userId;
        ncr1.RootCause = "Nhiễm bẩn tự nhiên từ địa chất";
        ncr1.CorrectiveAction = "Khuyến nghị khách hàng xử lý nước trước khi sử dụng";
        ncr1.PreventiveAction = "Tăng cường giám sát vùng có nguy cơ cao";
        await _ncrRepository.InsertAsync(ncr1);

        // NCR - Resolved
        var ncr2 = new NCR(
            _guidGenerator.Create(),
            "NCR-20251128-0001",
            "Sai lệch kết quả do thiết bị chưa hiệu chuẩn",
            "Máy đo pH cho kết quả sai lệch do quá hạn hiệu chuẩn",
            NCRType.Equipment,
            NCRSeverity.Critical,
            userId,
            now.AddDays(-8)
        );
        ncr2.Status = NCRStatus.Resolved;
        ncr2.RelatedEntityType = "Equipment";
        ncr2.Department = "Phòng Hóa học";
        ncr2.AssignedToId = userId;
        ncr2.RootCause = "Không theo dõi lịch hiệu chuẩn thiết bị";
        ncr2.CorrectiveAction = "Hiệu chuẩn lại thiết bị, thử nghiệm lại các mẫu bị ảnh hưởng";
        ncr2.PreventiveAction = "Thiết lập hệ thống nhắc nhở tự động về lịch hiệu chuẩn";
        ncr2.ClosedById = userId;
        ncr2.ClosedAt = now.AddDays(-10);
        ncr2.Notes = "Đã hiệu chuẩn và thử nghiệm lại thành công";
        await _ncrRepository.InsertAsync(ncr2);

        // Task Assignment 1 - In Progress
        var task1 = new TaskAssignment(
            _guidGenerator.Create(),
            "TASK-20251205-0001",
            "Phân tích mẫu nước ngầm - Viện NC Tây Nguyên",
            TaskType.Adhoc,
            TaskPriority.High,
            userId,
            userId,
            "Phòng Hóa học",
            now.AddDays(-1)
        );
        task1.Description = "Thực hiện phân tích các chỉ tiêu nước ngầm theo yêu cầu REQ-20251205-0003";
        task1.Status = TaskAssignmentStatus.InProgress;
        task1.StartDate = now.AddDays(-7);
        task1.Progress = 50;
        await _taskAssignmentRepository.InsertAsync(task1);

        // Task Assignment 2 - In Progress
        var task2 = new TaskAssignment(
            _guidGenerator.Create(),
            "TASK-20251203-0001",
            "Phân tích mẫu đất - HTX Ea H'leo",
            TaskType.Adhoc,
            TaskPriority.Medium,
            userId,
            userId,
            "Phòng Hóa học",
            now.AddDays(-3)
        );
        task2.Description = "Thực hiện phân tích đất theo yêu cầu REQ-20251203-0002";
        task2.Status = TaskAssignmentStatus.InProgress;
        task2.StartDate = now.AddDays(-9);
        task2.Progress = 75;
        await _taskAssignmentRepository.InsertAsync(task2);

        // Task Assignment 3 - Completed
        var task3 = new TaskAssignment(
            _guidGenerator.Create(),
            "TASK-20251201-0001",
            "Phân tích mẫu cà phê - Công ty Cà phê Tây Nguyên",
            TaskType.Adhoc,
            TaskPriority.High,
            userId,
            userId,
            "Phòng Hóa học",
            now.AddDays(-8)
        );
        task3.Description = "Thực hiện phân tích cà phê theo yêu cầu REQ-20251201-0001";
        task3.Status = TaskAssignmentStatus.Completed;
        task3.StartDate = now.AddDays(-11);
        task3.CompletedDate = now.AddDays(-9);
        task3.Progress = 100;
        task3.Notes = "Hoàn thành đúng hạn";
        await _taskAssignmentRepository.InsertAsync(task3);
    }
}
