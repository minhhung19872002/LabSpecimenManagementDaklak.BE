using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface ISpecimenTypeAppService : ICrudAppService<
    SpecimenTypeDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateSpecimenTypeDto>
{
    Task<ListResultDto<SpecimenTypeDto>> GetActiveSpecimenTypesAsync();

    #region Indicator Management

    /// <summary>
    /// Lấy tất cả loại mẫu kèm danh sách chỉ tiêu
    /// </summary>
    Task<List<SpecimenTypeWithIndicatorsDto>> GetAllWithIndicatorsAsync();

    /// <summary>
    /// Lấy 1 loại mẫu kèm danh sách chỉ tiêu
    /// </summary>
    Task<SpecimenTypeWithIndicatorsDto> GetWithIndicatorsAsync(Guid id);

    /// <summary>
    /// Lấy danh sách chỉ tiêu của loại mẫu
    /// </summary>
    Task<List<SpecimenTypeIndicatorDto>> GetIndicatorsAsync(Guid specimenTypeId);

    /// <summary>
    /// Thêm chỉ tiêu vào loại mẫu
    /// </summary>
    Task AddIndicatorsAsync(Guid specimenTypeId, AddIndicatorsToSpecimenTypeDto input);

    /// <summary>
    /// Xóa chỉ tiêu khỏi loại mẫu
    /// </summary>
    Task RemoveIndicatorAsync(Guid specimenTypeId, Guid indicatorId);

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của chỉ tiêu trong loại mẫu
    /// </summary>
    Task<SpecimenTypeIndicatorDto> ToggleIndicatorActiveAsync(Guid specimenTypeId, Guid indicatorId);

    #endregion
}
