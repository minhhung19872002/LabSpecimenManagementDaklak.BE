using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using LabSpecimenManagement.Email;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
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
    private readonly IRepository<QuotationSample, Guid> _quotationSampleRepository;
    private readonly IRepository<QuotationSampleIndicator, Guid> _quotationSampleIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IClock _clock;
    private readonly IEmailService _emailService;
    private readonly IQuotationPdfGenerator _pdfGenerator;

    public QuotationAppService(
        IRepository<Quotation, Guid> quotationRepository,
        IRepository<QuotationSample, Guid> quotationSampleRepository,
        IRepository<QuotationSampleIndicator, Guid> quotationSampleIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IIdentityUserRepository userRepository,
        IClock clock,
        IEmailService emailService,
        IQuotationPdfGenerator pdfGenerator)
    {
        _quotationRepository = quotationRepository;
        _quotationSampleRepository = quotationSampleRepository;
        _quotationSampleIndicatorRepository = quotationSampleIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _userRepository = userRepository;
        _clock = clock;
        _emailService = emailService;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<PagedResultDto<QuotationListDto>> GetListAsync(GetQuotationListInput input)
    {
        var query = await _quotationRepository.WithDetailsAsync(x => x.Samples);

        // Filter
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.ToLower();
            query = query.Where(x =>
                x.QuotationNumber.ToLower().Contains(filter) ||
                (x.CustomerName != null && x.CustomerName.ToLower().Contains(filter)));
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

        // Load indicators for each sample to count
        var quotationIds = quotations.Select(q => q.Id).ToList();
        var sampleQuery = await _quotationSampleRepository.WithDetailsAsync(s => s.Indicators);
        var samples = await AsyncExecuter.ToListAsync(
            sampleQuery.Where(s => quotationIds.Contains(s.QuotationId)));
        var samplesByQuotation = samples.GroupBy(s => s.QuotationId)
            .ToDictionary(g => g.Key, g => g.ToList());

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

        var items = quotations.Select(x =>
        {
            var quotationSamples = samplesByQuotation.GetValueOrDefault(x.Id) ?? new List<QuotationSample>();
            return new QuotationListDto
            {
                Id = x.Id,
                QuotationNumber = x.QuotationNumber,
                QuotationDate = x.QuotationDate,
                CustomerName = x.CustomerName,
                Phone = x.Phone,
                TotalCost = x.TotalCost,
                Status = x.Status,
                SampleCount = quotationSamples.Count,
                IndicatorCount = quotationSamples.Sum(s => s.Indicators.Count),
                CreationTime = x.CreationTime,
                CreatorUserName = x.CreatorId.HasValue && creatorDict.ContainsKey(x.CreatorId.Value)
                    ? creatorDict[x.CreatorId.Value]
                    : null
            };
        }).ToList();

        return new PagedResultDto<QuotationListDto>(totalCount, items);
    }

    public async Task<QuotationDto> GetAsync(Guid id)
    {
        var query = await _quotationRepository.WithDetailsAsync(x => x.Samples, x => x.Organization);
        var quotation = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (quotation == null)
        {
            throw new UserFriendlyException("Không tìm thấy báo giá");
        }

        // Load samples with indicators
        var sampleQuery = await _quotationSampleRepository.WithDetailsAsync(s => s.Indicators);
        var samples = await AsyncExecuter.ToListAsync(
            sampleQuery.Where(s => s.QuotationId == id).OrderBy(s => s.SortOrder));

        // Load Indicator and TestMethod for each indicator to get FormattedName
        var indicatorIds = samples.SelectMany(s => s.Indicators.Select(i => i.IndicatorId)).Distinct().ToList();
        var testMethodIds = samples.SelectMany(s => s.Indicators.Select(i => i.TestMethodId)).Distinct().ToList();

        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();

        var indicators = await AsyncExecuter.ToListAsync(
            indicatorQuery.Where(i => indicatorIds.Contains(i.Id)));
        var testMethods = await AsyncExecuter.ToListAsync(
            testMethodQuery.Where(t => testMethodIds.Contains(t.Id)));

        var indicatorDict = indicators.ToDictionary(i => i.Id);
        var testMethodDict = testMethods.ToDictionary(t => t.Id);

        return MapToDto(quotation, samples, indicatorDict, testMethodDict);
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

        // Add samples
        foreach (var sampleDto in input.Samples)
        {
            var sample = new QuotationSample(
                GuidGenerator.Create(),
                quotation.Id,
                sampleDto.SortOrder,
                sampleDto.SampleName);

            // Add indicators to sample
            foreach (var indicatorDto in sampleDto.Indicators)
            {
                var indicator = new QuotationSampleIndicator(
                    GuidGenerator.Create(),
                    sample.Id,
                    indicatorDto.IndicatorId,
                    indicatorDto.TestMethodId,
                    indicatorDto.IndicatorName,
                    indicatorDto.TestMethodName,
                    indicatorDto.UnitPrice,
                    indicatorDto.SortOrder);

                sample.Indicators.Add(indicator);
            }

            sample.RecalculateTotalPrice();
            quotation.Samples.Add(sample);
        }

        // Calculate total cost
        quotation.RecalculateTotalCost();

        await _quotationRepository.InsertAsync(quotation, autoSave: true);

        return MapToDto(quotation);
    }

    [Authorize(LabSpecimenManagementPermissions.Quotations.Edit)]
    public async Task<QuotationDto> UpdateAsync(Guid id, CreateUpdateQuotationDto input)
    {
        var query = await _quotationRepository.WithDetailsAsync(x => x.Samples);
        var quotation = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (quotation == null)
        {
            throw new UserFriendlyException("Không tìm thấy báo giá");
        }

        // Load samples with indicators
        var sampleQuery = await _quotationSampleRepository.WithDetailsAsync(s => s.Indicators);
        var existingSamples = await AsyncExecuter.ToListAsync(
            sampleQuery.Where(s => s.QuotationId == id));

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

        // Get existing sample ids
        var existingSampleIds = existingSamples.Select(x => x.Id).ToList();
        var inputSampleIds = input.Samples.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToList();

        // Remove samples that are not in input
        var samplesToRemove = existingSamples.Where(x => !inputSampleIds.Contains(x.Id)).ToList();
        foreach (var sample in samplesToRemove)
        {
            // Delete all indicators in the sample first
            foreach (var indicator in sample.Indicators.ToList())
            {
                await _quotationSampleIndicatorRepository.DeleteAsync(indicator);
            }
            quotation.Samples.Remove(sample);
            await _quotationSampleRepository.DeleteAsync(sample);
        }

        // Update existing samples and add new samples
        foreach (var sampleDto in input.Samples)
        {
            if (sampleDto.Id.HasValue && existingSampleIds.Contains(sampleDto.Id.Value))
            {
                // Update existing sample
                var existingSample = existingSamples.First(x => x.Id == sampleDto.Id.Value);
                existingSample.SortOrder = sampleDto.SortOrder;
                existingSample.SampleName = sampleDto.SampleName;

                // Handle indicators
                var existingIndicatorIds = existingSample.Indicators.Select(i => i.Id).ToList();
                var inputIndicatorIds = sampleDto.Indicators.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToList();

                // Remove indicators not in input
                var indicatorsToRemove = existingSample.Indicators.Where(i => !inputIndicatorIds.Contains(i.Id)).ToList();
                foreach (var indicator in indicatorsToRemove)
                {
                    existingSample.Indicators.Remove(indicator);
                    await _quotationSampleIndicatorRepository.DeleteAsync(indicator);
                }

                // Update or add indicators
                foreach (var indicatorDto in sampleDto.Indicators)
                {
                    if (indicatorDto.Id.HasValue && existingIndicatorIds.Contains(indicatorDto.Id.Value))
                    {
                        // Update existing indicator
                        var existingIndicator = existingSample.Indicators.First(i => i.Id == indicatorDto.Id.Value);
                        existingIndicator.IndicatorId = indicatorDto.IndicatorId;
                        existingIndicator.TestMethodId = indicatorDto.TestMethodId;
                        existingIndicator.IndicatorName = indicatorDto.IndicatorName;
                        existingIndicator.TestMethodName = indicatorDto.TestMethodName;
                        existingIndicator.UnitPrice = indicatorDto.UnitPrice;
                        existingIndicator.SortOrder = indicatorDto.SortOrder;
                    }
                    else
                    {
                        // Add new indicator
                        var newIndicator = new QuotationSampleIndicator(
                            GuidGenerator.Create(),
                            existingSample.Id,
                            indicatorDto.IndicatorId,
                            indicatorDto.TestMethodId,
                            indicatorDto.IndicatorName,
                            indicatorDto.TestMethodName,
                            indicatorDto.UnitPrice,
                            indicatorDto.SortOrder);

                        existingSample.Indicators.Add(newIndicator);
                    }
                }

                existingSample.RecalculateTotalPrice();
            }
            else
            {
                // Add new sample
                var newSample = new QuotationSample(
                    GuidGenerator.Create(),
                    quotation.Id,
                    sampleDto.SortOrder,
                    sampleDto.SampleName);

                foreach (var indicatorDto in sampleDto.Indicators)
                {
                    var indicator = new QuotationSampleIndicator(
                        GuidGenerator.Create(),
                        newSample.Id,
                        indicatorDto.IndicatorId,
                        indicatorDto.TestMethodId,
                        indicatorDto.IndicatorName,
                        indicatorDto.TestMethodName,
                        indicatorDto.UnitPrice,
                        indicatorDto.SortOrder);

                    newSample.Indicators.Add(indicator);
                }

                newSample.RecalculateTotalPrice();
                quotation.Samples.Add(newSample);
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
            Samples = quotation.Samples.OrderBy(s => s.SortOrder).Select(sample => new QuotationSampleDto
            {
                Id = sample.Id,
                QuotationId = sample.QuotationId,
                SortOrder = sample.SortOrder,
                SampleName = sample.SampleName,
                TotalPrice = sample.TotalPrice,
                Indicators = sample.Indicators.OrderBy(i => i.SortOrder).Select(indicator => new QuotationSampleIndicatorDto
                {
                    Id = indicator.Id,
                    QuotationSampleId = indicator.QuotationSampleId,
                    IndicatorId = indicator.IndicatorId,
                    TestMethodId = indicator.TestMethodId,
                    IndicatorName = indicator.IndicatorName,
                    TestMethodName = indicator.TestMethodName,
                    UnitPrice = indicator.UnitPrice,
                    SortOrder = indicator.SortOrder
                }).ToList()
            }).ToList()
        };
    }

    private QuotationDto MapToDto(
        Quotation quotation,
        List<QuotationSample> samples,
        Dictionary<Guid, Indicator> indicatorDict,
        Dictionary<Guid, TestMethod> testMethodDict)
    {
        // Get organization info if available
        var org = quotation.Organization;

        return new QuotationDto
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            QuotationDate = quotation.QuotationDate,
            OrganizationId = quotation.OrganizationId,
            OrganizationName = org?.Name,
            CustomerName = quotation.CustomerName,
            ProvinceCode = quotation.ProvinceCode ?? org?.ProvinceCode,
            DistrictCode = quotation.DistrictCode,
            WardCode = quotation.WardCode ?? org?.WardCode,
            Address = quotation.Address ?? org?.Address,
            TaxCode = quotation.TaxCode ?? org?.TaxCode,
            ContactPerson = quotation.ContactPerson ?? org?.ContactPerson,
            Phone = quotation.Phone ?? org?.Phone,
            TotalCost = quotation.TotalCost,
            Notes = quotation.Notes,
            Status = quotation.Status,
            CreationTime = quotation.CreationTime,
            CreatorId = quotation.CreatorId,
            LastModificationTime = quotation.LastModificationTime,
            LastModifierId = quotation.LastModifierId,
            Samples = samples.OrderBy(s => s.SortOrder).Select(sample =>
            {
                return new QuotationSampleDto
                {
                    Id = sample.Id,
                    QuotationId = sample.QuotationId,
                    SortOrder = sample.SortOrder,
                    SampleName = sample.SampleName,
                    TotalPrice = sample.TotalPrice,
                    Indicators = sample.Indicators.OrderBy(i => i.SortOrder).Select(indicator =>
                    {
                        indicatorDict.TryGetValue(indicator.IndicatorId, out var ind);
                        testMethodDict.TryGetValue(indicator.TestMethodId, out var testMethod);

                        return new QuotationSampleIndicatorDto
                        {
                            Id = indicator.Id,
                            QuotationSampleId = indicator.QuotationSampleId,
                            IndicatorId = indicator.IndicatorId,
                            TestMethodId = indicator.TestMethodId,
                            IndicatorName = indicator.IndicatorName,
                            IndicatorFormattedName = ind?.FormattedName,
                            TestMethodName = indicator.TestMethodName,
                            TestMethodFormattedName = testMethod?.FormattedName,
                            UnitPrice = indicator.UnitPrice,
                            SortOrder = indicator.SortOrder
                        };
                    }).ToList()
                };
            }).ToList()
        };
    }

    public async Task SendEmailAsync(SendQuotationEmailDto input)
    {
        // Filter empty emails
        var toEmails = input.To?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList();
        var ccEmails = input.Cc?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList();

        if (toEmails == null || toEmails.Count == 0)
        {
            throw new UserFriendlyException("Vui lòng nhập địa chỉ email người nhận");
        }

        if (input.Samples == null || input.Samples.Count == 0)
        {
            throw new UserFriendlyException("Vui lòng thêm ít nhất một mẫu vào báo giá");
        }

        // Get current user name
        var currentUserName = await GetCurrentUserNameAsync();

        // Generate PDF
        var pdfData = await _pdfGenerator.GeneratePdfAsync(
            input.CustomerName ?? "Quý khách hàng",
            currentUserName,
            input.Samples,
            input.TotalPrice);

        var fileName = $"BaoGia_{_clock.Now:yyyyMMdd_HHmmss}.pdf";

        // Send email with attachment
        await _emailService.SendEmailWithAttachmentAsync(
            toEmails,
            ccEmails,
            input.Subject,
            input.Body,
            pdfData,
            fileName);
    }

    public async Task<byte[]> GeneratePdfAsync(GenerateQuotationPdfDto input)
    {
        if (input.Samples == null || input.Samples.Count == 0)
        {
            throw new UserFriendlyException("Vui lòng thêm ít nhất một mẫu vào báo giá");
        }

        // Get current user name
        var currentUserName = await GetCurrentUserNameAsync();

        var pdfData = await _pdfGenerator.GeneratePdfAsync(
            input.CustomerName ?? "Quý khách hàng",
            currentUserName,
            input.Samples,
            input.TotalPrice);

        return pdfData;
    }

    private async Task<string> GetCurrentUserNameAsync()
    {
        if (CurrentUser.Id.HasValue)
        {
            var user = await _userRepository.FindAsync(CurrentUser.Id.Value);
            if (user != null)
            {
                return user.Name ?? user.UserName ?? "Người dùng";
            }
        }
        return "Người dùng";
    }
}
