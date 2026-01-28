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
    private readonly IRepository<Department, Guid> _departmentRepository;

    public LaboratoryAppService(
        IRepository<Laboratory, Guid> repository,
        IRepository<Department, Guid> departmentRepository,
        IUserNameProvider userNameProvider,
        IEntityValidationService validationService)
        : base(repository)
    {
        _departmentRepository = departmentRepository;
        _userNameProvider = userNameProvider;
        _validationService = validationService;
    }

    public async Task<ListResultDto<LaboratoryDto>> GetActiveLaboratoriesAsync()
    {
        var labQuery = await Repository.GetQueryableAsync();
        var deptQuery = await _departmentRepository.GetQueryableAsync();

        var query = from lab in labQuery.WhereActive()
                    join dept in deptQuery on lab.DepartmentId equals dept.Id
                    orderby lab.SortOrder, dept.Name
                    select new LaboratoryDto
                    {
                        Id = lab.Id,
                        Code = lab.Code,
                        Name = dept.Name,
                        DepartmentId = lab.DepartmentId,
                        TechnicalManagerId = lab.TechnicalManagerId,
                        IsActive = lab.IsActive,
                        SortOrder = lab.SortOrder,
                        CreationTime = lab.CreationTime,
                        CreatorId = lab.CreatorId,
                        LastModificationTime = lab.LastModificationTime,
                        LastModifierId = lab.LastModifierId,
                    };

        var items = query.ToList();
        await EnrichWithTechnicalManagerNamesAsync(items);

        return new ListResultDto<LaboratoryDto>(items);
    }

    public async Task<ListResultDto<LaboratoryDto>> GetAllAsync()
    {
        var labQuery = await Repository.GetQueryableAsync();
        var deptQuery = await _departmentRepository.GetQueryableAsync();

        var query = from lab in labQuery
                    join dept in deptQuery on lab.DepartmentId equals dept.Id
                    orderby lab.SortOrder, dept.Name
                    select new LaboratoryDto
                    {
                        Id = lab.Id,
                        Code = lab.Code,
                        Name = dept.Name,
                        DepartmentId = lab.DepartmentId,
                        TechnicalManagerId = lab.TechnicalManagerId,
                        IsActive = lab.IsActive,
                        SortOrder = lab.SortOrder,
                        CreationTime = lab.CreationTime,
                        CreatorId = lab.CreatorId,
                        LastModificationTime = lab.LastModificationTime,
                        LastModifierId = lab.LastModifierId,
                    };

        var items = query.ToList();
        await EnrichWithTechnicalManagerNamesAsync(items);

        return new ListResultDto<LaboratoryDto>(items);
    }

    public override async Task<LaboratoryDto> GetAsync(Guid id)
    {
        var lab = await Repository.GetAsync(id);
        var dept = await _departmentRepository.GetAsync(lab.DepartmentId);

        var dto = new LaboratoryDto
        {
            Id = lab.Id,
            Code = lab.Code,
            Name = dept.Name,
            DepartmentId = lab.DepartmentId,
            TechnicalManagerId = lab.TechnicalManagerId,
            IsActive = lab.IsActive,
            SortOrder = lab.SortOrder,
            CreationTime = lab.CreationTime,
            CreatorId = lab.CreatorId,
            LastModificationTime = lab.LastModificationTime,
            LastModifierId = lab.LastModifierId,
        };

        dto.TechnicalManagerName = await _userNameProvider.GetUserNameAsync(lab.TechnicalManagerId);

        return dto;
    }

    public override async Task<PagedResultDto<LaboratoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var labQuery = await Repository.GetQueryableAsync();
        var deptQuery = await _departmentRepository.GetQueryableAsync();

        var totalCount = labQuery.Count();

        var query = from lab in labQuery
                    join dept in deptQuery on lab.DepartmentId equals dept.Id
                    orderby lab.SortOrder, dept.Name
                    select new LaboratoryDto
                    {
                        Id = lab.Id,
                        Code = lab.Code,
                        Name = dept.Name,
                        DepartmentId = lab.DepartmentId,
                        TechnicalManagerId = lab.TechnicalManagerId,
                        IsActive = lab.IsActive,
                        SortOrder = lab.SortOrder,
                        CreationTime = lab.CreationTime,
                        CreatorId = lab.CreatorId,
                        LastModificationTime = lab.LastModificationTime,
                        LastModifierId = lab.LastModifierId,
                    };

        var items = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        await EnrichWithTechnicalManagerNamesAsync(items);

        return new PagedResultDto<LaboratoryDto>(totalCount, items);
    }

    public override async Task<LaboratoryDto> CreateAsync(CreateUpdateLaboratoryDto input)
    {
        // Validate Code unique
        await _validationService.ValidateUniqueAsync(
            Repository,
            x => x.Code == input.Code,
            "code",
            $"Mã khoa phòng '{input.Code}' đã tồn tại");

        // Validate Department exists
        var department = await _departmentRepository.FindAsync(input.DepartmentId);
        if (department == null)
        {
            throw new UserFriendlyException("Phòng ban không tồn tại");
        }

        var entity = await Repository.InsertAsync(
            new Laboratory(GuidGenerator.Create(), input.Code, input.DepartmentId)
            {
                TechnicalManagerId = input.TechnicalManagerId,
                IsActive = input.IsActive,
            },
            autoSave: true);

        return await GetAsync(entity.Id);
    }

    public override async Task<LaboratoryDto> UpdateAsync(Guid id, CreateUpdateLaboratoryDto input)
    {
        // Validate Code unique (exclude current)
        await _validationService.ValidateUniqueAsync(
            Repository,
            x => x.Code == input.Code,
            "code",
            $"Mã khoa phòng '{input.Code}' đã tồn tại",
            excludeId: id);

        // Validate Department exists
        var department = await _departmentRepository.FindAsync(input.DepartmentId);
        if (department == null)
        {
            throw new UserFriendlyException("Phòng ban không tồn tại");
        }

        var entity = await Repository.GetAsync(id);
        entity.Code = input.Code;
        entity.DepartmentId = input.DepartmentId;
        entity.TechnicalManagerId = input.TechnicalManagerId;
        entity.IsActive = input.IsActive;

        await Repository.UpdateAsync(entity);
        return await GetAsync(id);
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
