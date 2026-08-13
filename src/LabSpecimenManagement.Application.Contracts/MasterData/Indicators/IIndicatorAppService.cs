using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.Indicators;

/// <summary>
/// Interface cho Indicator AppService
/// </summary>
public interface IIndicatorAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách chỉ tiêu có lọc
    /// </summary>
    Task<PagedResultDto<IndicatorDto>> GetListAsync(IndicatorFilterDto input);

    /// <summary>
    /// Lấy chi tiết chỉ tiêu
    /// </summary>
    Task<IndicatorDto> GetAsync(Guid id);

    /// <summary>
    /// Tạo chỉ tiêu mới
    /// </summary>
    Task<IndicatorDto> CreateAsync(CreateIndicatorDto input);

    /// <summary>
    /// Tạo nhiều chỉ tiêu cùng lúc
    /// </summary>
    Task<List<IndicatorDto>> CreateBulkAsync(CreateBulkIndicatorsDto input);

    /// <summary>
    /// Cập nhật chỉ tiêu
    /// </summary>
    Task<IndicatorDto> UpdateAsync(Guid id, UpdateIndicatorDto input);

    /// <summary>
    /// Xóa chỉ tiêu (kiểm tra đã sử dụng chưa)
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Kiểm tra chỉ tiêu có thể xóa không
    /// </summary>
    Task<bool> CanDeleteAsync(Guid id);

    /// <summary>
    /// Bật/tắt trạng thái hoạt động
    /// </summary>
    Task<IndicatorDto> ToggleActiveAsync(Guid id);

    /// <summary>
    /// Lấy chỉ tiêu theo nhóm
    /// </summary>
    Task<List<IndicatorDto>> GetByGroupAsync(Guid groupId);

    /// <summary>
    /// Lấy danh sách chỉ tiêu đang hoạt động
    /// </summary>
    Task<List<IndicatorDto>> GetActiveIndicatorsAsync(bool includeInactive = false);

    /// <summary>
    /// Import chỉ tiêu từ Excel
    /// </summary>
    Task<ImportIndicatorsResultDto> ImportFromExcelAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Lấy template Excel mẫu
    /// </summary>
    Task<byte[]> GetExcelTemplateAsync();

    /// <summary>
    /// Xóa nhiều chỉ tiêu cùng lúc
    /// </summary>
    Task<BatchOperationResultDto> DeleteBatchAsync(BatchIndicatorIdsDto input);

    /// <summary>
    /// Ngưng hoạt động nhiều chỉ tiêu cùng lúc
    /// </summary>
    Task<BatchOperationResultDto> DeactivateBatchAsync(BatchIndicatorIdsDto input);

    /// <summary>
    /// Kích hoạt nhiều chỉ tiêu cùng lúc
    /// </summary>
    Task<BatchOperationResultDto> ActivateBatchAsync(BatchIndicatorIdsDto input);

    /// <summary>
    /// Lấy danh sách chỉ tiêu với phương pháp thử
    /// </summary>
    Task<List<IndicatorWithMethodsDto>> GetAllWithMethodsAsync();

    /// <summary>
    /// Cập nhật chỉ tiêu cha + quản lý chỉ tiêu phụ (thêm/sửa/xóa)
    /// </summary>
    Task<IndicatorDto> UpdateWithSubIndicatorsAsync(Guid id, UpdateIndicatorWithSubsDto input);
}
