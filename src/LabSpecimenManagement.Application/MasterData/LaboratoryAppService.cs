using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

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
    private readonly IUserNameProvider _userNameProvider;
    private readonly IEntityValidationService _validationService;

    public LaboratoryAppService(
        IRepository<Laboratory, Guid> repository,
        IUserNameProvider userNameProvider,
        IEntityValidationService validationService)
        : base(repository)
    {
        _userNameProvider = userNameProvider;
        _validationService = validationService;
    }

    public async Task<ListResultDto<LaboratoryDto>> GetActiveLaboratoriesAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderByThenBy(x => x.SortOrder, x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<Laboratory>, List<LaboratoryDto>>(items);
        await EnrichWithTechnicalManagerNamesAsync(dtos);

        return new ListResultDto<LaboratoryDto>(dtos);
    }

    public async Task<ListResultDto<LaboratoryDto>> GetAllAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .OrderByThenBy(x => x.SortOrder, x => x.Name)
            .ToList();

        var dtos = ObjectMapper.Map<List<Laboratory>, List<LaboratoryDto>>(items);
        await EnrichWithTechnicalManagerNamesAsync(dtos);

        return new ListResultDto<LaboratoryDto>(dtos);
    }

    public override async Task<LaboratoryDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var dto = ObjectMapper.Map<Laboratory, LaboratoryDto>(entity);

        dto.TechnicalManagerName = await _userNameProvider.GetUserNameAsync(entity.TechnicalManagerId);

        return dto;
    }

    public override async Task<PagedResultDto<LaboratoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.GetQueryableAsync();

        var totalCount = query.Count();

        var items = query
            .OrderByThenBy(x => x.SortOrder, x => x.Name)
            .ApplyPaging(input)
            .ToList();

        var dtos = ObjectMapper.Map<List<Laboratory>, List<LaboratoryDto>>(items);
        await EnrichWithTechnicalManagerNamesAsync(dtos);

        return new PagedResultDto<LaboratoryDto>(totalCount, dtos);
    }

    public override async Task<LaboratoryDto> CreateAsync(CreateUpdateLaboratoryDto input)
    {
        await _validationService.ValidateUniqueAsync(
            Repository,
            x => x.Code == input.Code,
            "code",
            $"Mã khoa phòng '{input.Code}' đã tồn tại");

        await _validationService.ValidateUniqueAsync(
            Repository,
            x => x.Name == input.Name,
            "name",
            $"Tên khoa phòng '{input.Name}' đã tồn tại");

        return await base.CreateAsync(input);
    }

    public override async Task<LaboratoryDto> UpdateAsync(Guid id, CreateUpdateLaboratoryDto input)
    {
        await _validationService.ValidateUniqueAsync(
            Repository,
            x => x.Code == input.Code,
            "code",
            $"Mã khoa phòng '{input.Code}' đã tồn tại",
            excludeId: id);

        await _validationService.ValidateUniqueAsync(
            Repository,
            x => x.Name == input.Name,
            "name",
            $"Tên khoa phòng '{input.Name}' đã tồn tại",
            excludeId: id);

        return await base.UpdateAsync(id, input);
    }

    /// <summary>
    /// Enriches DTOs with technical manager names
    /// </summary>
    private async Task EnrichWithTechnicalManagerNamesAsync(List<LaboratoryDto> dtos)
    {
        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => dto.TechnicalManagerId,
            (dto, name) => dto.TechnicalManagerName = name);
    }
}
