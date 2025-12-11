using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Specimens;

public interface ISpecimenAppService :
    ICrudAppService<
        SpecimenDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateSpecimenDto,
        UpdateSpecimenDto>
{
}
