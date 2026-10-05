using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for TestRequest entity
/// </summary>
public class TestRequestSyncHandler : ISyncEntityHandler, ITransientDependency
{
    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;

    public TestRequestSyncHandler(
        ITestRequestAppService testRequestAppService,
        IRepository<TestRequest, Guid> testRequestRepository)
    {
        _testRequestAppService = testRequestAppService;
        _testRequestRepository = testRequestRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.TestRequest;

    public bool UsesClientIdAsServerId => true;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateTestRequestDto>(data);
        await EnsureUniqueContractNumberAsync(createDto);
        var result = await _testRequestAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    /// <summary>
    /// Số HĐ của phiếu tạo offline chỉ là số tạm: dạng OFFLINE-... (tạo khi mất mạng) hoặc số đã lấy trước khi
    /// mất mạng nhưng trong lúc offline đã bị phiếu khác dùng → cấp số mới theo dãy tuần tự,
    /// đổi luôn tiền tố mã mẫu "&lt;số cũ&gt;/001" cho khớp số mới.
    /// </summary>
    private async Task EnsureUniqueContractNumberAsync(CreateTestRequestDto dto)
    {
        var oldNumber = dto.ContractNumber?.Trim();
        if (string.IsNullOrEmpty(oldNumber))
        {
            return;
        }

        var isTemporary = oldNumber.StartsWith(TestRequestAppService.OfflineContractNumberPrefix, StringComparison.OrdinalIgnoreCase);
        if (!isTemporary && !await _testRequestAppService.CheckContractNumberExistsAsync(oldNumber))
        {
            return;
        }

        TestRequestAppService.ReplaceContractNumber(dto, oldNumber, await _testRequestAppService.GenerateContractNumberAsync());
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        // Payload là UpdateTestRequestDto từ form sửa (giống PUT online) - mẫu/chỉ tiêu kèm Id.
        // Không đi qua CreateTestRequestDto: mất Id → BE xoá hết mẫu/chỉ tiêu rồi tạo lại (đã gửi phòng thì bị đánh dấu khách huỷ).
        var updateDto = DeserializeData<UpdateTestRequestDto>(data);
        var result = await _testRequestAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _testRequestAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _testRequestAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _testRequestRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _testRequestRepository.FindAsync(serverId);
        return entity != null;
    }

    private static T DeserializeData<T>(object data) => SyncDataSerializer.Deserialize<T>(data);
}
