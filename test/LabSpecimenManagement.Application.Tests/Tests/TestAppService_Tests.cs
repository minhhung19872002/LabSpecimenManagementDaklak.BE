using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Tests;
using LabSpecimenManagement.TestRequests;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.Tests;

public abstract class TestAppService_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ITestAppService _testAppService;

    protected TestAppService_Tests()
    {
        _testAppService = GetRequiredService<ITestAppService>();
    }

    [Fact]
    public async Task Should_Get_List_Of_Tests()
    {
        // Act
        var result = await _testAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Get_Test_By_Id()
    {
        // Act
        var result = await _testAppService.GetAsync(LabSpecimenManagementTestData.Test1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(LabSpecimenManagementTestData.Test1Id);
        result.Name.ShouldBe("Đo pH");
        result.Method.ShouldBe("TCVN 6492:2011");
        result.Status.ShouldBe(TestStatus.InProgress);
        result.SpecimenId.ShouldBe(LabSpecimenManagementTestData.Specimen1Id);
    }

    [Fact]
    public async Task Should_Create_Test()
    {
        // Arrange
        var input = new CreateTestDto
        {
            SpecimenId = LabSpecimenManagementTestData.Specimen1Id,
            Name = "Xác định BOD5",
            Method = "Phương pháp pha loãng và cấy",
            Standard = "TCVN 6001-1:2008",
            EstimatedDuration = 5,
            Notes = "Thời gian ủ 5 ngày"
        };

        // Act
        var result = await _testAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Code.ShouldStartWith("TEST-");
        result.Name.ShouldBe("Xác định BOD5");
        result.Method.ShouldBe("Phương pháp pha loãng và cấy");
        result.Standard.ShouldBe("TCVN 6001-1:2008");
        result.Status.ShouldBe(TestStatus.Pending);
    }

    [Fact]
    public async Task Should_Update_Test()
    {
        // Arrange
        var input = new UpdateTestDto
        {
            Name = "Đo pH - Updated",
            Method = "TCVN 6492:2011 (Updated)",
            Standard = "TCVN 6492:2011",
            Status = TestStatus.Completed,
            StartDate = DateTime.Now.AddDays(-1),
            CompletionDate = DateTime.Now,
            Notes = "Hoàn thành thử nghiệm"
        };

        // Act
        var result = await _testAppService.UpdateAsync(LabSpecimenManagementTestData.Test1Id, input);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Đo pH - Updated");
        result.Method.ShouldBe("TCVN 6492:2011 (Updated)");
        result.Status.ShouldBe(TestStatus.Completed);
    }

    [Fact]
    public async Task Should_Delete_Test()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await _testAppService.DeleteAsync(LabSpecimenManagementTestData.Test2Id);
        });

        // Verify deleted
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _testAppService.GetAsync(LabSpecimenManagementTestData.Test2Id);
        });
    }

    [Fact]
    public async Task Should_Get_Tests_By_Specimen()
    {
        // Act
        var result = await _testAppService.GetBySpecimenAsync(LabSpecimenManagementTestData.Specimen1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.Items.ShouldAllBe(x => x.SpecimenId == LabSpecimenManagementTestData.Specimen1Id);
    }

    [Fact]
    public async Task Should_Get_Test_With_SpecimenInfo()
    {
        // Act
        var result = await _testAppService.GetAsync(LabSpecimenManagementTestData.Test1Id);

        // Assert
        result.ShouldNotBeNull();
        result.SpecimenCode.ShouldBe("SPE-20240101-0001");
        result.SpecimenName.ShouldBe("Mẫu nước thải #1");
    }

    [Fact]
    public async Task Should_Generate_Unique_TestCodes()
    {
        // Arrange & Act
        var test1 = await _testAppService.CreateAsync(new CreateTestDto
        {
            SpecimenId = LabSpecimenManagementTestData.Specimen1Id,
            Name = "Test 1",
            Method = "Method 1",
            Standard = "Standard 1"
        });

        var test2 = await _testAppService.CreateAsync(new CreateTestDto
        {
            SpecimenId = LabSpecimenManagementTestData.Specimen1Id,
            Name = "Test 2",
            Method = "Method 2",
            Standard = "Standard 2"
        });

        // Assert
        test1.Code.ShouldNotBe(test2.Code);
        test1.Code.ShouldStartWith("TEST-");
        test2.Code.ShouldStartWith("TEST-");
    }

    [Fact]
    public async Task Should_Create_Test_With_Pending_Status()
    {
        // Arrange
        var input = new CreateTestDto
        {
            SpecimenId = LabSpecimenManagementTestData.Specimen1Id,
            Name = "New Test",
            Method = "Test Method",
            Standard = "Test Standard"
        };

        // Act
        var result = await _testAppService.CreateAsync(input);

        // Assert
        result.Status.ShouldBe(TestStatus.Pending);
    }

    [Fact]
    public async Task Should_Assign_Test_To_User()
    {
        // Arrange
        var assigneeId = Guid.NewGuid();
        var input = new UpdateTestDto
        {
            Name = "Đo pH",
            Method = "TCVN 6492:2011",
            Standard = "TCVN 6492:2011",
            Status = TestStatus.Assigned,
            AssigneeId = assigneeId,
            AssignmentDate = DateTime.Now
        };

        // Act
        var result = await _testAppService.UpdateAsync(LabSpecimenManagementTestData.Test1Id, input);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(TestStatus.Assigned);
        result.AssigneeId.ShouldBe(assigneeId);
        result.AssignmentDate.ShouldNotBeNull();
    }
}
