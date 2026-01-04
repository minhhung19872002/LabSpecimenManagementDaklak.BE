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
}
