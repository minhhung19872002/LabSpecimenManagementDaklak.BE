using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.SpecimenHistories;

[RemoteService(IsEnabled = false)]
public class SpecimenHistoryAppService : CrudAppService<
    SpecimenHistory,
    SpecimenHistoryDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateSpecimenHistoryDto,
    UpdateSpecimenHistoryDto>, ISpecimenHistoryAppService
{
    public SpecimenHistoryAppService(IRepository<SpecimenHistory, Guid> repository)
        : base(repository)
    {
    }

    public override async Task<PagedResultDto<SpecimenHistoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Specimen);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = ApplyPaging(queryable, input);
        queryable = ApplySorting(queryable, input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = entities.Select(entity =>
        {
            var dto = ObjectMapper.Map<SpecimenHistory, SpecimenHistoryDto>(entity);
            if (entity.Specimen != null)
            {
                dto.SpecimenCode = entity.Specimen.Code;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<SpecimenHistoryDto>(totalCount, dtos);
    }

    public override async Task<SpecimenHistoryDto> GetAsync(Guid id)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Specimen);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.Id == id)
        );

        if (entity == null)
        {
            throw new Volo.Abp.Domain.Entities.EntityNotFoundException(typeof(SpecimenHistory), id);
        }

        var dto = ObjectMapper.Map<SpecimenHistory, SpecimenHistoryDto>(entity);
        if (entity.Specimen != null)
        {
            dto.SpecimenCode = entity.Specimen.Code;
        }

        return dto;
    }
}
