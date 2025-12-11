using System;
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
