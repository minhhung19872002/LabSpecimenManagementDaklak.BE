using System;
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
    private readonly IRepository<Specimen, Guid> _specimenRepository;
    private readonly IRepository<Test, Guid> _testRepository;
    private readonly IRepository<TestResult, Guid> _testResultRepository;
    private readonly IRepository<NCR, Guid> _ncrRepository;
    private readonly IRepository<TaskAssignment, Guid> _taskAssignmentRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IConfiguration _configuration;

    public LabSampleDataSeedContributor(
        IRepository<Department, Guid> departmentRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<StorageLocation, Guid> storageLocationRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IRepository<TestResult, Guid> testResultRepository,
        IRepository<NCR, Guid> ncrRepository,
        IRepository<TaskAssignment, Guid> taskAssignmentRepository,
        IGuidGenerator guidGenerator,
        IConfiguration configuration)
    {
        _departmentRepository = departmentRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _testMethodRepository = testMethodRepository;
        _storageLocationRepository = storageLocationRepository;
        _customerRepository = customerRepository;
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _testResultRepository = testResultRepository;
        _ncrRepository = ncrRepository;
        _taskAssignmentRepository = taskAssignmentRepository;
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
        await SeedTestRequestsAndRelatedDataAsync();
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
        var methods = new[]
        {
            new { Code = "TCVN6492", Name = "TCVN 6492:2011 - Xác định pH", Standard = "QCVN 08-MT:2015/BTNMT" },
            new { Code = "TCVN4559", Name = "TCVN 4559:1998 - Xác định EC", Standard = "QCVN 08-MT:2015/BTNMT" },
            new { Code = "TCVN6638", Name = "TCVN 6638:2000 - Xác định Nitơ tổng", Standard = "QCVN 08-MT:2015/BTNMT" },
            new { Code = "TCVN6187", Name = "TCVN 6187-1:2019 - Xác định Coliform", Standard = "QCVN 09-MT:2015/BTNMT" },
            new { Code = "AOAC950", Name = "AOAC 950.40 - Xác định Caffeine", Standard = "TCVN 5253:1990" },
            new { Code = "TCVN6193", Name = "TCVN 6193:1996 - Xác định kim loại nặng", Standard = "QCVN 08-MT:2015/BTNMT" },
            new { Code = "TCVN5979", Name = "TCVN 5979:2007 - Xác định pH đất", Standard = "TCVN 5979:2007" },
            new { Code = "TCVN6498", Name = "TCVN 6498:1999 - Xác định Nitơ tổng đất", Standard = "TCVN 6498:1999" },
            new { Code = "TCVN6928", Name = "TCVN 6928:2007 - Xác định độ ẩm cà phê", Standard = "TCVN 4193:2014" },
            new { Code = "TCVN4325", Name = "TCVN 4325:2007 - Xác định chất béo", Standard = "TCVN 4325:2007" },
            new { Code = "TCVN5501", Name = "TCVN 5501:2010 - Xác định protein", Standard = "TCVN 5501:2010" },
            new { Code = "ISO17025", Name = "ISO 17025:2017 - Phương pháp chuẩn PTN", Standard = "ISO 17025:2017" },
            new { Code = "TCVN7538", Name = "TCVN 7538-1:2006 - Xác định độ ẩm đất", Standard = "TCVN 7538-1:2006" },
            new { Code = "SMEWW4500", Name = "SMEWW 4500-NO3 - Xác định Nitrat", Standard = "QCVN 08-MT:2015/BTNMT" },
            new { Code = "SMEWW5210", Name = "SMEWW 5210B - Xác định BOD5", Standard = "QCVN 40:2011/BTNMT" },
            new { Code = "SMEWW5220", Name = "SMEWW 5220C - Xác định COD", Standard = "QCVN 40:2011/BTNMT" },
        };

        foreach (var method in methods)
        {
            var existing = await _testMethodRepository.FirstOrDefaultAsync(x => x.Code == method.Code);
            if (existing != null)
            {
                // Update Standard if missing
                if (string.IsNullOrEmpty(existing.Standard))
                {
                    existing.Standard = method.Standard;
                    await _testMethodRepository.UpdateAsync(existing);
                }
            }
            else
            {
                // Insert new
                var entity = new TestMethod(_guidGenerator.Create(), method.Code, method.Name);
                entity.Standard = method.Standard;
                await _testMethodRepository.InsertAsync(entity);
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

    private async Task SeedTestRequestsAndRelatedDataAsync()
    {
        if (await _testRequestRepository.GetCountAsync() > 0) return;

        var now = DateTime.Now;
        var userId = Guid.NewGuid(); // Placeholder for user reference

        // Test Request 1 - Completed
        var req1Id = _guidGenerator.Create();
        var req1 = new TestRequest(
            req1Id,
            "REQ-20251201-0001",
            now.AddDays(-12),
            "Công ty TNHH Cà phê Tây Nguyên",
            "info@capheTN.com.vn",
            "0262 3852 115"
        );
        req1.Status = TestRequestStatus.Completed;
        req1.ExpectedCompletionDate = now.AddDays(-5);
        req1.Notes = "Khách hàng yêu cầu kết quả gấp";
        await _testRequestRepository.InsertAsync(req1);

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
            "HTX Nông nghiệp Ea H'leo",
            "htxeahleo@gmail.com",
            "0262 3777 222"
        );
        req2.Status = TestRequestStatus.InProgress;
        req2.ExpectedCompletionDate = now.AddDays(-3);
        await _testRequestRepository.InsertAsync(req2);

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
            "Viện Nghiên cứu Khoa học Tây Nguyên",
            "viennc@taynguyen.gov.vn",
            "0262 3856 789"
        );
        req3.Status = TestRequestStatus.InProgress;
        req3.ExpectedCompletionDate = now.AddDays(-1);
        req3.Notes = "Mẫu nghiên cứu đặc biệt";
        await _testRequestRepository.InsertAsync(req3);

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
            "Công ty CP Phân bón Đắk Lắk",
            "phanbondaklak@gmail.com",
            "0262 3859 999"
        );
        req4.Status = TestRequestStatus.Pending;
        req4.ExpectedCompletionDate = now.AddDays(2);
        await _testRequestRepository.InsertAsync(req4);

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
            "Sở Tài nguyên và Môi trường Đắk Lắk",
            "stnmt@daklak.gov.vn",
            "0262 3851 234"
        );
        req5.Status = TestRequestStatus.Pending;
        req5.ExpectedCompletionDate = now.AddDays(4);
        req5.Notes = "Yêu cầu quan trắc định kỳ";
        await _testRequestRepository.InsertAsync(req5);

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
