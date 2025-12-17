using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.TestRequests;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.TestRequests;

public abstract class TestRequestAppService_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ITestRequestAppService _testRequestAppService;

    protected TestRequestAppService_Tests()
    {
        _testRequestAppService = GetRequiredService<ITestRequestAppService>();
    }

    [Fact]
    public async Task Should_Get_List_Of_TestRequests()
    {
        // Act
        var result = await _testRequestAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
        result.Items.ShouldContain(x => x.RequestNumber == "TR-20240101-0001");
        result.Items.ShouldContain(x => x.RequestNumber == "TR-20240101-0002");
    }

    [Fact]
    public async Task Should_Get_TestRequest_By_Id()
    {
        // Act
        var result = await _testRequestAppService.GetAsync(LabSpecimenManagementTestData.TestRequest1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(LabSpecimenManagementTestData.TestRequest1Id);
        result.RequestNumber.ShouldBe("TR-20240101-0001");
        result.CustomerName.ShouldBe("Công ty TNHH ABC");
        result.Phone.ShouldBe("0901234567");
        result.Status.ShouldBe(TestRequestStatus.InProgress);
    }

    [Fact]
    public async Task Should_Create_TestRequest()
    {
        // Arrange
        var input = new CreateTestRequestDto
        {
            RequestDate = DateTime.Now,
            CustomerName = "Công ty Test ABC",
            Phone = "0903456789",
            Address = "123 Đường Test",
            Notes = "Ghi chú yêu cầu thử nghiệm",
            ExpectedCompletionDate = DateTime.Now.AddDays(7),
            Samples = new List<CreateTestRequestSampleDto>()
        };

        // Act
        var result = await _testRequestAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.RequestNumber.ShouldStartWith("TR-");
        result.CustomerName.ShouldBe("Công ty Test ABC");
        result.Phone.ShouldBe("0903456789");
        result.Status.ShouldBe(TestRequestStatus.Pending);
    }

    [Fact]
    public async Task Should_Update_TestRequest()
    {
        // Arrange
        var input = new UpdateTestRequestDto
        {
            RequestDate = DateTime.Now.AddDays(-5),
            CustomerName = "Công ty TNHH ABC - Updated",
            Phone = "0904567890",
            Address = "Địa chỉ đã cập nhật",
            Notes = "Ghi chú đã cập nhật",
            Status = TestRequestStatus.Completed,
            ExpectedCompletionDate = DateTime.Now.AddDays(3),
            Samples = new List<UpdateTestRequestSampleDto>()
        };

        // Act
        var result = await _testRequestAppService.UpdateAsync(LabSpecimenManagementTestData.TestRequest1Id, input);

        // Assert
        result.ShouldNotBeNull();
        result.CustomerName.ShouldBe("Công ty TNHH ABC - Updated");
        result.Phone.ShouldBe("0904567890");
        result.Status.ShouldBe(TestRequestStatus.Completed);
    }

    [Fact]
    public async Task Should_Delete_TestRequest()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await _testRequestAppService.DeleteAsync(LabSpecimenManagementTestData.TestRequest2Id);
        });

        // Verify deleted
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _testRequestAppService.GetAsync(LabSpecimenManagementTestData.TestRequest2Id);
        });
    }

    [Fact]
    public async Task Should_Generate_Unique_RequestNumbers()
    {
        // Arrange & Act
        var request1 = await _testRequestAppService.CreateAsync(new CreateTestRequestDto
        {
            RequestDate = DateTime.Now,
            CustomerName = "Customer 1",
            Samples = new List<CreateTestRequestSampleDto>()
        });

        var request2 = await _testRequestAppService.CreateAsync(new CreateTestRequestDto
        {
            RequestDate = DateTime.Now,
            CustomerName = "Customer 2",
            Samples = new List<CreateTestRequestSampleDto>()
        });

        // Assert
        request1.RequestNumber.ShouldNotBe(request2.RequestNumber);
        request1.RequestNumber.ShouldStartWith("TR-");
        request2.RequestNumber.ShouldStartWith("TR-");
    }

    [Fact]
    public async Task Should_Create_TestRequest_With_Pending_Status()
    {
        // Arrange
        var input = new CreateTestRequestDto
        {
            RequestDate = DateTime.Now,
            CustomerName = "New Customer",
            Samples = new List<CreateTestRequestSampleDto>()
        };

        // Act
        var result = await _testRequestAppService.CreateAsync(input);

        // Assert
        result.Status.ShouldBe(TestRequestStatus.Pending);
    }

    [Fact]
    public async Task Should_Get_Paged_TestRequests()
    {
        // Arrange
        var input = new PagedAndSortedResultRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 1
        };

        // Act
        var result = await _testRequestAppService.GetListAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Create_TestRequest_With_Samples()
    {
        // Arrange
        var input = new CreateTestRequestDto
        {
            RequestDate = DateTime.Now,
            CustomerName = "Customer With Samples",
            Samples = new List<CreateTestRequestSampleDto>
            {
                new CreateTestRequestSampleDto
                {
                    SortOrder = 1,
                    SampleCode = "SAMPLE-001",
                    SampleName = "Mẫu test 1",
                    SamplingAddress = "Địa chỉ lấy mẫu 1",
                    SampleQuantity = "1kg",
                    Status = SampleStatus.Normal,
                    Indicators = new List<CreateTestRequestSampleIndicatorDto>()
                },
                new CreateTestRequestSampleDto
                {
                    SortOrder = 2,
                    SampleCode = "SAMPLE-002",
                    SampleName = "Mẫu test 2",
                    SamplingAddress = "Địa chỉ lấy mẫu 2",
                    SampleQuantity = "500g",
                    Status = SampleStatus.Normal,
                    Indicators = new List<CreateTestRequestSampleIndicatorDto>()
                }
            }
        };

        // Act
        var result = await _testRequestAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Samples.Count.ShouldBe(2);
        result.Samples.ShouldContain(s => s.SampleCode == "SAMPLE-001");
        result.Samples.ShouldContain(s => s.SampleCode == "SAMPLE-002");
    }
}
