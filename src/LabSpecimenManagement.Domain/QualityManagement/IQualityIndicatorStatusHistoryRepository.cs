using System;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.QualityManagement;

public interface IQualityIndicatorStatusHistoryRepository : IRepository<QualityIndicatorStatusHistory, Guid>
{
}
