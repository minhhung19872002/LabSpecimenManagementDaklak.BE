using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.TestResults;

[RemoteService(IsEnabled = false)]
public class TestResultAppService : CrudAppService<
    TestResult,
    TestResultDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateTestResultDto,
    UpdateTestResultDto>, ITestResultAppService
{
    public TestResultAppService(IRepository<TestResult, Guid> repository)
        : base(repository)
    {
    }

    public override async Task<PagedResultDto<TestResultDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Test);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = ApplyPaging(queryable, input);
        queryable = ApplySorting(queryable, input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = entities.Select(entity =>
        {
            var dto = ObjectMapper.Map<TestResult, TestResultDto>(entity);
            if (entity.Test != null)
            {
                dto.TestCode = entity.Test.Code;
                dto.TestName = entity.Test.Name;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<TestResultDto>(totalCount, dtos);
    }

    public override async Task<TestResultDto> GetAsync(Guid id)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Test);
        var entity = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(x => x.Id == id)
        );

        if (entity == null)
        {
            throw new Volo.Abp.Domain.Entities.EntityNotFoundException(typeof(TestResult), id);
        }

        var dto = ObjectMapper.Map<TestResult, TestResultDto>(entity);
        if (entity.Test != null)
        {
            dto.TestCode = entity.Test.Code;
            dto.TestName = entity.Test.Name;
        }

        return dto;
    }

    public async Task<TestResultDto> SubmitForReviewAsync(Guid id, SubmitForReviewDto input)
    {
        var testResult = await Repository.GetAsync(id);

        if (testResult.ApprovalStatus != ResultApprovalStatus.Draft)
        {
            throw new BusinessException("Only draft test results can be submitted for review");
        }

        testResult.ApprovalStatus = ResultApprovalStatus.Submitted;
        testResult.Notes = input.Notes;
        testResult.EnteredById = CurrentUser.Id;
        testResult.EntryDate = DateTime.UtcNow;

        await Repository.UpdateAsync(testResult);

        return await GetAsync(id);
    }

    public async Task<TestResultDto> ReviewAsync(Guid id, ReviewResultDto input)
    {
        var testResult = await Repository.GetAsync(id);

        if (testResult.ApprovalStatus != ResultApprovalStatus.Submitted)
        {
            throw new BusinessException("Only submitted test results can be reviewed");
        }

        testResult.ReviewerId = CurrentUser.Id;
        testResult.ReviewDate = DateTime.UtcNow;
        testResult.ReviewComment = input.ReviewComments;

        if (input.IsApproved)
        {
            testResult.ApprovalStatus = ResultApprovalStatus.Step1Reviewed;
        }
        else
        {
            testResult.ApprovalStatus = ResultApprovalStatus.Rejected;
        }

        await Repository.UpdateAsync(testResult);

        return await GetAsync(id);
    }

    public async Task<TestResultDto> ApproveAsync(Guid id, ApproveResultDto input)
    {
        var testResult = await Repository.GetAsync(id);

        if (testResult.ApprovalStatus != ResultApprovalStatus.Step1Reviewed)
        {
            throw new BusinessException("Only reviewed test results can be approved");
        }

        testResult.ApproverId = CurrentUser.Id;
        testResult.ApprovalDate = DateTime.UtcNow;
        testResult.ApprovalComment = input.ApprovalComments;

        if (input.IsApproved)
        {
            testResult.ApprovalStatus = ResultApprovalStatus.Step2Approved;
        }
        else
        {
            testResult.ApprovalStatus = ResultApprovalStatus.Rejected;
        }

        await Repository.UpdateAsync(testResult);

        return await GetAsync(id);
    }

    public async Task<PagedResultDto<TestResultDto>> GetPendingReviewListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Test);

        queryable = queryable.Where(x => x.ApprovalStatus == ResultApprovalStatus.Submitted);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = ApplyPaging(queryable, input);
        queryable = ApplySorting(queryable, input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = entities.Select(entity =>
        {
            var dto = ObjectMapper.Map<TestResult, TestResultDto>(entity);
            if (entity.Test != null)
            {
                dto.TestCode = entity.Test.Code;
                dto.TestName = entity.Test.Name;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<TestResultDto>(totalCount, dtos);
    }

    public async Task<PagedResultDto<TestResultDto>> GetPendingApprovalListAsync(PagedAndSortedResultRequestDto input)
    {
        var queryable = await Repository.WithDetailsAsync(x => x.Test);

        queryable = queryable.Where(x => x.ApprovalStatus == ResultApprovalStatus.Step1Reviewed);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        queryable = ApplyPaging(queryable, input);
        queryable = ApplySorting(queryable, input);

        var entities = await AsyncExecuter.ToListAsync(queryable);

        var dtos = entities.Select(entity =>
        {
            var dto = ObjectMapper.Map<TestResult, TestResultDto>(entity);
            if (entity.Test != null)
            {
                dto.TestCode = entity.Test.Code;
                dto.TestName = entity.Test.Name;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<TestResultDto>(totalCount, dtos);
    }
}
