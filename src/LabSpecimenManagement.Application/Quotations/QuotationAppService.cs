using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace LabSpecimenManagement.Quotations;

[Authorize(LabSpecimenManagementPermissions.Quotations.Default)]
public class QuotationAppService : ApplicationService, IQuotationAppService
{
    private readonly IRepository<Quotation, Guid> _quotationRepository;
    private readonly IRepository<QuotationItem, Guid> _quotationItemRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IClock _clock;

    public QuotationAppService(
        IRepository<Quotation, Guid> quotationRepository,
        IRepository<QuotationItem, Guid> quotationItemRepository,
        IIdentityUserRepository userRepository,
        IClock clock)
    {
        _quotationRepository = quotationRepository;
        _quotationItemRepository = quotationItemRepository;
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task<PagedResultDto<QuotationListDto>> GetListAsync(GetQuotationListInput input)
    {
        var query = await _quotationRepository.WithDetailsAsync(x => x.Items);

        // Filter
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.ToLower();
            query = query.Where(x =>
                x.QuotationNumber.ToLower().Contains(filter) ||
                x.CustomerName.ToLower().Contains(filter));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(x => x.QuotationDate >= input.FromDate.Value.Date);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(x => x.QuotationDate <= input.ToDate.Value.Date.AddDays(1).AddSeconds(-1));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        // Sort
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            query = query.OrderByDescending(x => x.CreationTime);
        }
        else
        {
            query = query.OrderBy(input.Sorting);
        }

        // Paging
        query = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var quotations = await AsyncExecuter.ToListAsync(query);

        // Get creator user names
        var creatorIds = quotations
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        var creators = creatorIds.Count > 0
            ? await _userRepository.GetListByIdsAsync(creatorIds)
            : new List<IdentityUser>();

        var creatorDict = creators.ToDictionary(x => x.Id, x => x.Name ?? x.UserName);

        var items = quotations.Select(x => new QuotationListDto
        {
            Id = x.Id,
            QuotationNumber = x.QuotationNumber,
            QuotationDate = x.QuotationDate,
            CustomerName = x.CustomerName,
            Phone = x.Phone,
            TotalCost = x.TotalCost,
            Status = x.Status,
            ItemCount = x.Items.Count,
            SampleCount = x.Items.Sum(i => i.SampleCount),
            CreationTime = x.CreationTime,
            CreatorUserName = x.CreatorId.HasValue && creatorDict.ContainsKey(x.CreatorId.Value)
                ? creatorDict[x.CreatorId.Value]
                : null
        }).ToList();

        return new PagedResultDto<QuotationListDto>(totalCount, items);
    }

    public async Task<QuotationDto> GetAsync(Guid id)
    {
        var query = await _quotationRepository.WithDetailsAsync(x => x.Items, x => x.Organization);
        var quotation = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (quotation == null)
        {
            throw new UserFriendlyException("Không tìm thấy báo giá");
        }

        return MapToDto(quotation);
    }

    [Authorize(LabSpecimenManagementPermissions.Quotations.Create)]
    public async Task<QuotationDto> CreateAsync(CreateUpdateQuotationDto input)
    {
        var quotationNumber = await GenerateQuotationNumberAsync();

        var quotation = new Quotation(
            GuidGenerator.Create(),
            quotationNumber,
            input.QuotationDate,
            input.CustomerName)
        {
            OrganizationId = input.OrganizationId,
            ProvinceCode = input.ProvinceCode,
            DistrictCode = input.DistrictCode,
            WardCode = input.WardCode,
            Address = input.Address,
            TaxCode = input.TaxCode,
            ContactPerson = input.ContactPerson,
            Phone = input.Phone,
            Notes = input.Notes,
            Status = input.Status
        };

        // Add items
        foreach (var itemDto in input.Items)
        {
            var item = new QuotationItem(
                GuidGenerator.Create(),
                quotation.Id,
                itemDto.IndicatorId,
                itemDto.TestMethodId,
                itemDto.IndicatorName,
                itemDto.TestMethodName,
                itemDto.SampleCount,
                itemDto.UnitPrice,
                itemDto.SortOrder);

            quotation.Items.Add(item);
        }

        // Calculate total cost
        quotation.RecalculateTotalCost();

        await _quotationRepository.InsertAsync(quotation, autoSave: true);

        return MapToDto(quotation);
    }

