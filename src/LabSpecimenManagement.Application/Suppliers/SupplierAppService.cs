using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Suppliers;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.Suppliers.Default)]
public class SupplierAppService : ApplicationService, ISupplierAppService
{
    private readonly IRepository<Supplier, Guid> _supplierRepository;
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public SupplierAppService(
        IRepository<Supplier, Guid> supplierRepository,
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
    {
        _supplierRepository = supplierRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
    }

    public async Task<PagedResultDto<SupplierDto>> GetListAsync(SupplierFilterDto input)
    {
        var query = await _supplierRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(input.SupplierGroup))
        {
            query = query.Where(x => x.SupplierGroup == input.SupplierGroup);
        }

        var totalCount = query.Count();

        IQueryable<Supplier> sortedQuery;
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            var sorting = input.Sorting.Trim().ToLower();
            sortedQuery = sorting switch
            {
                "name asc" => query.OrderBy(x => x.Name),
                "name desc" => query.OrderByDescending(x => x.Name),
                "code asc" => query.OrderBy(x => x.Code),
                "code desc" => query.OrderByDescending(x => x.Code),
                "suppliergroup asc" => query.OrderBy(x => x.SupplierGroup),
                "suppliergroup desc" => query.OrderByDescending(x => x.SupplierGroup),
                "creationtime asc" => query.OrderBy(x => x.CreationTime),
                _ => query.OrderByDescending(x => x.CreationTime),
            };
        }
        else
        {
            sortedQuery = query.OrderByDescending(x => x.CreationTime);
        }

        var items = sortedQuery
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<SupplierDto>(totalCount, dtos);
    }

    public async Task<SupplierDto> GetAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetAsync(id);
        return MapToDto(supplier);
    }

    [Authorize(LabSpecimenManagementPermissions.Suppliers.Create)]
    public async Task<SupplierDto> CreateAsync(CreateUpdateSupplierDto input)
    {
        await ValidateDuplicateNameAsync(input.Name);

        var code = !string.IsNullOrWhiteSpace(input.Code)
            ? input.Code.Trim()
            : await GenerateSupplierCodeAsync();

        await ValidateDuplicateCodeAsync(code);

        var supplier = new Supplier(GuidGenerator.Create(), code, input.Name)
        {
            TaxCode = input.TaxCode,
            Phone = input.Phone,
            Email = input.Email,
            Address = input.Address,
            LogoFileId = input.LogoFileId,
            SupplierGroup = input.SupplierGroup,
            Note = input.Note,
            IsActive = input.IsActive,
        };

        await _supplierRepository.InsertAsync(supplier, autoSave: true);

        return MapToDto(supplier);
    }

    [Authorize(LabSpecimenManagementPermissions.Suppliers.Edit)]
    public async Task<SupplierDto> UpdateAsync(Guid id, CreateUpdateSupplierDto input)
    {
        await ValidateDuplicateNameAsync(input.Name, id);

        var supplier = await _supplierRepository.GetAsync(id);

        // Only validate code if changed
        var newCode = !string.IsNullOrWhiteSpace(input.Code) ? input.Code.Trim() : supplier.Code;
        if (newCode != supplier.Code)
        {
            await ValidateDuplicateCodeAsync(newCode, id);
            supplier.Code = newCode;
        }

        supplier.Name = input.Name;
        supplier.TaxCode = input.TaxCode;
        supplier.Phone = input.Phone;
        supplier.Email = input.Email;
        supplier.Address = input.Address;
        supplier.LogoFileId = input.LogoFileId;
        supplier.SupplierGroup = input.SupplierGroup;
        supplier.Note = input.Note;
        supplier.IsActive = input.IsActive;

        await _supplierRepository.UpdateAsync(supplier, autoSave: true);

        return MapToDto(supplier);
    }

    [Authorize(LabSpecimenManagementPermissions.Suppliers.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await CheckSupplierNotReferencedAsync(id);
        await _supplierRepository.DeleteAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.Suppliers.Delete)]
    public async Task<int> BatchDeleteAsync(List<Guid> ids)
    {
        if (ids == null || !ids.Any()) return 0;

        foreach (var id in ids)
        {
            await CheckSupplierNotReferencedAsync(id);
        }

        var count = 0;
        foreach (var id in ids)
        {
            await _supplierRepository.DeleteAsync(id);
            count++;
        }
        return count;
    }

    [Authorize(LabSpecimenManagementPermissions.Suppliers.Edit)]
    public async Task ToggleActiveAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetAsync(id);
        supplier.IsActive = !supplier.IsActive;
        await _supplierRepository.UpdateAsync(supplier);
    }

    public async Task<List<string>> GetSupplierGroupsAsync()
    {
        var query = await _supplierRepository.GetQueryableAsync();
        return query
            .Where(x => x.SupplierGroup != null && x.SupplierGroup != "")
            .Select(x => x.SupplierGroup!)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    #region Private Methods

    private async Task CheckSupplierNotReferencedAsync(Guid supplierId)
    {
        var query = await _goodsReceiptRepository.GetQueryableAsync();
        var isReferenced = query.Any(x => x.SupplierId == supplierId);
        if (isReferenced)
        {
            throw new UserFriendlyException("Không thể xóa nhà cung cấp này vì đang được sử dụng trong phiếu nhập hàng.");
        }
    }

    private async Task<string> GenerateSupplierCodeAsync()
    {
        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix("SUP");
            var query = await _supplierRepository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate("SUP", count);
        }
    }

    private async Task ValidateDuplicateNameAsync(string name, Guid? excludeId = null)
    {
        var query = await _supplierRepository.GetQueryableAsync();
        var normalizedName = name.Trim().ToLower();

        var exists = await AsyncExecuter.AnyAsync(query.Where(x =>
            x.Name.ToLower() == normalizedName &&
            (excludeId == null || x.Id != excludeId)));

        if (exists)
        {
            throw new UserFriendlyException("Tên nhà cung cấp đã tồn tại. Vui lòng chọn tên khác.");
        }
    }

    private async Task ValidateDuplicateCodeAsync(string code, Guid? excludeId = null)
    {
        var query = await _supplierRepository.GetQueryableAsync();
        var normalizedCode = code.Trim().ToLower();

        var exists = await AsyncExecuter.AnyAsync(query.Where(x =>
            x.Code.ToLower() == normalizedCode &&
            (excludeId == null || x.Id != excludeId)));

        if (exists)
        {
            throw new UserFriendlyException("Mã nhà cung cấp đã tồn tại. Vui lòng chọn mã khác.");
        }
    }

    private SupplierDto MapToDto(Supplier supplier)
    {
        var dto = ObjectMapper.Map<Supplier, SupplierDto>(supplier);
        if (!string.IsNullOrWhiteSpace(supplier.LogoFileId))
        {
            dto.LogoUrl = $"/api/v1/files/{supplier.LogoFileId}";
        }
        return dto;
    }

    #endregion
}
