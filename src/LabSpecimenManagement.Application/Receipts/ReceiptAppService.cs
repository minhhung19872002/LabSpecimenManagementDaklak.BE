using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Application Service cho quản lý phiếu thu
/// </summary>
public class ReceiptAppService : LabSpecimenManagementAppService, IReceiptAppService
{
    private readonly IRepository<Receipt, Guid> _receiptRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<PaymentHistory, Guid> _paymentHistoryRepository;
    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IIdentityUserRepository _userRepository;

    // Các status không được cập nhật lại thành Chờ thanh toán/Đã thanh toán
    private static readonly TestRequestStatus[] StatusesBeyondPayment = new[]
    {
        TestRequestStatus.UnderReview,
        TestRequestStatus.WaitingCustomerConfirm,
        TestRequestStatus.RequestApproved,
        TestRequestStatus.SampleSent,
        TestRequestStatus.InProgress,
        TestRequestStatus.PendingChangeApproval,
        TestRequestStatus.PendingApproval,
        TestRequestStatus.Approved,
        TestRequestStatus.ResultSent,
        TestRequestStatus.PartialResultSent,
        TestRequestStatus.ReportPublished,
        TestRequestStatus.Completed,
        TestRequestStatus.Cancelled
    };

    public ReceiptAppService(
        IRepository<Receipt, Guid> receiptRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<PaymentHistory, Guid> paymentHistoryRepository,
        ITestRequestAppService testRequestAppService,
        IIdentityUserRepository userRepository)
    {
        _receiptRepository = receiptRepository;
        _testRequestRepository = testRequestRepository;
        _paymentHistoryRepository = paymentHistoryRepository;
        _testRequestAppService = testRequestAppService;
        _userRepository = userRepository;
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

        // Check trùng số phiếu thu (nếu có nhập số phiếu thu)
        if (!string.IsNullOrWhiteSpace(input.ReceiptNumber))
        {
            var existingReceipt = await _receiptRepository.FirstOrDefaultAsync(r =>
                r.ReceiptNumber == input.ReceiptNumber && r.Id != id);

            if (existingReceipt != null)
            {
                throw new UserFriendlyException($"Số phiếu thu '{input.ReceiptNumber}' đã tồn tại");
            }
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

        // Check trùng số phiếu thu trước khi đánh dấu thanh toán
        if (!string.IsNullOrWhiteSpace(receipt.ReceiptNumber))
        {
            var existingReceipt = await _receiptRepository.FirstOrDefaultAsync(r =>
                r.ReceiptNumber == receipt.ReceiptNumber && r.Id != id);

            if (existingReceipt != null)
            {
                throw new UserFriendlyException($"Số phiếu thu '{receipt.ReceiptNumber}' đã tồn tại");
            }
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

    /// <summary>
    /// Lấy thông tin thanh toán cho modal - tự động tạo Receipt nếu chưa có
    /// </summary>
    public async Task<PaymentInfoDto> GetPaymentInfoAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.GetAsync(testRequestId);
        if (testRequest == null)
        {
            throw new UserFriendlyException("Không tìm thấy phiếu yêu cầu");
        }

        // Tìm Receipt theo TestRequestId
        var receipt = await _receiptRepository.FirstOrDefaultAsync(r => r.TestRequestId == testRequestId);

        // Nếu chưa có Receipt, tạo mới
        if (receipt == null)
        {
            receipt = new Receipt(GuidGenerator.Create(), testRequestId, ReceiptStatus.PendingPayment);
            await _receiptRepository.InsertAsync(receipt);
        }

        // Lấy danh sách PaymentHistory
        var paymentHistories = await _paymentHistoryRepository.GetListAsync(ph => ph.ReceiptId == receipt.Id);
        paymentHistories = paymentHistories.OrderBy(ph => ph.PaymentNumber).ToList();

        // Tính tổng đã thanh toán
        var paidAmount = paymentHistories.Sum(ph => ph.Amount);
        var totalCost = testRequest.TotalCost;
        var remainingAmount = totalCost - paidAmount;

        // Lấy thông tin người tạo cho từng payment history
        var paymentHistoryDtos = new List<PaymentHistoryDto>();
        foreach (var ph in paymentHistories)
        {
            string? creatorName = null;
            if (ph.CreatorId.HasValue)
            {
                var user = await _userRepository.FindAsync(ph.CreatorId.Value);
                creatorName = user?.Name ?? user?.UserName;
            }

            paymentHistoryDtos.Add(new PaymentHistoryDto
            {
                Id = ph.Id,
                ReceiptId = ph.ReceiptId,
                PaymentNumber = ph.PaymentNumber,
                Amount = ph.Amount,
                RemainingAmountBeforePayment = ph.RemainingAmountBeforePayment,
                PaymentMethod = ph.PaymentMethod,
                PaymentDate = ph.PaymentDate,
                Notes = ph.Notes,
                CreationTime = ph.CreationTime,
                CreatorName = creatorName
            });
        }

        return new PaymentInfoDto
        {
            ReceiptId = receipt.Id,
            TestRequestId = testRequestId,
            ContractNumber = testRequest.ContractNumber,
            TotalCost = totalCost,
            PaidAmount = paidAmount,
            RemainingAmount = remainingAmount,
            Status = receipt.Status,
            PaymentHistories = paymentHistoryDtos
        };
    }

    /// <summary>
    /// Xử lý thanh toán mới
    /// </summary>
    public async Task<PaymentResultDto> ProcessPaymentAsync(Guid receiptId, CreatePaymentInput input)
    {
        var query = await _receiptRepository.WithDetailsAsync(r => r.TestRequest);
        var receipt = await AsyncExecuter.FirstOrDefaultAsync(query.Where(r => r.Id == receiptId));

        if (receipt == null)
        {
            throw new UserFriendlyException("Không tìm thấy phiếu thu");
        }

        if (receipt.Status == ReceiptStatus.Paid)
        {
            throw new UserFriendlyException("Phiếu thu đã được thanh toán đầy đủ");
        }

        if (receipt.Status == ReceiptStatus.Cancelled)
        {
            throw new UserFriendlyException("Không thể thanh toán cho phiếu thu đã hủy");
        }

        var testRequest = receipt.TestRequest;
        var totalCost = testRequest.TotalCost;

        // Lấy danh sách PaymentHistory hiện tại
        var existingPayments = await _paymentHistoryRepository.GetListAsync(ph => ph.ReceiptId == receiptId);
        var paidAmount = existingPayments.Sum(ph => ph.Amount);
        var remainingAmount = totalCost - paidAmount;

        // Validate số tiền thanh toán
        if (input.Amount <= 0)
        {
            throw new UserFriendlyException("Số tiền thanh toán phải lớn hơn 0");
        }

        if (input.Amount > remainingAmount)
        {
            throw new UserFriendlyException($"Số tiền thanh toán không được vượt quá số tiền còn lại ({remainingAmount:N0} đ)");
        }

        // Xác định số thứ tự lần thanh toán
        var paymentNumber = existingPayments.Count + 1;

        // Tạo PaymentHistory mới
        var paymentHistory = new PaymentHistory(
            GuidGenerator.Create(),
            receiptId,
            paymentNumber,
            input.Amount,
            remainingAmount,
            input.PaymentMethod,
            DateTime.Now,
            input.Notes
        );

        await _paymentHistoryRepository.InsertAsync(paymentHistory);

        // Tính toán số tiền còn lại sau khi thanh toán
        var newPaidAmount = paidAmount + input.Amount;
        var newRemainingAmount = totalCost - newPaidAmount;

        // Cập nhật status cho Receipt
        string message;
        if (newRemainingAmount <= 0)
        {
            receipt.MarkAsPaid();
            message = "Đã thanh toán đầy đủ";
        }
        else
        {
            receipt.MarkAsPartiallyPaid();
            message = $"Đã thanh toán một phần. Còn lại: {newRemainingAmount:N0} đ";
        }

        await _receiptRepository.UpdateAsync(receipt);

        // Luôn cập nhật PaymentReceived, chỉ cập nhật Status nếu chưa qua các status payment-related
        var currentTestRequestStatus = testRequest.Status;
        var shouldUpdateStatus = !StatusesBeyondPayment.Contains(currentTestRequestStatus);

        if (newRemainingAmount <= 0)
        {
            // Luôn cập nhật PaymentReceived = true khi thanh toán đủ
            testRequest.PaymentReceived = true;
            // Chỉ cập nhật Status nếu chưa qua các status payment-related
            if (shouldUpdateStatus)
            {
                testRequest.Status = TestRequestStatus.Paid;
            }
        }
        else
        {
            testRequest.PaymentReceived = false;
            if (shouldUpdateStatus)
            {
                testRequest.Status = TestRequestStatus.PendingPayment;
            }
        }

        await _testRequestRepository.UpdateAsync(testRequest);

        return new PaymentResultDto
        {
            PaymentHistoryId = paymentHistory.Id,
            PaidAmount = newPaidAmount,
            RemainingAmount = newRemainingAmount,
            ReceiptStatus = receipt.Status,
            Message = message
        };
    }

    /// <summary>
    /// Xuất danh sách thanh toán ra Excel
    /// </summary>
    public async Task<byte[]> ExportPaymentHistoryAsync(ExportPaymentHistoryInput input)
    {
        // Query PaymentHistory với Receipt và TestRequest
        var paymentQuery = await _paymentHistoryRepository.GetQueryableAsync();

        // Join với Receipt để lấy thông tin
        var receiptQuery = await _receiptRepository.WithDetailsAsync(r => r.TestRequest);

        var query = from ph in paymentQuery
                    join r in receiptQuery on ph.ReceiptId equals r.Id
                    select new { PaymentHistory = ph, Receipt = r };

        // Filter theo ngày thanh toán
        if (input.FromDate.HasValue)
        {
            var fromDate = input.FromDate.Value.Date;
            query = query.Where(x => x.PaymentHistory.PaymentDate >= fromDate);
        }

        if (input.ToDate.HasValue)
        {
            var toDate = input.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.PaymentHistory.PaymentDate < toDate);
        }

        // Order by payment date
        query = query.OrderBy(x => x.PaymentHistory.PaymentDate);

        var data = await AsyncExecuter.ToListAsync(query);

        // Map to export data
        var exportData = data.Select(x =>
        {
            var ph = x.PaymentHistory;
            var receipt = x.Receipt;
            var testRequest = receipt.TestRequest;

            // Tạo mã phiếu thu: {receiptId_8ký_đầu}-YYMMDDxx
            var shortReceiptId = receipt.Id.ToString("N").Substring(0, 8).ToUpper();
            var paymentDate = ph.PaymentDate;
            var yy = paymentDate.ToString("yy");
            var mm = paymentDate.ToString("MM");
            var dd = paymentDate.ToString("dd");
            var xx = ph.PaymentNumber.ToString("D2");
            var receiptCode = $"{shortReceiptId}-{yy}{mm}{dd}{xx}";

            // Hình thức thanh toán
            var paymentMethodLabel = ph.PaymentMethod == PaymentMethod.Cash ? "Tiền mặt" : "Chuyển khoản";

            return new
            {
                SoHopDong = testRequest?.ContractNumber ?? "",
                MaPhieuThu = receiptCode,
                TongChiPhi = testRequest?.TotalCost ?? 0,
                SoTienThanhToan = ph.Amount,
                HinhThucThanhToan = paymentMethodLabel,
                GhiChu = ph.Notes ?? ""
            };
        }).ToList();

        // Group by SoHopDong để merge cells
        var groupedData = exportData
            .GroupBy(x => x.SoHopDong)
            .OrderBy(g => g.Key)
            .ToList();

        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            // Add stylesheet for formatting
            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = CreateStylesheet();
            stylesPart.Stylesheet.Save();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();

            // Create columns with custom widths
            var columns = new Columns(
                new Column { Min = 1, Max = 1, Width = 20, CustomWidth = true },  // Số hợp đồng
                new Column { Min = 2, Max = 2, Width = 28, CustomWidth = true },  // Mã phiếu thu
                new Column { Min = 3, Max = 3, Width = 18, CustomWidth = true },  // Tổng chi phí
                new Column { Min = 4, Max = 4, Width = 22, CustomWidth = true },  // Số tiền thanh toán
                new Column { Min = 5, Max = 5, Width = 22, CustomWidth = true },  // Hình thức thanh toán
                new Column { Min = 6, Max = 6, Width = 35, CustomWidth = true }   // Ghi chú
            );

            var mergeCells = new MergeCells();

            // Header row
            var headerRow = new Row { RowIndex = 1 };
            headerRow.Append(
                CreateCell("Số hợp đồng", 2),      // Bold, center
                CreateCell("Mã phiếu thu", 2),
                CreateCell("Tổng chi phí", 2),
                CreateCell("Số tiền thanh toán", 2),
                CreateCell("Hình thức thanh toán", 2),
                CreateCell("Ghi chú", 2)
            );
            sheetData.Append(headerRow);

            // Data rows
            uint currentRow = 2;
            foreach (var group in groupedData)
            {
                var items = group.ToList();
                uint startRow = currentRow;

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var dataRow = new Row { RowIndex = currentRow };

                    // Cột Số hợp đồng - chỉ điền ở dòng đầu
                    if (i == 0)
                    {
                        dataRow.Append(CreateCell(item.SoHopDong, 1)); // Center aligned
                    }
                    else
                    {
                        dataRow.Append(CreateCell("", 1));
                    }

                    dataRow.Append(
                        CreateCell(item.MaPhieuThu, 1),
                        CreateNumberCell(i == 0 ? item.TongChiPhi : 0, 3), // Number format, center
                        CreateNumberCell(item.SoTienThanhToan, 3),
                        CreateCell(item.HinhThucThanhToan, 1),
                        CreateCell(item.GhiChu, 4) // Left aligned with border
                    );

                    sheetData.Append(dataRow);
                    currentRow++;
                }

                // Merge cells cho Số hợp đồng và Tổng chi phí nếu có nhiều hơn 1 dòng
                if (items.Count > 1)
                {
                    // Merge cột A (Số hợp đồng)
                    mergeCells.Append(new MergeCell { Reference = $"A{startRow}:A{currentRow - 1}" });
                    // Merge cột C (Tổng chi phí)
                    mergeCells.Append(new MergeCell { Reference = $"C{startRow}:C{currentRow - 1}" });
                }
            }

            var worksheet = new Worksheet();
            worksheet.Append(columns);
            worksheet.Append(sheetData);

            if (mergeCells.HasChildren)
            {
                worksheet.InsertAfter(mergeCells, sheetData);
            }

            worksheetPart.Worksheet = worksheet;
            worksheetPart.Worksheet.Save();

            // Add sheet to workbook
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Lịch sử thanh toán"
            });

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Create stylesheet for Excel formatting
    /// </summary>
    private static Stylesheet CreateStylesheet()
    {
        return new Stylesheet(
            new Fonts(
                new Font( // Index 0 - Normal
                    new FontSize { Val = 11 },
                    new FontName { Val = "Calibri" }
                ),
                new Font( // Index 1 - Bold
                    new Bold(),
                    new FontSize { Val = 11 },
                    new FontName { Val = "Calibri" }
                )
            ),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }), // Index 0
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }) // Index 1
            ),
            new Borders(
                new Border( // Index 0 - No border
                    new LeftBorder(),
                    new RightBorder(),
                    new TopBorder(),
                    new BottomBorder(),
                    new DiagonalBorder()
                ),
                new Border( // Index 1 - Thin border
                    new LeftBorder { Style = BorderStyleValues.Thin },
                    new RightBorder { Style = BorderStyleValues.Thin },
                    new TopBorder { Style = BorderStyleValues.Thin },
                    new BottomBorder { Style = BorderStyleValues.Thin },
                    new DiagonalBorder()
                )
            ),
            new CellFormats(
                new CellFormat(), // Index 0 - Default
                new CellFormat   // Index 1 - Center aligned with border
                {
                    Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                    BorderId = 1,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat   // Index 2 - Bold, center aligned with border (header)
                {
                    FontId = 1,
                    Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                    BorderId = 1,
                    ApplyFont = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat   // Index 3 - Number format, center aligned with border
                {
                    NumberFormatId = 3, // #,##0
                    Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Center, Vertical = VerticalAlignmentValues.Center },
                    BorderId = 1,
                    ApplyNumberFormat = true,
                    ApplyBorder = true,
                    ApplyAlignment = true
                },
                new CellFormat   // Index 4 - Left aligned with border (for notes)
                {
                    Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center },
                    BorderId = 1,
                    ApplyBorder = true,
                    ApplyAlignment = true
                }
            )
        );
    }

    /// <summary>
    /// Create a text cell with style
    /// </summary>
    private static Cell CreateCell(string text, uint styleIndex)
    {
        return new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(text ?? ""),
            StyleIndex = styleIndex
        };
    }

    /// <summary>
    /// Create a number cell with style
    /// </summary>
    private static Cell CreateNumberCell(decimal value, uint styleIndex)
    {
        return new Cell
        {
            DataType = CellValues.Number,
            CellValue = new CellValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            StyleIndex = styleIndex
        };
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