    [Authorize(LabSpecimenManagementPermissions.Quotations.Edit)]
    public async Task<QuotationDto> UpdateAsync(Guid id, CreateUpdateQuotationDto input)
    {
        var query = await _quotationRepository.WithDetailsAsync(x => x.Items);
        var quotation = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (quotation == null)
        {
            throw new UserFriendlyException("Không tìm thấy báo giá");
        }

        // Update quotation properties
        quotation.QuotationDate = input.QuotationDate;
        quotation.OrganizationId = input.OrganizationId;
        quotation.CustomerName = input.CustomerName;
        quotation.ProvinceCode = input.ProvinceCode;
        quotation.DistrictCode = input.DistrictCode;
        quotation.WardCode = input.WardCode;
        quotation.Address = input.Address;
        quotation.TaxCode = input.TaxCode;
        quotation.ContactPerson = input.ContactPerson;
        quotation.Phone = input.Phone;
        quotation.Notes = input.Notes;
        quotation.Status = input.Status;

        // Get existing item ids
        var existingItemIds = quotation.Items.Select(x => x.Id).ToList();
        var inputItemIds = input.Items.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToList();

        // Remove items that are not in input
        var itemsToRemove = quotation.Items.Where(x => !inputItemIds.Contains(x.Id)).ToList();
        foreach (var item in itemsToRemove)
        {
            quotation.Items.Remove(item);
            await _quotationItemRepository.DeleteAsync(item);
        }

        // Update existing items and add new items
        foreach (var itemDto in input.Items)
        {
            if (itemDto.Id.HasValue && existingItemIds.Contains(itemDto.Id.Value))
            {
                // Update existing item
                var existingItem = quotation.Items.First(x => x.Id == itemDto.Id.Value);
                existingItem.IndicatorId = itemDto.IndicatorId;
                existingItem.TestMethodId = itemDto.TestMethodId;
                existingItem.IndicatorName = itemDto.IndicatorName;
                existingItem.TestMethodName = itemDto.TestMethodName;
                existingItem.SampleCount = itemDto.SampleCount;
                existingItem.UnitPrice = itemDto.UnitPrice;
                existingItem.SortOrder = itemDto.SortOrder;
                existingItem.RecalculateTotalPrice();
            }
            else
            {
                // Add new item
                var newItem = new QuotationItem(
                    GuidGenerator.Create(),
                    quotation.Id,
                    itemDto.IndicatorId,
                    itemDto.TestMethodId,
                    itemDto.IndicatorName,
                    itemDto.TestMethodName,
                    itemDto.SampleCount,
                    itemDto.UnitPrice,
                    itemDto.SortOrder);

                quotation.Items.Add(newItem);
            }
        }

        // Recalculate total cost
        quotation.RecalculateTotalCost();

        await _quotationRepository.UpdateAsync(quotation, autoSave: true);

        return MapToDto(quotation);
    }

    [Authorize(LabSpecimenManagementPermissions.Quotations.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var quotation = await _quotationRepository.GetAsync(id);
        if (quotation == null)
        {
            throw new UserFriendlyException("Không tìm thấy báo giá");
        }

        await _quotationRepository.DeleteAsync(quotation);
    }

    public async Task<string> GenerateQuotationNumberAsync()
    {
        var today = _clock.Now;
        var dateStr = today.ToString("yyyyMMdd");
        var prefix = $"BG-{dateStr}-";

        // Find the last quotation number for today
        var query = (await _quotationRepository.GetQueryableAsync())
            .Where(x => x.QuotationNumber.StartsWith(prefix))
            .OrderByDescending(x => x.QuotationNumber);

        var lastQuotation = await AsyncExecuter.FirstOrDefaultAsync(query);

        int nextNumber = 1;
        if (lastQuotation != null)
        {
            var lastNumberStr = lastQuotation.QuotationNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    private QuotationDto MapToDto(Quotation quotation)
    {
        return new QuotationDto
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            QuotationDate = quotation.QuotationDate,
            OrganizationId = quotation.OrganizationId,
            OrganizationName = quotation.Organization?.Name,
            CustomerName = quotation.CustomerName,
            ProvinceCode = quotation.ProvinceCode,
            DistrictCode = quotation.DistrictCode,
            WardCode = quotation.WardCode,
            Address = quotation.Address,
            TaxCode = quotation.TaxCode,
            ContactPerson = quotation.ContactPerson,
            Phone = quotation.Phone,
            TotalCost = quotation.TotalCost,
            Notes = quotation.Notes,
            Status = quotation.Status,
            CreationTime = quotation.CreationTime,
            CreatorId = quotation.CreatorId,
            LastModificationTime = quotation.LastModificationTime,
            LastModifierId = quotation.LastModifierId,
            Items = quotation.Items.OrderBy(x => x.SortOrder).Select(item => new QuotationItemDto
            {
                Id = item.Id,
                QuotationId = item.QuotationId,
                IndicatorId = item.IndicatorId,
                TestMethodId = item.TestMethodId,
                IndicatorName = item.IndicatorName,
                TestMethodName = item.TestMethodName,
                SampleCount = item.SampleCount,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                SortOrder = item.SortOrder
            }).ToList()
        };
    }
}
