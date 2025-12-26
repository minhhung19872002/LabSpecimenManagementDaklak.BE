using System;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.QualityManagement;

public interface IQualityManagementTaskRepository : IRepository<QualityManagementTask, Guid>
{
}
