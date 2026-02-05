using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface ITestResultValueAppService : IApplicationService
{
    Task<PagedResultDto<TestResultValueDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<TestResultValueDto> GetAsync(Guid id);
    Task<TestResultValueDto> CreateAsync(CreateUpdateTestResultValueDto input);
    Task<TestResultValueDto> UpdateAsync(Guid id, CreateUpdateTestResultValueDto input);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Lấy danh sách kết quả đang hoạt động
    /// </summary>
    Task<ListResultDto<TestResultValueDto>> GetActiveTestResultValuesAsync();

    /// <summary>
    /// Get or create a test result value by name
    /// Used for auto-creating text values when submitting results for approval
    /// </summary>
    Task<TestResultValueDto?> GetOrCreateByNameAsync(string name);

    /// <summary>
    /// Lấy danh sách kết quả recommend cho chỉ tiêu
    /// - Nếu chỉ tiêu có kết quả riêng (trong junction table): chỉ trả về kết quả của chỉ tiêu đó
    /// - Nếu chỉ tiêu không có kết quả riêng: trả về tất cả kết quả không gắn chỉ tiêu
    /// </summary>
    Task<ListResultDto<TestResultValueDto>> GetResultsForIndicatorAsync(Guid indicatorId);

    #region Indicator Management

    /// <summary>
    /// Lấy danh sách kết quả kèm theo danh sách chỉ tiêu
    /// </summary>
    Task<ListResultDto<TestResultValueDto>> GetWithIndicatorsAsync();

    /// <summary>
    /// Thêm chỉ tiêu vào kết quả thử nghiệm
    /// </summary>
    Task AddIndicatorsAsync(Guid testResultValueId, AddIndicatorsToTestResultValueDto input);

    /// <summary>
    /// Xóa một chỉ tiêu khỏi kết quả thử nghiệm
    /// </summary>
    Task RemoveIndicatorAsync(Guid testResultValueId, Guid indicatorId, Guid specimenTypeId);

    /// <summary>
    /// Toggle trạng thái active của chỉ tiêu trong kết quả thử nghiệm
    /// </summary>
    Task<TestResultValueIndicatorDto> ToggleIndicatorActiveAsync(Guid testResultValueId, Guid indicatorId, Guid specimenTypeId);

    /// <summary>
    /// Xóa nhiều chỉ tiêu khỏi kết quả thử nghiệm (batch)
    /// </summary>
    Task<int> BatchRemoveIndicatorsAsync(Guid testResultValueId, BatchRemoveIndicatorsDto input);

    #endregion
}
