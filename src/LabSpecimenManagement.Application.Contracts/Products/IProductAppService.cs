using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Products;

/// <summary>
/// Interface cho Product AppService
/// </summary>
public interface IProductAppService : IApplicationService
{
    Task<PagedResultDto<ProductDto>> GetListAsync(ProductFilterDto input);
    Task<ProductDto> GetAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateUpdateProductDto input);
    Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input);
    Task DeleteAsync(Guid id);
    Task ToggleActiveAsync(Guid id);
    Task<ListResultDto<ProductDto>> GetActiveProductsAsync();
    Task DeleteAttachmentAsync(Guid productId, Guid attachmentId);
    Task<PagedResultDto<ProductSourceDto>> GetProductSourcesAsync(Guid productId, GetProductSourcesInput input);
}
