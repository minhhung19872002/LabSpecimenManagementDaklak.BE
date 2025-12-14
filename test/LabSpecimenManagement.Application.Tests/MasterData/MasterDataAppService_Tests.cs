using System;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.MasterData;

public abstract class MasterDataAppService_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IDepartmentAppService _departmentAppService;
    private readonly ISpecimenTypeAppService _specimenTypeAppService;
    private readonly ITestMethodAppService _testMethodAppService;
    private readonly IStorageLocationAppService _storageLocationAppService;
    private readonly ICustomerAppService _customerAppService;

    protected MasterDataAppService_Tests()
    {
        _departmentAppService = GetRequiredService<IDepartmentAppService>();
        _specimenTypeAppService = GetRequiredService<ISpecimenTypeAppService>();
        _testMethodAppService = GetRequiredService<ITestMethodAppService>();
        _storageLocationAppService = GetRequiredService<IStorageLocationAppService>();
        _customerAppService = GetRequiredService<ICustomerAppService>();
    }

    #region Department Tests

    [Fact]
    public async Task Should_Get_List_Of_Departments()
    {
        // Act
        var result = await _departmentAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Get_Department_By_Id()
    {
        // Act
        var result = await _departmentAppService.GetAsync(LabSpecimenManagementTestData.Department1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("DEP-001");
        result.Name.ShouldBe("Phòng Hóa học");
    }

    [Fact]
    public async Task Should_Create_Department()
    {
        // Arrange
        var input = new CreateUpdateDepartmentDto
        {
            Code = "DEP-003",
            Name = "Phòng Vật lý",
            Description = "Phòng thử nghiệm vật lý",
            IsActive = true
        };

        // Act
        var result = await _departmentAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Code.ShouldBe("DEP-003");
        result.Name.ShouldBe("Phòng Vật lý");
        result.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Update_Department()
    {
        // Arrange
        var input = new CreateUpdateDepartmentDto
        {
            Code = "DEP-001",
            Name = "Phòng Hóa học - Updated",
            Description = "Mô tả đã cập nhật",
            IsActive = true
        };

        // Act
        var result = await _departmentAppService.UpdateAsync(LabSpecimenManagementTestData.Department1Id, input);

        // Assert
        result.Name.ShouldBe("Phòng Hóa học - Updated");
    }

    [Fact]
    public async Task Should_Delete_Department()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await _departmentAppService.DeleteAsync(LabSpecimenManagementTestData.Department2Id);
        });
    }

    [Fact]
    public async Task Should_Get_Active_Departments()
    {
        // Act
        var result = await _departmentAppService.GetActiveDepartmentsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(x => x.IsActive);
    }

    #endregion

    #region SpecimenType Tests

    [Fact]
    public async Task Should_Get_List_Of_SpecimenTypes()
    {
        // Act
        var result = await _specimenTypeAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Get_SpecimenType_By_Id()
    {
        // Act
        var result = await _specimenTypeAppService.GetAsync(LabSpecimenManagementTestData.SpecimenType1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("ST-001");
        result.Name.ShouldBe("Nước");
    }

    [Fact]
    public async Task Should_Create_SpecimenType()
    {
        // Arrange
        var input = new CreateUpdateSpecimenTypeDto
        {
            Code = "ST-003",
            Name = "Không khí",
            Description = "Mẫu không khí",
            IsActive = true
        };

        // Act
        var result = await _specimenTypeAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("ST-003");
        result.Name.ShouldBe("Không khí");
    }

    [Fact]
    public async Task Should_Get_Active_SpecimenTypes()
    {
        // Act
        var result = await _specimenTypeAppService.GetActiveSpecimenTypesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(x => x.IsActive);
    }

    #endregion

    #region TestMethod Tests

    [Fact]
    public async Task Should_Get_List_Of_TestMethods()
    {
        // Act
        var result = await _testMethodAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Get_TestMethod_By_Id()
    {
        // Act
        var result = await _testMethodAppService.GetAsync(LabSpecimenManagementTestData.TestMethod1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("TM-001");
        result.Name.ShouldBe("TCVN 6492:2011");
    }

    [Fact]
    public async Task Should_Create_TestMethod()
    {
        // Arrange
        var input = new CreateUpdateTestMethodDto
        {
            Code = "TM-003",
            Name = "TCVN 5501:2010",
            Description = "Phương pháp xác định COD",
            IsActive = true
        };

        // Act
        var result = await _testMethodAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("TM-003");
        result.Name.ShouldBe("TCVN 5501:2010");
    }

    [Fact]
    public async Task Should_Get_Active_TestMethods()
    {
        // Act
        var result = await _testMethodAppService.GetActiveTestMethodsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(x => x.IsActive);
    }

    #endregion

    #region StorageLocation Tests

    [Fact]
    public async Task Should_Get_List_Of_StorageLocations()
    {
        // Act
        var result = await _storageLocationAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Get_StorageLocation_By_Id()
    {
        // Act
        var result = await _storageLocationAppService.GetAsync(LabSpecimenManagementTestData.StorageLocation1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("SL-001");
        result.Name.ShouldBe("Tủ lạnh 1");
    }

    [Fact]
    public async Task Should_Create_StorageLocation()
    {
        // Arrange
        var input = new CreateUpdateStorageLocationDto
        {
            Code = "SL-003",
            Name = "Tủ đông",
            Description = "Tủ đông bảo quản mẫu dài hạn",
            Temperature = "-20°C",
            IsActive = true
        };

        // Act
        var result = await _storageLocationAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("SL-003");
        result.Name.ShouldBe("Tủ đông");
        result.Temperature.ShouldBe("-20°C");
    }

    [Fact]
    public async Task Should_Get_Active_StorageLocations()
    {
        // Act
        var result = await _storageLocationAppService.GetActiveStorageLocationsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(x => x.IsActive);
    }

    #endregion

    #region Customer Tests

    [Fact]
    public async Task Should_Get_List_Of_Customers()
    {
        // Act
        var result = await _customerAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Get_Customer_By_Id()
    {
        // Act
        var result = await _customerAppService.GetAsync(LabSpecimenManagementTestData.Customer1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("CUS-001");
        result.Name.ShouldBe("Công ty TNHH ABC");
    }

    [Fact]
    public async Task Should_Create_Customer()
    {
        // Arrange
        var input = new CreateUpdateCustomerDto
        {
            Name = "Công ty DEF",
            Email = "def@company.com",
            Phone = "0903456789",
            Address = "789 Đường DEF, Quận 3, TP.HCM",
            ContactPerson = "Lê Văn C",
            IsActive = true
        };

        // Act
        var result = await _customerAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Công ty DEF");
        result.Email.ShouldBe("def@company.com");
    }

    [Fact]
    public async Task Should_Update_Customer()
    {
        // Arrange
        var input = new CreateUpdateCustomerDto
        {
            Name = "Công ty TNHH ABC - Updated",
            Email = "updated@abc.com",
            Phone = "0901234567",
            IsActive = true
        };

        // Act
        var result = await _customerAppService.UpdateAsync(LabSpecimenManagementTestData.Customer1Id, input);

        // Assert
        result.Name.ShouldBe("Công ty TNHH ABC - Updated");
        result.Email.ShouldBe("updated@abc.com");
    }

    [Fact]
    public async Task Should_Get_Active_Customers()
    {
        // Act
        var result = await _customerAppService.GetActiveCustomersAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldAllBe(x => x.IsActive);
    }

    #endregion
}
