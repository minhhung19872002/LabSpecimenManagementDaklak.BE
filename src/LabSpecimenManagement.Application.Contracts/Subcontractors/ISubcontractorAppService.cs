using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Subcontractors;

public interface ISubcontractorAppService : ICrudAppService<
    SubcontractorDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateSubcontractorDto>
{
    Task<PagedResultDto<SubcontractorDto>> GetFilteredListAsync(GetSubcontractorListInput input);
    Task<ListResultDto<SubcontractorDto>> GetActiveSubcontractorsAsync();
    Task<ListResultDto<SubcontractorCertificationDto>> GetExpiringCertificationsAsync(int daysAhead = 90);
}
