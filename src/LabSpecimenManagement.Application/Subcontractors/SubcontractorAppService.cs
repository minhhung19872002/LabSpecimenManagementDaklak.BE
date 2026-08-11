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

namespace LabSpecimenManagement.Subcontractors;

[RemoteService(IsEnabled = false)]
public class SubcontractorAppService :
    CrudAppService<
        Subcontractor,
        SubcontractorDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateSubcontractorDto>,
    ISubcontractorAppService
{
    private readonly IRepository<SubcontractorCertification, Guid> _certificationRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public SubcontractorAppService(
        IRepository<Subcontractor, Guid> repository,
        IRepository<SubcontractorCertification, Guid> certificationRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
        : base(repository)
    {
        _certificationRepository = certificationRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
    }

    protected override async Task<Subcontractor> MapToEntityAsync(CreateUpdateSubcontractorDto createInput)
    {
        var code = await GenerateCodeAsync();
        var entity = new Subcontractor(GuidGenerator.Create(), code, createInput.Name);

        entity.Address = createInput.Address;
        entity.Phone = createInput.Phone;
        entity.Email = createInput.Email;
        entity.ContactPerson = createInput.ContactPerson;
        entity.TaxCode = createInput.TaxCode;
        entity.IsActive = createInput.IsActive;
        entity.Capabilities = createInput.Capabilities;
        entity.EvaluationScore = createInput.EvaluationScore;
        entity.ContractStartDate = createInput.ContractStartDate;
        entity.ContractEndDate = createInput.ContractEndDate;
        entity.Notes = createInput.Notes;

        return entity;
    }

    public override async Task<SubcontractorDto> CreateAsync(CreateUpdateSubcontractorDto input)
    {
        var entity = await MapToEntityAsync(input);
        await Repository.InsertAsync(entity);

        // Add certifications
        if (input.Certifications != null)
        {
            foreach (var certDto in input.Certifications)
            {
                var cert = new SubcontractorCertification(
                    GuidGenerator.Create(),
                    entity.Id,
                    certDto.Name,
                    certDto.IssuedBy,
                    certDto.IssuedDate,
                    certDto.ExpiryDate
                );
                cert.CertificateNumber = certDto.CertificateNumber;
                cert.FilePath = certDto.FilePath;

                await _certificationRepository.InsertAsync(cert);
            }
        }

        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<PagedResultDto<SubcontractorDto>> GetFilteredListAsync(GetSubcontractorListInput input)
    {
        var query = await Repository.GetQueryableAsync();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(filter) ||
                x.Name.ToLower().Contains(filter) ||
                (x.ContactPerson != null && x.ContactPerson.ToLower().Contains(filter)));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == input.IsActive.Value);
        }

        // Get total count before paging
        var totalCount = query.Count();

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            query = query.OrderBy(input.Sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreationTime);
        }

        // Apply paging
        var items = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<SubcontractorDto>(
            totalCount,
            ObjectMapper.Map<List<Subcontractor>, List<SubcontractorDto>>(items)
        );
    }

    public async Task<ListResultDto<SubcontractorDto>> GetActiveSubcontractorsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<SubcontractorDto>(
            ObjectMapper.Map<List<Subcontractor>, List<SubcontractorDto>>(items)
        );
    }

    public async Task<ListResultDto<SubcontractorCertificationDto>> GetExpiringCertificationsAsync(int daysAhead = 90)
    {
        var cutoffDate = DateTime.Now.AddDays(daysAhead);
        var query = await _certificationRepository.GetQueryableAsync();
        var items = query
            .Where(x => x.ExpiryDate <= cutoffDate)
            .OrderBy(x => x.ExpiryDate)
            .ToList();

        return new ListResultDto<SubcontractorCertificationDto>(
            ObjectMapper.Map<List<SubcontractorCertification>, List<SubcontractorCertificationDto>>(items)
        );
    }

    private async Task<string> GenerateCodeAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("NTP");
            var count = await Repository.CountAsync(x => x.Code.StartsWith(prefix));
            return _codeGenerator.GenerateCodeWithDate("NTP", count, sequencePadding: 3);
        }
    }
}
