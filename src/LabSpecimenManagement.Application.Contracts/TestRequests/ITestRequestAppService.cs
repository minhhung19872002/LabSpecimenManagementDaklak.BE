using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.TestRequests;

public interface ITestRequestAppService :
    ICrudAppService<
        TestRequestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestRequestDto,
        UpdateTestRequestDto>
{
    /// <summary>
    /// Get filtered and paged list of test requests
    /// </summary>
    Task<PagedResultDto<TestRequestDto>> GetFilteredListAsync(GetTestRequestListDto input);
}
