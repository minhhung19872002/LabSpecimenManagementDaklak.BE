using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData.Organizations;

public class OrganizationAppService : CrudAppService<
    Organization,
    OrganizationDto,
    Guid,
    GetOrganizationListDto,
    CreateUpdateOrganizationDto>, IOrganizationAppService
{
    public OrganizationAppService(IRepository<Organization, Guid> repository)
        : base(repository)
    {
    }

    protected override async Task<IQueryable<Organization>> CreateFilteredQueryAsync(GetOrganizationListDto input)
    {
        var query = await base.CreateFilteredQueryAsync(input);

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            query = query.Where(x => x.Name.Contains(input.Search));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        return query;
    }

    protected override IQueryable<Organization> ApplyDefaultSorting(IQueryable<Organization> query)
    {
        return query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name);
    }

    public async Task<List<OrganizationDto>> GetActiveOrganizationsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var list = await AsyncExecuter.ToListAsync(
            query.WhereActive()
                 .OrderByThenBy(x => x.SortOrder, x => x.Name)
        );
        return ObjectMapper.Map<List<Organization>, List<OrganizationDto>>(list);
    }
}
