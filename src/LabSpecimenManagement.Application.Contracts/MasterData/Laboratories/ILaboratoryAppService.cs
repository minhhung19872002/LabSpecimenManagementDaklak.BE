using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface ILaboratoryAppService : ICrudAppService<
    LaboratoryDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateLaboratoryDto>
{
    Task<ListResultDto<LaboratoryDto>> GetActiveLaboratoriesAsync();
    Task<ListResultDto<LaboratoryDto>> GetAllAsync();
}
