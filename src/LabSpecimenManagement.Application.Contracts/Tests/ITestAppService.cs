using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Tests;

public interface ITestAppService :
    ICrudAppService<
        TestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestDto,
        UpdateTestDto>
{
    /// <summary>
    /// Get filtered and paged list of tests
    /// </summary>
    Task<PagedResultDto<TestDto>> GetFilteredListAsync(GetTestListDto input);

    Task<ListResultDto<TestDto>> GetBySpecimenAsync(Guid specimenId);
}
