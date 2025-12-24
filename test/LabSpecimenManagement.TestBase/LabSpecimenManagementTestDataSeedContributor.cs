using System;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace LabSpecimenManagement;

public class LabSpecimenManagementTestDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<Specimen, Guid> _specimenRepository;
    private readonly IRepository<Test, Guid> _testRepository;
    private readonly IRepository<TestResult, Guid> _testResultRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<StorageLocation, Guid> _storageLocationRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<NCR, Guid> _ncrRepository;
    private readonly IRepository<TaskAssignment, Guid> _taskAssignmentRepository;
    private readonly IGuidGenerator _guidGenerator;

    public LabSpecimenManagementTestDataSeedContributor(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IRepository<TestResult, Guid> testResultRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<StorageLocation, Guid> storageLocationRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<NCR, Guid> ncrRepository,
        IRepository<TaskAssignment, Guid> taskAssignmentRepository,
        IGuidGenerator guidGenerator)
    {
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _testResultRepository = testResultRepository;
        _departmentRepository = departmentRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _testMethodRepository = testMethodRepository;
        _storageLocationRepository = storageLocationRepository;
        _customerRepository = customerRepository;
        _ncrRepository = ncrRepository;
        _taskAssignmentRepository = taskAssignmentRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedMasterDataAsync();
        await SeedTestRequestsAsync();
        await SeedNCRsAsync();
        await SeedTaskAssignmentsAsync();
    }

    private async Task SeedMasterDataAsync()
    {
        // Seed Departments
        if (await _departmentRepository.GetCountAsync() == 0)
        {
            var dept1 = new Department(LabSpecimenManagementTestData.Department1Id, "DEP-001", "Phòng Hóa học");
            dept1.Description = "Phòng thử nghiệm hóa học";
            await _departmentRepository.InsertAsync(dept1);

            var dept2 = new Department(LabSpecimenManagementTestData.Department2Id, "DEP-002", "Phòng Vi sinh");
            dept2.Description = "Phòng thử nghiệm vi sinh";
            await _departmentRepository.InsertAsync(dept2);
        }

        // Seed Specimen Types
        if (await _specimenTypeRepository.GetCountAsync() == 0)
        {
            var st1 = new SpecimenType(LabSpecimenManagementTestData.SpecimenType1Id, "ST-001", "Nước");
            st1.Description = "Mẫu nước các loại";
            await _specimenTypeRepository.InsertAsync(st1);

            var st2 = new SpecimenType(LabSpecimenManagementTestData.SpecimenType2Id, "ST-002", "Đất");
            st2.Description = "Mẫu đất các loại";
            await _specimenTypeRepository.InsertAsync(st2);
        }

        // Seed Test Methods
        if (await _testMethodRepository.GetCountAsync() == 0)
        {
            var tm1 = new TestMethod(LabSpecimenManagementTestData.TestMethod1Id, "TM-001", "TCVN 6492:2011 - Phương pháp đo pH", "");
            await _testMethodRepository.InsertAsync(tm1);

            var tm2 = new TestMethod(LabSpecimenManagementTestData.TestMethod2Id, "TM-002", "TCVN 6625:2000 - Phương pháp xác định TSS", "mg/L");
            await _testMethodRepository.InsertAsync(tm2);
        }

        // Seed Storage Locations
        if (await _storageLocationRepository.GetCountAsync() == 0)
        {
            var sl1 = new StorageLocation(LabSpecimenManagementTestData.StorageLocation1Id, "SL-001", "Tủ lạnh 1");
            sl1.Description = "Tủ lạnh bảo quản mẫu nước, nhiệt độ 4°C";
            sl1.Temperature = "4°C";
            await _storageLocationRepository.InsertAsync(sl1);

            var sl2 = new StorageLocation(LabSpecimenManagementTestData.StorageLocation2Id, "SL-002", "Kho mẫu đất");
            sl2.Description = "Kho bảo quản mẫu đất, nhiệt độ phòng";
            sl2.Temperature = "25°C";
            await _storageLocationRepository.InsertAsync(sl2);
        }

        // Seed Customers
        if (await _customerRepository.GetCountAsync() == 0)
        {
            var cust1 = new Customer(LabSpecimenManagementTestData.Customer1Id, "CUS-001", "Công ty TNHH ABC");
            cust1.ContactPerson = "Nguyễn Văn A";
            cust1.Email = "abc@company.com";
            cust1.Phone = "0901234567";
            cust1.Address = "123 Đường ABC, Quận 1, TP.HCM";
            await _customerRepository.InsertAsync(cust1);

            var cust2 = new Customer(LabSpecimenManagementTestData.Customer2Id, "CUS-002", "Công ty CP XYZ");
            cust2.ContactPerson = "Trần Thị B";
            cust2.Email = "xyz@company.com";
            cust2.Phone = "0902345678";
            cust2.Address = "456 Đường XYZ, Quận 2, TP.HCM";
            await _customerRepository.InsertAsync(cust2);
        }
    }

    private async Task SeedTestRequestsAsync()
    {
        if (await _testRequestRepository.GetCountAsync() > 0)
        {
            return;
        }

        // Create Test Request 1
        var testRequest1 = new TestRequest(
            LabSpecimenManagementTestData.TestRequest1Id,
            "TR-20240101-0001",
            DateTime.Now.AddDays(-5),
            "Công ty TNHH ABC"
        );
        testRequest1.Phone = "0901234567";
        testRequest1.Status = TestRequestStatus.InProgress;
        testRequest1.Notes = "Yêu cầu thử nghiệm mẫu nước thải";
        await _testRequestRepository.InsertAsync(testRequest1);

        // Create Test Request 2
        var testRequest2 = new TestRequest(
            LabSpecimenManagementTestData.TestRequest2Id,
            "TR-20240101-0002",
            DateTime.Now.AddDays(-3),
            "Công ty CP XYZ"
        );
        testRequest2.Phone = "0902345678";
        testRequest2.Status = TestRequestStatus.Pending;
        testRequest2.Notes = "Yêu cầu thử nghiệm mẫu đất";
        await _testRequestRepository.InsertAsync(testRequest2);

        // Create Specimens for Test Request 1
        var specimen1 = new Specimen(
            LabSpecimenManagementTestData.Specimen1Id,
            LabSpecimenManagementTestData.TestRequest1Id,
            "SPE-20240101-0001",
            "Mẫu nước thải #1",
            "Nước thải công nghiệp",
            DateTime.Now.AddDays(-5)
        );
        specimen1.Status = SpecimenStatus.Testing;
        specimen1.Quantity = 1;
        specimen1.Unit = "lít";
        specimen1.StorageLocation = "Tủ lạnh 1";
        await _specimenRepository.InsertAsync(specimen1);

        var specimen2 = new Specimen(
            LabSpecimenManagementTestData.Specimen2Id,
            LabSpecimenManagementTestData.TestRequest1Id,
            "SPE-20240101-0002",
            "Mẫu nước thải #2",
            "Nước thải sinh hoạt",
            DateTime.Now.AddDays(-5)
        );
        specimen2.Status = SpecimenStatus.Received;
        specimen2.Quantity = 2;
        specimen2.Unit = "lít";
        await _specimenRepository.InsertAsync(specimen2);

        // Create Specimen for Test Request 2
        var specimen3 = new Specimen(
            LabSpecimenManagementTestData.Specimen3Id,
            LabSpecimenManagementTestData.TestRequest2Id,
            "SPE-20240101-0003",
            "Mẫu đất #1",
            "Đất nông nghiệp",
            DateTime.Now.AddDays(-3)
        );
        specimen3.Status = SpecimenStatus.Received;
        specimen3.Quantity = 500;
        specimen3.Unit = "g";
        await _specimenRepository.InsertAsync(specimen3);

        // Create Tests for Specimen 1
        var test1 = new Test(
            LabSpecimenManagementTestData.Test1Id,
            LabSpecimenManagementTestData.Specimen1Id,
            "TEST-20240101-0001",
            "Đo pH",
            "TCVN 6492:2011",
            "TCVN 6492:2011"
        );
        test1.Status = TestStatus.InProgress;
        test1.AssigneeId = Guid.NewGuid();
        await _testRepository.InsertAsync(test1);

        var test2 = new Test(
            LabSpecimenManagementTestData.Test2Id,
            LabSpecimenManagementTestData.Specimen1Id,
            "TEST-20240101-0002",
            "Xác định TSS",
            "TCVN 6625:2000",
            "TCVN 6625:2000"
        );
        test2.Status = TestStatus.Pending;
        await _testRepository.InsertAsync(test2);

        // Create Test Result for Test 1
        var testResult1 = new TestResult(
            LabSpecimenManagementTestData.TestResult1Id,
            LabSpecimenManagementTestData.Test1Id,
            "pH",
            "7.2",
            ""
        );
        testResult1.LowerLimit = 6.5m;
        testResult1.UpperLimit = 8.5m;
        testResult1.IsPass = true;
        testResult1.EnteredById = Guid.NewGuid();
        testResult1.EntryDate = DateTime.Now.AddDays(-1);
        testResult1.ApprovalStatus = ResultApprovalStatus.Draft;
        await _testResultRepository.InsertAsync(testResult1);
    }

    private async Task SeedNCRsAsync()
    {
        if (await _ncrRepository.GetCountAsync() > 0)
        {
            return;
        }

        var userId = Guid.NewGuid();
        var ncr1 = new NCR(
            LabSpecimenManagementTestData.NCR1Id,
            "NCR-20240101-0001",
            "Sai lệch quy trình lấy mẫu",
            "Mẫu nước không được bảo quản đúng nhiệt độ quy định",
            NCRType.Process,
            NCRSeverity.Major,
            userId,
            DateTime.Now.AddDays(7)
        );
        ncr1.Status = NCRStatus.Open;
        ncr1.RelatedEntityType = "Specimen";
        ncr1.RelatedEntityId = LabSpecimenManagementTestData.Specimen1Id;
        await _ncrRepository.InsertAsync(ncr1);
    }

    private async Task SeedTaskAssignmentsAsync()
    {
        if (await _taskAssignmentRepository.GetCountAsync() > 0)
        {
            return;
        }

        var userId = Guid.NewGuid();
        var task1 = new TaskAssignment(
            LabSpecimenManagementTestData.TaskAssignment1Id,
            "TASK-20240101-0001",
            "Thực hiện phép thử pH",
            TaskType.Adhoc,
            TaskPriority.High,
            userId,
            userId,
            "Phòng Hóa học",
            DateTime.Now.AddDays(2)
        );
        task1.Status = TaskAssignmentStatus.InProgress;
        task1.RelatedTestIds = $"[\"{LabSpecimenManagementTestData.Test1Id}\"]";
        await _taskAssignmentRepository.InsertAsync(task1);
    }
}

