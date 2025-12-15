using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Specimens;

public interface ISpecimenAppService :
    ICrudAppService<
        SpecimenDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateSpecimenDto,
        UpdateSpecimenDto>
{
    /// <summary>
    /// Get filtered and paged list of specimens
    /// </summary>
    Task<PagedResultDto<SpecimenDto>> GetFilteredListAsync(GetSpecimenListDto input);

    /// <summary>
    /// Lấy danh sách mẫu theo phiếu yêu cầu
    /// </summary>
    Task<ListResultDto<SpecimenDto>> GetByTestRequestAsync(Guid testRequestId);

    /// <summary>
    /// Tạo nhiều mẫu cùng lúc cho một phiếu yêu cầu
    /// </summary>
    Task<CreateManySpecimensResultDto> CreateManyAsync(CreateManySpecimensDto input);
}
