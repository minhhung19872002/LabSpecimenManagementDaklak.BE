using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Application Service cho quản lý phiếu thu
/// </summary>
public class ReceiptAppService : LabSpecimenManagementAppService, IReceiptAppService
{
    private readonly IRepository<Receipt, Guid> _receiptRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly ITestRequestAppService _testRequestAppService;

    public ReceiptAppService(
        IRepository<Receipt, Guid> receiptRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        ITestRequestAppService testRequestAppService)
    {
        _receiptRepository = receiptRepository;
        _testRequestRepository = testRequestRepository;
        _testRequestAppService = testRequestAppService;
    }

    /// <summary>
    /// Lấy danh sách phiếu thu với phân trang và filter
    /// </summary>
    public async Task<PagedResultDto<ReceiptDto>> GetListAsync(GetReceiptListInput input)
    {
        var query = await _receiptRepository.WithDetailsAsync(x => x.TestRequest);

        // Filter by status
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        // Filter by search text (contract number, customer name, receipt number)
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(x =>
                (x.TestRequest.ContractNumber != null && x.TestRequest.ContractNumber.ToLower().Contains(filter)) ||
                (x.TestRequest.CustomerName != null && x.TestRequest.CustomerName.ToLower().Contains(filter)) ||
                (x.ReceiptNumber != null && x.ReceiptNumber.ToLower().Contains(filter)));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        // Sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            query = query.OrderBy(input.Sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreationTime);
        }

        // Paging
        query = query.Skip(input.SkipCount).Take(input.MaxResultCount);

        var items = await AsyncExecuter.ToListAsync(query);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<ReceiptDto>(totalCount, dtos);
    }

    /// <summary>
    /// Lấy chi tiết phiếu thu theo ID
    /// </summary>
    public async Task<ReceiptDto> GetAsync(Guid id)
    {
        var query = await _receiptRepository.WithDetailsAsync(x => x.TestRequest);
        var receipt = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (receipt == null)
        {
            throw new UserFriendlyException("Không tìm thấy phiếu thu");
        }

        return MapToDto(receipt);
    }

    /// <summary>
    /// Lấy chi tiết phiếu thu đầy đủ (bao gồm TestRequest với samples, indicators)
    /// </summary>
    public async Task<ReceiptDetailDto> GetDetailAsync(Guid id)
    {
        var query = await _receiptRepository.WithDetailsAsync(x => x.TestRequest);
        var receipt = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));

        if (receipt == null)
        {
            throw new UserFriendlyException("Không tìm thấy phiếu thu");
        }

        // Get full TestRequest data with samples and indicators
        var testRequestDto = await _testRequestAppService.GetAsync(receipt.TestRequestId);

        return new ReceiptDetailDto
        {
            Id = receipt.Id,
            TestRequestId = receipt.TestRequestId,
            ReceiptNumber = receipt.ReceiptNumber,
            Status = receipt.Status,
            Notes = receipt.Notes,
            PaidDate = receipt.PaidDate,
            CreationTime = receipt.CreationTime,
            LastModificationTime = receipt.LastModificationTime,
            CreatorId = receipt.CreatorId,
            LastModifierId = receipt.LastModifierId,

            // From TestRequest (basic fields)
            ContractNumber = receipt.TestRequest?.ContractNumber,
            RequestDate = receipt.TestRequest?.RequestDate,
            ExpectedCompletionDate = receipt.TestRequest?.ExpectedCompletionDate,
            TotalCost = receipt.TestRequest?.TotalCost ?? 0,
            CustomerName = receipt.TestRequest?.CustomerName,

            // Full TestRequest detail
            TestRequestDetail = testRequestDto
        };
    }

    /// <summary>
    /// Cập nhật thông tin phiếu thu (số phiếu thu)
    /// </summary>
    public async Task UpdateReceiptInfoAsync(Guid id, UpdateReceiptDto input)
    {
        var receipt = await _receiptRepository.GetAsync(id);

        if (receipt == null)
        {
            throw new UserFriendlyException("Không tìm thấy phiếu thu");
        }

        receipt.ReceiptNumber = input.ReceiptNumber;

        await _receiptRepository.UpdateAsync(receipt);
    }

    /// <summary>
    /// Đánh dấu đã thanh toán
    /// </summary>
    public async Task MarkAsPaidAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetAsync(id);

        if (receipt == null)
        {
            throw new UserFriendlyException("Không tìm thấy phiếu thu");
        }

        if (receipt.Status == ReceiptStatus.Paid)
        {
            throw new UserFriendlyException("Phiếu thu đã được thanh toán");
        }

        if (receipt.Status == ReceiptStatus.Cancelled)
        {
            throw new UserFriendlyException("Không thể cập nhật phiếu thu đã hủy");
        }

        receipt.MarkAsPaid();
        await _receiptRepository.UpdateAsync(receipt);

        // Cập nhật TestRequest: PaymentReceived = true và Status = Paid
        var testRequest = await _testRequestRepository.GetAsync(receipt.TestRequestId);
        testRequest.PaymentReceived = true;
        testRequest.Status = TestRequestStatus.Paid;
        await _testRequestRepository.UpdateAsync(testRequest);
    }

    /// <summary>
    /// Đánh dấu chưa thanh toán
    /// </summary>
    public async Task MarkAsUnpaidAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetAsync(id);

        if (receipt == null)
        {
            throw new UserFriendlyException("Không tìm thấy phiếu thu");
        }

        if (receipt.Status == ReceiptStatus.Paid)
        {
            throw new UserFriendlyException("Không thể đánh dấu chưa thanh toán cho phiếu đã thanh toán");
        }

        if (receipt.Status == ReceiptStatus.Cancelled)
        {
            throw new UserFriendlyException("Không thể cập nhật phiếu thu đã hủy");
        }

        receipt.MarkAsUnpaid();
        await _receiptRepository.UpdateAsync(receipt);
    }

    private static ReceiptDto MapToDto(Receipt receipt)
    {
        var testRequest = receipt.TestRequest;

        return new ReceiptDto
        {
            Id = receipt.Id,
            TestRequestId = receipt.TestRequestId,
            ReceiptNumber = receipt.ReceiptNumber,
            Status = receipt.Status,
            Notes = receipt.Notes,
            PaidDate = receipt.PaidDate,
            CreationTime = receipt.CreationTime,
            LastModificationTime = receipt.LastModificationTime,
            CreatorId = receipt.CreatorId,
            LastModifierId = receipt.LastModifierId,

            // From TestRequest
            ContractNumber = testRequest?.ContractNumber,
            RequestDate = testRequest?.RequestDate,
            ExpectedCompletionDate = testRequest?.ExpectedCompletionDate,
            TotalCost = testRequest?.TotalCost ?? 0,
            CustomerName = testRequest?.CustomerName
        };
    }
}
