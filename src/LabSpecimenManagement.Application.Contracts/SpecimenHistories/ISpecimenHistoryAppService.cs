using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.SpecimenHistories;

public interface ISpecimenHistoryAppService : ICrudAppService<
    SpecimenHistoryDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateSpecimenHistoryDto,
    UpdateSpecimenHistoryDto>
{
}
