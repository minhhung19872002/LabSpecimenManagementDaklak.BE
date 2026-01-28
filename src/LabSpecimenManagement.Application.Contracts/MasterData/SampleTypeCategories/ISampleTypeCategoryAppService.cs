using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.SampleTypeCategories;

/// <summary>
/// Interface cho SampleTypeCategory AppService
/// </summary>
public interface ISampleTypeCategoryAppService : ICrudAppService<
    SampleTypeCategoryDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateSampleTypeCategoryDto>
{
    /// <summary>
    /// Lấy danh sách danh mục loại mẫu đang hoạt động
    /// </summary>
    Task<ListResultDto<SampleTypeCategoryDto>> GetActiveCategoriesAsync();

    /// <summary>
    /// Lấy danh sách danh mục loại mẫu kèm số lượng loại mẫu
    /// </summary>
    Task<ListResultDto<SampleTypeCategoryDto>> GetCategoriesWithCountAsync();

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của danh mục (cascade sang tất cả loại mẫu bên trong)
    /// </summary>
    Task ToggleActiveAsync(Guid id);
}
