using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.TestRequests;

[RemoteService(IsEnabled = false)]
public class TestRequestAppService :
    CrudAppService<
        TestRequest,
        TestRequestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestRequestDto,
        UpdateTestRequestDto>,
    ITestRequestAppService
{
    public TestRequestAppService(IRepository<TestRequest, Guid> repository)
        : base(repository)
    {
    }

    public async Task<PagedResultDto<TestRequestDto>> GetFilteredListAsync(GetTestRequestListDto input)
    {
        var queryable = await Repository.GetQueryableAsync();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            queryable = queryable.Where(x =>
                x.RequestNumber.ToLower().Contains(search) ||
                x.CustomerName.ToLower().Contains(search) ||
                (x.CustomerEmail != null && x.CustomerEmail.ToLower().Contains(search)) ||
                (x.CustomerPhone != null && x.CustomerPhone.ToLower().Contains(search)));
        }

        // Apply status filter
        if (input.Status.HasValue)
        {
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        }

        // Apply date range filter
        if (input.FromDate.HasValue)
        {
            queryable = queryable.Where(x => x.RequestDate >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            queryable = queryable.Where(x => x.RequestDate <= input.ToDate.Value);
        }

        // Apply customer name filter
        if (!string.IsNullOrWhiteSpace(input.CustomerName))
        {
            var customerName = input.CustomerName.ToLower();
            queryable = queryable.Where(x => x.CustomerName.ToLower().Contains(customerName));
        }

        // Get total count
        var totalCount = await AsyncExecuter.CountAsync(queryable);

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            queryable = queryable.OrderBy(input.Sorting);
        }
        else
        {
            queryable = queryable.OrderByDescending(x => x.RequestDate);
        }

        // Apply paging
        queryable = queryable
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(queryable);
        var dtos = ObjectMapper.Map<System.Collections.Generic.List<TestRequest>, System.Collections.Generic.List<TestRequestDto>>(entities);

        return new PagedResultDto<TestRequestDto>(totalCount, dtos);
    }

    protected override async Task<TestRequest> MapToEntityAsync(CreateTestRequestDto createInput)
    {
        // Generate request number automatically
        var requestNumber = await GenerateRequestNumberAsync();

        var entity = new TestRequest(
            GuidGenerator.Create(),
            requestNumber,
            createInput.RequestDate,
            createInput.CustomerName,
            createInput.CustomerEmail,
            createInput.CustomerPhone
        );

        entity.CustomerInfo = createInput.CustomerInfo;
        entity.Notes = createInput.Notes;
        entity.ExpectedCompletionDate = createInput.ExpectedCompletionDate;
        entity.Subcontractor = createInput.Subcontractor;

        return entity;
    }

    private async Task<string> GenerateRequestNumberAsync()
    {
        // Generate request number format: REQ-YYYYMMDD-XXXX
        var today = DateTime.Now;
        var prefix = $"REQ-{today:yyyyMMdd}";

        var count = await Repository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
        var sequence = (count + 1).ToString("D4");

        return $"{prefix}-{sequence}";
    }
}