/// <summary>
/// Test data constants for unit tests
/// </summary>
public static class LabSpecimenManagementTestData
{
    // Master Data IDs
    public static Guid Department1Id { get; } = Guid.Parse("3a001001-0001-0001-0001-000000000001");
    public static Guid Department2Id { get; } = Guid.Parse("3a001001-0001-0001-0001-000000000002");
    public static Guid SpecimenType1Id { get; } = Guid.Parse("3a001002-0001-0001-0001-000000000001");
    public static Guid SpecimenType2Id { get; } = Guid.Parse("3a001002-0001-0001-0001-000000000002");
    public static Guid TestMethod1Id { get; } = Guid.Parse("3a001003-0001-0001-0001-000000000001");
    public static Guid TestMethod2Id { get; } = Guid.Parse("3a001003-0001-0001-0001-000000000002");
    public static Guid StorageLocation1Id { get; } = Guid.Parse("3a001004-0001-0001-0001-000000000001");
    public static Guid StorageLocation2Id { get; } = Guid.Parse("3a001004-0001-0001-0001-000000000002");
    public static Guid Customer1Id { get; } = Guid.Parse("3a001005-0001-0001-0001-000000000001");
    public static Guid Customer2Id { get; } = Guid.Parse("3a001005-0001-0001-0001-000000000002");

