using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.Units;

/// <summary>
/// Interface cho Unit AppService
/// </summary>
public interface IUnitAppService : ICrudAppService<
    UnitDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateUnitDto>
{
    /// <summary>
    /// Lấy danh sách đơn vị đang hoạt động
    /// </summary>
    Task<ListResultDto<UnitDto>> GetActiveUnitsAsync();

    /// <summary>
    /// Bật/tắt trạng thái hoạt động
    /// </summary>
    Task ToggleActiveAsync(Guid id);

    /// <summary>
    /// Xóa hàng loạt
    /// </summary>
    Task<int> BatchDeleteAsync(List<Guid> ids);

    /// <summary>
    /// Bật/tắt trạng thái hàng loạt
    /// </summary>
    Task<int> BatchToggleActiveAsync(List<Guid> ids, bool isActive);
}
