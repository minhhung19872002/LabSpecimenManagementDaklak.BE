using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using LabSpecimenManagement.PublishedReports;

namespace LabSpecimenManagement.EntityFrameworkCore.PublishedReports;

public class EfCorePublishedReportRepository
    : EfCoreRepository<LabSpecimenManagementDbContext, PublishedReport, Guid>,
      IPublishedReportRepository
{
    public EfCorePublishedReportRepository(
        IDbContextProvider<LabSpecimenManagementDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<List<PublishedReport>> GetByTestRequestIdAsync(Guid testRequestId)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(x => x.TestRequestId == testRequestId)
            .OrderByDescending(x => x.CreationTime)
            .ToListAsync();
    }

    public async Task<List<PublishedReport>> GetByQualityTaskIdAsync(Guid qualityTaskId)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(x => x.QualityManagementTaskId == qualityTaskId)
            .OrderByDescending(x => x.CreationTime)
            .ToListAsync();
    }
}
