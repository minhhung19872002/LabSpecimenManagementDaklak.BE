using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Entities;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Specimens;

[RemoteService(IsEnabled = false)]
public class SpecimenAppService :
    CrudAppService<
        Specimen,
        SpecimenDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateSpecimenDto,
        UpdateSpecimenDto>,
    ISpecimenAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public SpecimenAppService(
        IRepository<Specimen, Guid> repository,
        IRepository<TestRequest, Guid> testRequestRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
        : base(repository)
    {
        _testRequestRepository = testRequestRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
    }

    protected override async Task<Specimen> MapToEntityAsync(CreateSpecimenDto createInput)
    {
        // Generate specimen code automatically
        var specimenCode = await GenerateSpecimenCodeAsync();

        var entity = new Specimen(
            GuidGenerator.Create(),
            createInput.TestRequestId,
            specimenCode,
            createInput.Name,
            createInput.Type,
            createInput.ReceivedDate
        );

        entity.StorageLocation = createInput.StorageLocation;
        entity.Notes = createInput.Notes;
        entity.Images = createInput.Images;
        entity.Deviation = createInput.Deviation;
        entity.Quantity = createInput.Quantity;
        entity.Unit = createInput.Unit;

        return entity;
    }

    public async Task<PagedResultDto<SpecimenDto>> GetFilteredListAsync(GetSpecimenListDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.TestRequest);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                x.Type.ToLower().Contains(search) ||
                (x.TestRequest != null && x.TestRequest.RequestNumber.ToLower().Contains(search)));
        }

        // Apply status filter
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        // Apply type filter
        if (!string.IsNullOrWhiteSpace(input.Type))
        {
            query = query.Where(x => x.Type == input.Type);
        }

        // Apply test request filter
        if (input.TestRequestId.HasValue)
        {
            query = query.Where(x => x.TestRequestId == input.TestRequestId.Value);
        }

        // Apply date range filter
        if (input.FromDate.HasValue)
        {
            query = query.Where(x => x.ReceivedDate >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(x => x.ReceivedDate <= input.ToDate.Value);
        }

        // Get total count
        var totalCount = await AsyncExecuter.CountAsync(query);

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            query = query.OrderBy(input.Sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.ReceivedDate);
        }

        // Apply paging
        query = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(query);

        var dtos = ObjectMapper.Map<List<Specimen>, List<SpecimenDto>>(entities);

        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(x => x.Id == dto.Id);
            if (entity?.TestRequest != null)
            {
                dto.TestRequestNumber = entity.TestRequest.RequestNumber;
            }
        }

        return new PagedResultDto<SpecimenDto>(totalCount, dtos);
    }

    public override async Task<PagedResultDto<SpecimenDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await Repository.WithDetailsAsync(x => x.TestRequest);

        query = ApplyDefaultSorting(query);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = ApplySorting(query, input);
        query = ApplyPaging(query, input);

        var entities = await AsyncExecuter.ToListAsync(query);

        var dtos = ObjectMapper.Map<System.Collections.Generic.List<Specimen>, System.Collections.Generic.List<SpecimenDto>>(entities);

        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(x => x.Id == dto.Id);
            if (entity?.TestRequest != null)
            {
                dto.TestRequestNumber = entity.TestRequest.RequestNumber;
            }
        }

        return new PagedResultDto<SpecimenDto>(
            totalCount,
            dtos
        );
    }

    public override async Task<SpecimenDto> GetAsync(Guid id)
    {
        var query = await Repository.WithDetailsAsync(x => x.TestRequest);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (entity == null)
        {
            throw new EntityNotFoundException(typeof(Specimen), id);
        }

        var dto = ObjectMapper.Map<Specimen, SpecimenDto>(entity);

        if (entity.TestRequest != null)
        {
            dto.TestRequestNumber = entity.TestRequest.RequestNumber;
        }

        return dto;
    }

    private async Task<string> GenerateSpecimenCodeAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("SPE");
            var count = await Repository.CountAsync(x => x.Code.StartsWith(prefix));
            return _codeGenerator.GenerateCodeWithDate("SPE", count);
        }
    }

    public async Task<ListResultDto<SpecimenDto>> GetByTestRequestAsync(Guid testRequestId)
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.TestRequestId == testRequestId)
            .OrderBy(x => x.Code)
            .ToList();

        return new ListResultDto<SpecimenDto>(
            ObjectMapper.Map<System.Collections.Generic.List<Specimen>, System.Collections.Generic.List<SpecimenDto>>(items)
        );
    }

    public async Task<CreateManySpecimensResultDto> CreateManyAsync(CreateManySpecimensDto input)
    {
        var result = new CreateManySpecimensResultDto
        {
            TotalRequested = input.Specimens.Count
        };

        // Validate test request exists
        var testRequest = await _testRequestRepository.FindAsync(input.TestRequestId);
        if (testRequest == null)
        {
            result.Errors.Add($"Không tìm thấy phiếu yêu cầu với ID: {input.TestRequestId}");
            result.FailedCount = input.Specimens.Count;
            return result;
        }

        var createdSpecimens = new List<Specimen>();

        foreach (var specimenItem in input.Specimens)
        {
            try
            {
                // Generate code if not provided
                var code = string.IsNullOrWhiteSpace(specimenItem.Code)
                    ? await GenerateSpecimenCodeAsync()
                    : specimenItem.Code;

                var specimen = new Specimen(
                    GuidGenerator.Create(),
                    input.TestRequestId,
                    code,
                    specimenItem.Name,
                    specimenItem.Type,
                    specimenItem.ReceivedDate
                );

                specimen.Quantity = specimenItem.Quantity;
                specimen.Unit = specimenItem.Unit;
                specimen.Status = specimenItem.Status;
                specimen.StorageLocation = input.StorageLocation;
                specimen.Notes = input.Description;

                // Add storage condition to notes if provided
                if (!string.IsNullOrWhiteSpace(input.StorageCondition))
                {
                    specimen.Notes = string.IsNullOrWhiteSpace(specimen.Notes)
                        ? $"Điều kiện bảo quản: {input.StorageCondition}"
                        : $"{specimen.Notes}\nĐiều kiện bảo quản: {input.StorageCondition}";
                }

                createdSpecimens.Add(specimen);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Lỗi tạo mẫu '{specimenItem.Name}': {ex.Message}");
                result.FailedCount++;
            }
        }

        // Batch insert all specimens
        if (createdSpecimens.Any())
        {
            await Repository.InsertManyAsync(createdSpecimens, autoSave: true);

            // Map to DTOs
            foreach (var specimen in createdSpecimens)
            {
                var dto = ObjectMapper.Map<Specimen, SpecimenDto>(specimen);
                dto.TestRequestNumber = testRequest.RequestNumber;
                result.CreatedSpecimens.Add(dto);
                result.SuccessCount++;
            }
        }

        return result;
    }
}
