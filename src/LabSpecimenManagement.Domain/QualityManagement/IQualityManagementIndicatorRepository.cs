using System;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.QualityManagement;

public interface IQualityManagementIndicatorRepository : IRepository<QualityManagementIndicator, Guid>
{
}
