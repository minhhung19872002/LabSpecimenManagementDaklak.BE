using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Chat;

public class ContactAppService : ApplicationService, IContactAppService
{
    private readonly IRepository<ChatContact, Guid> _contactRepository;
    private readonly IRepository<ChatChannel, Guid> _channelRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;

    public ContactAppService(
        IRepository<ChatContact, Guid> contactRepository,
        IRepository<ChatChannel, Guid> channelRepository,
        IRepository<Customer, Guid> customerRepository)
    {
        _contactRepository = contactRepository;
        _channelRepository = channelRepository;
        _customerRepository = customerRepository;
    }

    public async Task<PagedResultDto<ChatContactDto>> GetListAsync(PagedResultRequestDto input)
    {
        var queryable = await _contactRepository.GetQueryableAsync();

        queryable = queryable.OrderByDescending(c => c.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        var dtos = new List<ChatContactDto>();
        foreach (var item in items)
        {
            var dto = await MapToContactDtoAsync(item);
            dtos.Add(dto);
        }

        return new PagedResultDto<ChatContactDto>(totalCount, dtos);
    }

    public async Task<ChatContactDto> GetAsync(Guid id)
    {
        var contact = await _contactRepository.GetAsync(id);
        return await MapToContactDtoAsync(contact);
    }

    public async Task<ChatContactDto> LinkToCustomerAsync(LinkContactToCustomerInput input)
    {
        var contact = await _contactRepository.GetAsync(input.ContactId);

        // Verify customer exists
        var customer = await _customerRepository.GetAsync(input.CustomerId);

        contact.CustomerId = customer.Id;
        await _contactRepository.UpdateAsync(contact);

        return await MapToContactDtoAsync(contact);
    }

    public async Task<ChatContactDto> UnlinkFromCustomerAsync(Guid contactId)
    {
        var contact = await _contactRepository.GetAsync(contactId);

        contact.CustomerId = null;
        await _contactRepository.UpdateAsync(contact);

        return await MapToContactDtoAsync(contact);
    }

    #region Private Methods

    private async Task<ChatContactDto> MapToContactDtoAsync(ChatContact entity)
    {
        string? customerName = null;
        if (entity.CustomerId.HasValue)
        {
            var customer = await _customerRepository.FindAsync(entity.CustomerId.Value);
            customerName = customer?.Name;
        }

        // Load channel
        var channel = await _channelRepository.FindAsync(entity.ChannelId);

        return new ChatContactDto
        {
            Id = entity.Id,
            ChannelId = entity.ChannelId,
            ChannelName = channel?.Name ?? string.Empty,
            ChannelType = channel?.Type ?? ChannelType.Zalo,
            ExternalId = entity.ExternalId,
            DisplayName = entity.DisplayName,
            Avatar = entity.Avatar,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            CustomerId = entity.CustomerId,
            CustomerName = customerName,
            CreationTime = entity.CreationTime
        };
    }

    #endregion
}
