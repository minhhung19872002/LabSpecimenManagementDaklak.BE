using System;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.TestRequests;

public interface ISampleIndicatorNoteRepository : IRepository<SampleIndicatorNote, Guid>
{
}
