using System;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.TestResults;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for TestResult entity
/// </summary>
public class TestResultSyncHandler : ISyncEntityHandler, ITransientDependency
{
    private readonly ITestResultAppService _testResultAppService;
    private readonly IRepository<TestResult, Guid> _testResultRepository;

    public TestResultSyncHandler(
        ITestResultAppService testResultAppService,
        IRepository<TestResult, Guid> testResultRepository)
    {
        _testResultAppService = testResultAppService;
        _testResultRepository = testResultRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.TestResult;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateTestResultDto>(data);
        var result = await _testResultAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        var updateDto = DeserializeData<UpdateTestResultDto>(data);
        var result = await _testResultAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _testResultAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _testResultAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _testResultRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _testResultRepository.FindAsync(serverId);
        return entity != null;
    }

    private static T DeserializeData<T>(object data) => SyncDataSerializer.Deserialize<T>(data);
}
