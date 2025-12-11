using System;
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
}
