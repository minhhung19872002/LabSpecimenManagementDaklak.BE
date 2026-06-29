using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Common;

public interface IUnitConversionResolver
{
    Task<decimal> ResolveBaseQuantityAsync(Product product, Guid? unitId, decimal quantity, string itemNameForError);
}

/// <summary>
/// Resolves a quantity expressed in an arbitrary product unit into the product's base-unit quantity,
/// using the 3-tier fallback: base unit -> packaging unit -> ProductUnitConversion lookup.
/// </summary>
public class UnitConversionResolver : IUnitConversionResolver, ITransientDependency
{
    private readonly IRepository<ProductUnitConversion, Guid> _conversionRepository;

    public UnitConversionResolver(IRepository<ProductUnitConversion, Guid> conversionRepository)
    {
        _conversionRepository = conversionRepository;
    }

    public async Task<decimal> ResolveBaseQuantityAsync(Product product, Guid? unitId, decimal quantity, string itemNameForError)
    {
        if (product.BaseUnitId == null || unitId == product.BaseUnitId)
        {
            return quantity;
        }

        if (unitId == product.PackagingUnitId && product.PackagingQuantity > 0)
        {
            return quantity / product.PackagingQuantity.Value;
        }

        var conversionsQuery = await _conversionRepository.GetQueryableAsync();
        var conversion = conversionsQuery.FirstOrDefault(c =>
            c.ProductId == product.Id && c.ConvertedUnitId == unitId);

        if (conversion == null)
            throw new UserFriendlyException($"Không tìm thấy quy đổi đơn vị cho \"{itemNameForError}\". Vui lòng kiểm tra lại.");

        if (conversion.ConvertedQuantity == 0)
            throw new UserFriendlyException($"Quy đổi đơn vị cho \"{itemNameForError}\" có số lượng quy đổi = 0. Vui lòng kiểm tra lại.");

        decimal ratio = conversion.Quantity / conversion.ConvertedQuantity;
        return quantity * ratio;
    }
}
