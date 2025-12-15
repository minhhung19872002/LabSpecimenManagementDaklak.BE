using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Specimens;
using LabSpecimenManagement.TestRequests;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.Specimens;

public abstract class SpecimenAppService_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ISpecimenAppService _specimenAppService;

    protected SpecimenAppService_Tests()
    {
        _specimenAppService = GetRequiredService<ISpecimenAppService>();
    }

    [Fact]
    public async Task Should_Get_List_Of_Specimens()
    {
        // Act
        var result = await _specimenAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(3);
        result.Items.ShouldContain(x => x.Code == "SPE-20240101-0001");
    }

    [Fact]
    public async Task Should_Get_Specimen_By_Id()
    {
        // Act
        var result = await _specimenAppService.GetAsync(LabSpecimenManagementTestData.Specimen1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(LabSpecimenManagementTestData.Specimen1Id);
        result.Code.ShouldBe("SPE-20240101-0001");
        result.Name.ShouldBe("Mẫu nước thải #1");
        result.Type.ShouldBe("Nước thải công nghiệp");
        result.Status.ShouldBe(SpecimenStatus.Testing);
    }

    [Fact]
    public async Task Should_Create_Specimen()
    {
        // Arrange
        var input = new CreateSpecimenDto
        {
            TestRequestId = LabSpecimenManagementTestData.TestRequest1Id,
            Name = "Mẫu nước thải #3",
            Type = "Nước thải sinh hoạt",
            ReceivedDate = DateTime.Now,
            Quantity = 1.5m,
            Unit = "lít",
            StorageLocation = "Tủ lạnh 2",
            Notes = "Ghi chú test"
        };

        // Act
        var result = await _specimenAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Code.ShouldStartWith("SPE-");
        result.Name.ShouldBe("Mẫu nước thải #3");
        result.Type.ShouldBe("Nước thải sinh hoạt");
        result.Quantity.ShouldBe(1.5m);
        result.Unit.ShouldBe("lít");
        result.Status.ShouldBe(SpecimenStatus.Received);
    }

    [Fact]
    public async Task Should_Update_Specimen()
    {
        // Arrange
        var input = new UpdateSpecimenDto
        {
            Name = "Mẫu nước thải #1 - Updated",
            Type = "Nước thải công nghiệp loại A",
            StorageLocation = "Tủ lạnh 3",
            Notes = "Ghi chú đã cập nhật",
            Status = SpecimenStatus.Storage,
            Quantity = 0.5m,
            Unit = "lít"
        };

        // Act
        var result = await _specimenAppService.UpdateAsync(LabSpecimenManagementTestData.Specimen1Id, input);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Mẫu nước thải #1 - Updated");
        result.Type.ShouldBe("Nước thải công nghiệp loại A");
        result.StorageLocation.ShouldBe("Tủ lạnh 3");
        result.Status.ShouldBe(SpecimenStatus.Storage);
    }

    [Fact]
    public async Task Should_Delete_Specimen()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await _specimenAppService.DeleteAsync(LabSpecimenManagementTestData.Specimen2Id);
        });

        // Verify deleted
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _specimenAppService.GetAsync(LabSpecimenManagementTestData.Specimen2Id);
        });
    }

    [Fact]
    public async Task Should_Get_Specimens_By_TestRequest()
    {
        // Act
        var result = await _specimenAppService.GetByTestRequestAsync(LabSpecimenManagementTestData.TestRequest1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.Items.ShouldAllBe(x => x.TestRequestId == LabSpecimenManagementTestData.TestRequest1Id);
    }

    [Fact]
    public async Task Should_Create_Many_Specimens()
    {
        // Arrange
        var input = new CreateManySpecimensDto
        {
            TestRequestId = LabSpecimenManagementTestData.TestRequest2Id,
            StorageLocation = "Kho mẫu đất",
            StorageCondition = "Nhiệt độ phòng",
            Description = "Mẫu đất thu thập đợt 2",
            Specimens = new List<SpecimenItemDto>
            {
                new SpecimenItemDto
                {
                    Name = "Mẫu đất #2",
                    Type = "Đất nông nghiệp",
                    ReceivedDate = DateTime.Now,
                    Quantity = 300,
                    Unit = "g"
                },
                new SpecimenItemDto
                {
                    Name = "Mẫu đất #3",
                    Type = "Đất công nghiệp",
                    ReceivedDate = DateTime.Now,
                    Quantity = 400,
                    Unit = "g"
                },
                new SpecimenItemDto
                {
                    Code = "CUSTOM-001",
                    Name = "Mẫu đất #4",
                    Type = "Đất rừng",
                    ReceivedDate = DateTime.Now,
                    Quantity = 500,
                    Unit = "g",
                    Status = SpecimenStatus.Received
                }
            }
        };

        // Act
        var result = await _specimenAppService.CreateManyAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.TotalRequested.ShouldBe(3);
        result.SuccessCount.ShouldBe(3);
        result.FailedCount.ShouldBe(0);
        result.Errors.ShouldBeEmpty();
        result.CreatedSpecimens.Count.ShouldBe(3);

        // Verify auto-generated codes
        result.CreatedSpecimens.Take(2).ShouldAllBe(x => x.Code.StartsWith("SPE-"));

        // Verify custom code
        result.CreatedSpecimens.ShouldContain(x => x.Code == "CUSTOM-001");

        // Verify storage location applied to all
        result.CreatedSpecimens.ShouldAllBe(x => x.StorageLocation == "Kho mẫu đất");
    }

    [Fact]
    public async Task Should_Return_Error_When_TestRequest_Not_Found()
    {
        // Arrange
        var input = new CreateManySpecimensDto
        {
            TestRequestId = Guid.NewGuid(), // Non-existent ID
            Specimens = new List<SpecimenItemDto>
            {
                new SpecimenItemDto
                {
                    Name = "Test Sample",
                    Type = "Test Type",
                    ReceivedDate = DateTime.Now,
                    Quantity = 1,
                    Unit = "mẫu"
                }
            }
        };

        // Act
        var result = await _specimenAppService.CreateManyAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeFalse();
        result.FailedCount.ShouldBe(1);
        result.SuccessCount.ShouldBe(0);
        result.Errors.ShouldNotBeEmpty();
        result.Errors.First().ShouldContain("Không tìm thấy phiếu yêu cầu");
    }

    [Fact]
    public async Task Should_Get_Specimen_With_TestRequestNumber()
    {
        // Act
        var result = await _specimenAppService.GetAsync(LabSpecimenManagementTestData.Specimen1Id);

        // Assert
        result.ShouldNotBeNull();
        result.TestRequestNumber.ShouldBe("TR-20240101-0001");
    }

    [Fact]
    public async Task Should_Generate_Unique_Specimen_Codes()
    {
        // Arrange & Act
        var specimen1 = await _specimenAppService.CreateAsync(new CreateSpecimenDto
        {
            TestRequestId = LabSpecimenManagementTestData.TestRequest1Id,
            Name = "Test Specimen 1",
            Type = "Test Type",
            ReceivedDate = DateTime.Now,
            Quantity = 1,
            Unit = "mẫu"
        });

        var specimen2 = await _specimenAppService.CreateAsync(new CreateSpecimenDto
        {
            TestRequestId = LabSpecimenManagementTestData.TestRequest1Id,
            Name = "Test Specimen 2",
            Type = "Test Type",
            ReceivedDate = DateTime.Now,
            Quantity = 1,
            Unit = "mẫu"
        });

        // Assert
        specimen1.Code.ShouldNotBe(specimen2.Code);
        specimen1.Code.ShouldStartWith("SPE-");
        specimen2.Code.ShouldStartWith("SPE-");
    }
}