    // Test Request IDs
    public static Guid TestRequest1Id { get; } = Guid.Parse("3a002001-0001-0001-0001-000000000001");
    public static Guid TestRequest2Id { get; } = Guid.Parse("3a002001-0001-0001-0001-000000000002");

    // Specimen IDs
    public static Guid Specimen1Id { get; } = Guid.Parse("3a003001-0001-0001-0001-000000000001");
    public static Guid Specimen2Id { get; } = Guid.Parse("3a003001-0001-0001-0001-000000000002");
    public static Guid Specimen3Id { get; } = Guid.Parse("3a003001-0001-0001-0001-000000000003");

    // Test IDs
    public static Guid Test1Id { get; } = Guid.Parse("3a004001-0001-0001-0001-000000000001");
    public static Guid Test2Id { get; } = Guid.Parse("3a004001-0001-0001-0001-000000000002");

    // Test Result IDs
    public static Guid TestResult1Id { get; } = Guid.Parse("3a005001-0001-0001-0001-000000000001");

    // NCR IDs
    public static Guid NCR1Id { get; } = Guid.Parse("3a006001-0001-0001-0001-000000000001");

    // Task Assignment IDs
    public static Guid TaskAssignment1Id { get; } = Guid.Parse("3a007001-0001-0001-0001-000000000001");
}
