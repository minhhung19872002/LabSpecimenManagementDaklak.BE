using System;
using System.Threading.Tasks;
using LabSpecimenManagement.NCRs;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.NCRs;

public abstract class NCRAppService_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly INCRAppService _ncrAppService;

    protected NCRAppService_Tests()
    {
        _ncrAppService = GetRequiredService<INCRAppService>();
    }

    [Fact]
    public async Task Should_Get_List_Of_NCRs()
    {
        // Act
        var result = await _ncrAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Should_Get_NCR_By_Id()
    {
        // Act
        var result = await _ncrAppService.GetAsync(LabSpecimenManagementTestData.NCR1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(LabSpecimenManagementTestData.NCR1Id);
        result.NCRNumber.ShouldBe("NCR-20240101-0001");
        result.Title.ShouldBe("Sai lệch quy trình lấy mẫu");
        result.Type.ShouldBe(NCRType.Process);
        result.Severity.ShouldBe(NCRSeverity.Major);
        result.Status.ShouldBe(NCRStatus.Open);
    }

    [Fact]
    public async Task Should_Create_NCR()
    {
        // Arrange
        var input = new CreateNCRDto
        {
            Title = "NCR Test - Thiết bị lỗi",
            Description = "Máy đo pH không hoạt động đúng",
            Type = NCRType.Equipment,
            Severity = NCRSeverity.Minor,
            Department = "Phòng Hóa học",
            DueDate = DateTime.Now.AddDays(7),
            Notes = "Cần kiểm tra lại thiết bị"
        };

        // Act
        var result = await _ncrAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.NCRNumber.ShouldStartWith("NCR-");
        result.Title.ShouldBe("NCR Test - Thiết bị lỗi");
        result.Type.ShouldBe(NCRType.Equipment);
        result.Severity.ShouldBe(NCRSeverity.Minor);
        result.Status.ShouldBe(NCRStatus.Open);
    }

    [Fact]
    public async Task Should_Update_NCR()
    {
        // Arrange
        var input = new UpdateNCRDto
        {
            Title = "Sai lệch quy trình lấy mẫu - Updated",
            Description = "Mẫu nước không được bảo quản đúng nhiệt độ quy định - đã xác định nguyên nhân",
            Type = NCRType.Process,
            Severity = NCRSeverity.Major,
            Status = NCRStatus.InProgress,
            RootCause = "Nhân viên không tuân thủ SOP",
            CorrectiveAction = "Đào tạo lại nhân viên về quy trình bảo quản",
            PreventiveAction = "Tăng cường kiểm tra định kỳ"
        };

        // Act
        var result = await _ncrAppService.UpdateAsync(LabSpecimenManagementTestData.NCR1Id, input);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Sai lệch quy trình lấy mẫu - Updated");
        result.Status.ShouldBe(NCRStatus.InProgress);
        result.RootCause.ShouldBe("Nhân viên không tuân thủ SOP");
    }

    [Fact]
    public async Task Should_Get_Open_NCRs()
    {
        // Act
        var result = await _ncrAppService.GetOpenNCRsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(x => x.Status == NCRStatus.Open || x.Status == NCRStatus.InProgress);
    }

    [Fact]
    public async Task Should_Assign_NCR()
    {
        // Arrange
        var assigneeId = Guid.NewGuid();

        // Act
        var result = await _ncrAppService.AssignAsync(LabSpecimenManagementTestData.NCR1Id, assigneeId);

        // Assert
        result.ShouldNotBeNull();
        result.AssignedToId.ShouldBe(assigneeId);
    }

    [Fact]
    public async Task Should_Close_NCR()
    {
        // Create a new NCR to close
        var newNCR = await _ncrAppService.CreateAsync(new CreateNCRDto
        {
            Title = "NCR to close",
            Description = "This NCR will be closed",
            Type = NCRType.Other,
            Severity = NCRSeverity.Minor,
            DueDate = DateTime.Now.AddDays(1),
            Department = "Test"
        });

        // Update to add required fields
        await _ncrAppService.UpdateAsync(newNCR.Id, new UpdateNCRDto
        {
            Title = newNCR.Title,
            Description = newNCR.Description,
            Type = newNCR.Type,
            Severity = newNCR.Severity,
            Status = NCRStatus.Resolved,
            RootCause = "Test root cause",
            CorrectiveAction = "Test corrective action",
            PreventiveAction = "Test preventive action"
        });

        // Act
        var result = await _ncrAppService.CloseAsync(newNCR.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(NCRStatus.Closed);
        result.ClosedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Delete_NCR()
    {
        // Create a new NCR to delete
        var newNCR = await _ncrAppService.CreateAsync(new CreateNCRDto
        {
            Title = "NCR to delete",
            Description = "This NCR will be deleted",
            Type = NCRType.Other,
            Severity = NCRSeverity.Minor,
            DueDate = DateTime.Now.AddDays(1),
            Department = "Test"
        });

        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await _ncrAppService.DeleteAsync(newNCR.Id);
        });

        // Verify deleted
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _ncrAppService.GetAsync(newNCR.Id);
        });
    }
}
