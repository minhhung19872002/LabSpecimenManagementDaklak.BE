using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.Organizations;

public interface IOrganizationAppService : ICrudAppService<
    OrganizationDto,
    Guid,
    GetOrganizationListDto,
    CreateUpdateOrganizationDto>
{
    Task<List<OrganizationDto>> GetActiveOrganizationsAsync();
}
