using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class SpecimenTypeAppService :
    CrudAppService<
        SpecimenType,
        SpecimenTypeDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSpecimenTypeDto>,
    ISpecimenTypeAppService
{
    public SpecimenTypeAppService(IRepository<SpecimenType, Guid> repository)
        : base(repository)
    {
    }

    public async Task<ListResultDto<SpecimenTypeDto>> GetActiveSpecimenTypesAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<SpecimenTypeDto>(
            ObjectMapper.Map<List<SpecimenType>, List<SpecimenTypeDto>>(items)
        );
    }
}
