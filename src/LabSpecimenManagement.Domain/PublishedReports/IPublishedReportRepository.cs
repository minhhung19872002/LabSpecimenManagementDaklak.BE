using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.PublishedReports;

public interface IPublishedReportRepository : IRepository<PublishedReport, Guid>
{
    Task<List<PublishedReport>> GetByTestRequestIdAsync(Guid testRequestId);
    Task<List<PublishedReport>> GetByQualityTaskIdAsync(Guid qualityTaskId);
}
