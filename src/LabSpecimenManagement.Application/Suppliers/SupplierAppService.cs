using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
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
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public SupplierAppService(
        IRepository<Supplier, Guid> supplierRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
    {
        _supplierRepository = supplierRepository;
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

        var items = query
            .OrderByDescending(x => x.CreationTime)
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
        await _supplierRepository.DeleteAsync(id);
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

    private async Task ValidateDuplicateCodeAsync(string code, Guid? excludeId = null)
    {
        var query = await _supplierRepository.GetQueryableAsync();
        var normalizedCode = code.Trim().ToLower();

        var exists = query.Any(x =>
            x.Code.ToLower() == normalizedCode &&
            (excludeId == null || x.Id != excludeId));

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
