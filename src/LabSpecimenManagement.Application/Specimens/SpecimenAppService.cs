using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
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

    public SpecimenAppService(
        IRepository<Specimen, Guid> repository,
        IRepository<TestRequest, Guid> testRequestRepository)
        : base(repository)
    {
        _testRequestRepository = testRequestRepository;
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
        // Generate specimen code format: SPE-YYYYMMDD-XXXX
        var today = DateTime.Now;
        var prefix = $"SPE-{today:yyyyMMdd}";

        var count = await Repository.CountAsync(x => x.Code.StartsWith(prefix));
        var sequence = (count + 1).ToString("D4");

        return $"{prefix}-{sequence}";
    }
}
