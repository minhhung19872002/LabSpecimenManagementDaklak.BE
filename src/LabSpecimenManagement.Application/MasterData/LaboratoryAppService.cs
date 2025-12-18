using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class LaboratoryAppService :
    CrudAppService<
        Laboratory,
        LaboratoryDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateLaboratoryDto>,
    ILaboratoryAppService
{
    private readonly IIdentityUserRepository _userRepository;

    public LaboratoryAppService(
        IRepository<Laboratory, Guid> repository,
        IIdentityUserRepository userRepository)
        : base(repository)
    {
        _userRepository = userRepository;
    }

    public async Task<ListResultDto<LaboratoryDto>> GetActiveLaboratoriesAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<Laboratory>, List<LaboratoryDto>>(items);

        // Get user names for technical managers
        var userIds = items
            .Where(x => x.TechnicalManagerId.HasValue)
            .Select(x => x.TechnicalManagerId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Any())
        {
            var users = await _userRepository.GetListByIdsAsync(userIds);
            var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? u.UserName);

            foreach (var dto in dtos)
            {
                if (dto.TechnicalManagerId.HasValue && userDict.TryGetValue(dto.TechnicalManagerId.Value, out var userName))
                {
                    dto.TechnicalManagerName = userName;
                }
            }
        }

        return new ListResultDto<LaboratoryDto>(dtos);
    }

    public async Task<ListResultDto<LaboratoryDto>> GetAllAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<Laboratory>, List<LaboratoryDto>>(items);

        // Get user names for technical managers
        var userIds = items
            .Where(x => x.TechnicalManagerId.HasValue)
            .Select(x => x.TechnicalManagerId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Any())
        {
            var users = await _userRepository.GetListByIdsAsync(userIds);
            var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? u.UserName);

            foreach (var dto in dtos)
            {
                if (dto.TechnicalManagerId.HasValue && userDict.TryGetValue(dto.TechnicalManagerId.Value, out var userName))
                {
                    dto.TechnicalManagerName = userName;
                }
            }
        }

        return new ListResultDto<LaboratoryDto>(dtos);
    }

    public override async Task<LaboratoryDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var dto = ObjectMapper.Map<Laboratory, LaboratoryDto>(entity);

        if (entity.TechnicalManagerId.HasValue)
        {
            var user = await _userRepository.FindAsync(entity.TechnicalManagerId.Value);
            if (user != null)
            {
                dto.TechnicalManagerName = user.Name ?? user.UserName;
            }
        }

        return dto;
    }

    public override async Task<PagedResultDto<LaboratoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = query.Count();

        if (!string.IsNullOrEmpty(input.Sorting))
        {
            query = query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name);
        }

        var items = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = ObjectMapper.Map<List<Laboratory>, List<LaboratoryDto>>(items);

        // Get user names for technical managers
        var userIds = items
            .Where(x => x.TechnicalManagerId.HasValue)
            .Select(x => x.TechnicalManagerId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Any())
        {
            var users = await _userRepository.GetListByIdsAsync(userIds);
            var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? u.UserName);

            foreach (var dto in dtos)
            {
                if (dto.TechnicalManagerId.HasValue && userDict.TryGetValue(dto.TechnicalManagerId.Value, out var userName))
                {
                    dto.TechnicalManagerName = userName;
                }
            }
        }

        return new PagedResultDto<LaboratoryDto>(totalCount, dtos);
    }

    public override async Task<LaboratoryDto> CreateAsync(CreateUpdateLaboratoryDto input)
    {
        // Check for duplicate code
        var existingCode = await Repository.FindAsync(x => x.Code == input.Code);
        if (existingCode != null)
        {
            throw new UserFriendlyException($"Mã khoa phòng '{input.Code}' đã tồn tại!");
        }

        // Check for duplicate name
        var existingName = await Repository.FindAsync(x => x.Name == input.Name);
        if (existingName != null)
        {
            throw new UserFriendlyException($"Tên khoa phòng '{input.Name}' đã tồn tại!");
        }

        return await base.CreateAsync(input);
    }

    public override async Task<LaboratoryDto> UpdateAsync(Guid id, CreateUpdateLaboratoryDto input)
    {
        // Check for duplicate code (excluding current entity)
        var existingCode = await Repository.FindAsync(x => x.Code == input.Code && x.Id != id);
        if (existingCode != null)
        {
            throw new UserFriendlyException($"Mã khoa phòng '{input.Code}' đã tồn tại!");
        }

        // Check for duplicate name (excluding current entity)
        var existingName = await Repository.FindAsync(x => x.Name == input.Name && x.Id != id);
        if (existingName != null)
        {
            throw new UserFriendlyException($"Tên khoa phòng '{input.Name}' đã tồn tại!");
        }

        return await base.UpdateAsync(id, input);
    }
}
