using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.BatchNorms;
using LabSpecimenManagement.Products;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Danh mục vật tư / thiết bị (Danh mục vật tư thiết bị) để khai báo định mức theo mẻ (L3)
/// và phiếu tạo dự toán năm (L4) — kèm đơn vị tiêu hao hợp lệ và quy cách đóng gói.
/// </summary>
public class NormProductOptionProvider : ITransientDependency
{
    /// <summary>Vật tư = hóa chất, sinh phẩm, vật tư tiêu hao, chất chuẩn, dung môi, dung dịch pha</summary>
    public static readonly ProductType[] MaterialTypes =
    {
        ProductType.Chemical, ProductType.Biological, ProductType.Supply, ProductType.Standard, ProductType.Solvent,
        ProductType.PreparedSolution,
    };

    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _unitConversionRepository;

    public NormProductOptionProvider(
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<ProductUnitConversion, Guid> unitConversionRepository)
    {
        _productRepository = productRepository;
        _unitRepository = unitRepository;
        _unitConversionRepository = unitConversionRepository;
    }

    public async Task<List<BatchNormProductOptionDto>> LoadProductOptionsAsync(ProductType[] types)
    {
        var products = await _productRepository.GetListAsync(p => types.Contains(p.ProductType) && p.IsActive && !p.IsDraft);
        if (products.Count == 0)
        {
            return new List<BatchNormProductOptionDto>();
        }

        var allowed = await LoadAllowedUnitsAsync(products);
        var unitIds = allowed.Values.SelectMany(u => u)
            .Concat(products.Where(p => p.BaseUnitId.HasValue).Select(p => p.BaseUnitId!.Value))
            .Distinct()
            .ToList();
        var units = unitIds.Count == 0
            ? new Dictionary<Guid, Unit>()
            : (await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id))).ToDictionary(u => u.Id);

        return products
            .Select(p =>
            {
                var options = allowed[p.Id]
                    .Where(units.ContainsKey)
                    .Select(id => new BatchNormUnitOptionDto { Id = id, Name = units[id].Name })
                    .ToList();
                var hasPack = p.VolumePerUnit > 0 && p.VolumeUnitId.HasValue && p.BaseUnitId.HasValue;
                return new BatchNormProductOptionDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    FormattedName = p.FormattedName,
                    ProductType = p.ProductType,
                    Categories = p.Categories,
                    Units = options,
                    DefaultUnitId = options.FirstOrDefault()?.Id,
                    VolumePerUnit = hasPack ? p.VolumePerUnit : null,
                    VolumeUnitId = hasPack ? p.VolumeUnitId : null,
                    VolumeUnitName = hasPack ? units.GetValueOrDefault(p.VolumeUnitId!.Value)?.Name : null,
                    BaseUnitName = hasPack ? units.GetValueOrDefault(p.BaseUnitId!.Value)?.Name : null,
                };
            })
            .OrderBy(p => p.ProductType)
            .ThenBy(p => p.Name)
            .ToList();
    }

    /// <summary>
    /// Đơn vị tiêu hao hợp lệ của vật tư — thứ tự: thể tích, cơ bản, đơn vị quy đổi
    /// (đều là đơn vị mà bước quy đổi trừ kho hiểu được).
    /// </summary>
    public async Task<Dictionary<Guid, List<Guid>>> LoadAllowedUnitsAsync(List<Product> products)
    {
        var productIds = products.Select(p => p.Id).ToList();
        var conversions = await _unitConversionRepository.GetListAsync(c => productIds.Contains(c.ProductId));
        var conversionsByProduct = conversions.ToLookup(c => c.ProductId);

        return products.ToDictionary(p => p.Id, p =>
        {
            var ids = new List<Guid?> { p.VolumeUnitId, p.BaseUnitId };
            foreach (var c in conversionsByProduct[p.Id])
            {
                ids.Add(c.ConvertedUnitId);
                ids.Add(c.BaseUnitId);
            }

            return ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        });
    }
}
