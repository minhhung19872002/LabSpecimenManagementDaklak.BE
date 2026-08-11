using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// Service quản lý mẫu lưu kho
/// </summary>
public class SampleStorageAppService : ApplicationService, ISampleStorageAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;

    // Số ngày sau khi hoàn thành để chuyển sang trạng thái "Cần hủy mẫu"
    private const int DaysAfterCompletionForDisposal = 5;

    public SampleStorageAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _specimenTypeRepository = specimenTypeRepository;
    }

    /// <summary>
    /// Lấy danh sách HĐ có mẫu (status >= SampleSent và SampleStorage = Yes)
    /// </summary>
    public async Task<PagedResultDto<SampleStorageDto>> GetListAsync(GetSampleStorageListDto input)
    {
        var query = await _testRequestRepository.WithDetailsAsync(x => x.Samples);

        // Chỉ lấy TestRequest có SampleStorage = Yes (Có lưu mẫu) và status >= SampleSent
        query = query.Where(x => x.Status >= TestRequestStatus.SampleSent && x.SampleStorage == SampleStorageOption.Yes);

        // Tìm kiếm
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            query = query.Where(x =>
                (x.ContractNumber != null && x.ContractNumber.ToLower().Contains(search)) ||
                x.CustomerName.ToLower().Contains(search));
        }

        // Đếm total
        var totalCount = await AsyncExecuter.CountAsync(query);

        // Lấy data với paging
        var items = await AsyncExecuter.ToListAsync(
            query
                .OrderByDescending(x => x.SampleSentDate ?? x.CreationTime)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
        );

        // Map to DTO và filter theo status nếu cần
        var result = items.Select(tr => MapToDto(tr)).ToList();

        // Filter theo status nếu có
        if (input.Status.HasValue)
        {
            result = result.Where(x => x.Status == input.Status.Value).ToList();
            totalCount = result.Count;
        }

        return new PagedResultDto<SampleStorageDto>(totalCount, result);
    }

    /// <summary>
    /// Lấy chi tiết HĐ + danh sách mẫu
    /// </summary>
    public async Task<SampleStorageDetailDto> GetAsync(Guid id)
    {
        var query = await _testRequestRepository.WithDetailsAsync(x => x.Samples);
        var testRequest = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (testRequest == null)
        {
            throw new EntityNotFoundException("TestRequest", id);
        }

        return await MapToDetailDtoAsync(testRequest);
    }

    /// <summary>
    /// Cập nhật thông tin mẫu
    /// </summary>
    public async Task UpdateSampleAsync(Guid sampleId, UpdateSampleDto input)
    {
        var sample = await _sampleRepository.GetAsync(sampleId);

        if (sample.IsCancelled)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật mẫu đã hủy", "sample");
        }

        if (!string.IsNullOrWhiteSpace(input.SampleName))
        {
            sample.SampleName = input.SampleName;
        }

        if (!string.IsNullOrWhiteSpace(input.SampleQuantity))
        {
            sample.SampleQuantity = input.SampleQuantity;
        }

        if (!string.IsNullOrWhiteSpace(input.StatusText))
        {
            sample.StatusText = input.StatusText;
        }

        // Note: StorageDate is from TestRequest.SampleSentDate, not editable per sample

        await _sampleRepository.UpdateAsync(sample);
    }

    /// <summary>
    /// Hủy 1 mẫu
    /// </summary>
    public async Task CancelSampleAsync(Guid sampleId)
    {
        var sample = await _sampleRepository.GetAsync(sampleId);

        if (sample.IsCancelled)
        {
            throw new Common.Exceptions.InvalidOperationException("Mẫu này đã được hủy", "sample");
        }

        sample.IsCancelled = true;
        sample.CancelledDate = Clock.Now;

        await _sampleRepository.UpdateAsync(sample);
    }

    /// <summary>
    /// Hủy tất cả mẫu trong HĐ
    /// </summary>
    public async Task CancelAllSamplesAsync(Guid testRequestId)
    {
        var query = await _testRequestRepository.WithDetailsAsync(x => x.Samples);
        var testRequest = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == testRequestId));

        if (testRequest == null)
        {
            throw new EntityNotFoundException("TestRequest", testRequestId);
        }

        var now = Clock.Now;
        foreach (var sample in testRequest.Samples.Where(s => !s.IsCancelled))
        {
            sample.IsCancelled = true;
            sample.CancelledDate = now;
        }

        await _testRequestRepository.UpdateAsync(testRequest);
    }

    #region Private Methods

    /// <summary>
    /// Tính trạng thái quản lý mẫu
    /// - Ưu tiên check NeedDisposal flag từ DB (được set bởi cronjob)
    /// - Fallback về logic động nếu chưa được cronjob update
    /// </summary>
    private SampleStorageStatus CalculateStatus(TestRequest testRequest)
    {
        // Nếu tất cả mẫu đã hủy (IsCancelled = true)
        if (testRequest.Samples.All(s => s.IsCancelled))
        {
            return SampleStorageStatus.Disposed;
        }

        // Ưu tiên check NeedDisposal flag từ DB (được set bởi cronjob)
        // Nếu có ít nhất 1 mẫu chưa hủy và cần hủy -> PendingDisposal
        if (testRequest.Samples.Any(s => !s.IsCancelled && s.NeedDisposal))
        {
            return SampleStorageStatus.PendingDisposal;
        }

        // Fallback: Tính động nếu cronjob chưa chạy
        // Nếu HĐ hoàn thành + 5 ngày
        if (testRequest.Status == TestRequestStatus.Completed && testRequest.CompletedDate.HasValue)
        {
            if (testRequest.CompletedDate.Value.AddDays(DaysAfterCompletionForDisposal) <= Clock.Now)
            {
                return SampleStorageStatus.PendingDisposal;
            }
        }

        return SampleStorageStatus.Received;
    }

    private SampleStorageDto MapToDto(TestRequest testRequest)
    {
        return new SampleStorageDto
        {
            Id = testRequest.Id,
            ContractNumber = testRequest.ContractNumber ?? testRequest.RequestNumber,
            CustomerName = testRequest.CustomerName,
            TotalSampleCount = testRequest.Samples.Count,
            StorageDate = testRequest.SampleSentDate,
            Status = CalculateStatus(testRequest),
            CancelledCount = testRequest.Samples.Count(s => s.IsCancelled)
        };
    }

    private async Task<SampleStorageDetailDto> MapToDetailDtoAsync(TestRequest testRequest)
    {
        // Get all specimen type IDs from samples
        var specimenTypeIds = testRequest.Samples
            .Where(s => s.SpecimenTypeId.HasValue)
            .Select(s => s.SpecimenTypeId!.Value)
            .Distinct()
            .ToList();

        // Load all specimen types in one query
        var specimenTypes = specimenTypeIds.Count > 0
            ? await _specimenTypeRepository.GetListAsync(st => specimenTypeIds.Contains(st.Id))
            : new List<SpecimenType>();

        var specimenTypeDict = specimenTypes.ToDictionary(st => st.Id, st => st.Name);

        return new SampleStorageDetailDto
        {
            Id = testRequest.Id,
            ContractNumber = testRequest.ContractNumber ?? testRequest.RequestNumber,
            CustomerName = testRequest.CustomerName,
            TotalSampleCount = testRequest.Samples.Count,
            StorageDate = testRequest.SampleSentDate,
            Status = CalculateStatus(testRequest),
            Samples = testRequest.Samples
                .OrderBy(s => s.SortOrder)
                .Select(s => new SampleStorageItemDto
                {
                    Id = s.Id,
                    SampleCode = s.SampleCode,
                    SampleName = s.SampleName,
                    SpecimenTypeName = s.SpecimenTypeId.HasValue && specimenTypeDict.ContainsKey(s.SpecimenTypeId.Value)
                        ? specimenTypeDict[s.SpecimenTypeId.Value]
                        : null,
                    StorageDate = testRequest.SampleSentDate,
                    SampleQuantity = s.SampleQuantity,
                    StatusText = s.StatusText,
                    IsCancelled = s.IsCancelled,
                    CancelledDate = s.CancelledDate,
                    NeedDisposal = s.NeedDisposal,
                    NeedDisposalDate = s.NeedDisposalDate
                })
                .ToList()
        };
    }

    #endregion
}
