using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.TestRequests;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LabSpecimenManagement.Reports;

[RemoteService(IsEnabled = false)]
public class ReportAppService :
    CrudAppService<
        Report,
        ReportDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateReportDto,
        UpdateReportDto>,
    IReportAppService
{
    private readonly ICurrentUser _currentUser;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<Specimen, Guid> _specimenRepository;
    private readonly IRepository<Test, Guid> _testRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public ReportAppService(
        IRepository<Report, Guid> repository,
        ICurrentUser currentUser,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IUserNameProvider userNameProvider,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
        : base(repository)
    {
        _currentUser = currentUser;
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _userNameProvider = userNameProvider;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
    }

    protected override async Task<ReportDto> MapToGetOutputDtoAsync(Report entity)
    {
        var dto = await base.MapToGetOutputDtoAsync(entity);
        await PopulateRelatedDataAsync(dto, entity);
        return dto;
    }

    protected override async Task<ReportDto> MapToGetListOutputDtoAsync(Report entity)
    {
        var dto = await base.MapToGetListOutputDtoAsync(entity);
        await PopulateRelatedDataAsync(dto, entity);
        return dto;
    }

    private async Task PopulateRelatedDataAsync(ReportDto dto, Report entity)
    {
        // Populate TestRequestNumber
        var testRequest = await _testRequestRepository.FindAsync(entity.TestRequestId);
        if (testRequest != null)
        {
            dto.TestRequestNumber = testRequest.RequestNumber;
        }

        // Populate SpecimenCodes from SpecimenIds JSON
        if (!string.IsNullOrEmpty(entity.SpecimenIds))
        {
            try
            {
                var specimenIds = JsonSerializer.Deserialize<List<Guid>>(entity.SpecimenIds);
                if (specimenIds != null && specimenIds.Count > 0)
                {
                    var specimenCodes = new List<string>();
                    foreach (var specimenId in specimenIds)
                    {
                        var specimen = await _specimenRepository.FindAsync(specimenId);
                        if (specimen != null)
                        {
                            specimenCodes.Add(specimen.Code);
                        }
                    }
                    dto.SpecimenCodes = specimenCodes;
                }
            }
            catch
            {
                // If parsing fails, SpecimenCodes remains null
            }
        }

        // Populate TestCount from TestIds JSON
        if (!string.IsNullOrEmpty(entity.TestIds))
        {
            try
            {
                var testIds = JsonSerializer.Deserialize<List<Guid>>(entity.TestIds);
                dto.TestCount = testIds?.Count ?? 0;
            }
            catch
            {
                dto.TestCount = 0;
            }
        }

        // Populate user names using UserNameProvider
        var userIds = new[] { entity.PreparedById, entity.ReviewedById, entity.ApprovedById };
        var userNames = await _userNameProvider.GetUserNamesAsync(userIds);

        if (entity.PreparedById.HasValue && userNames.TryGetValue(entity.PreparedById.Value, out var preparedByName))
        {
            dto.PreparedByName = preparedByName;
        }

        if (entity.ReviewedById.HasValue && userNames.TryGetValue(entity.ReviewedById.Value, out var reviewedByName))
        {
            dto.ReviewedByName = reviewedByName;
        }

        if (entity.ApprovedById.HasValue && userNames.TryGetValue(entity.ApprovedById.Value, out var approvedByName))
        {
            dto.ApprovedByName = approvedByName;
        }
    }

    protected override async Task<Report> MapToEntityAsync(CreateReportDto createInput)
    {
        var reportNumber = await GenerateReportNumberAsync();

        var entity = new Report(
            GuidGenerator.Create(),
            reportNumber,
            createInput.Title,
            createInput.TestRequestId,
            createInput.CustomerName
        );

        entity.CustomerId = createInput.CustomerId;
        entity.SpecimenIds = createInput.SpecimenIds;
        entity.TestIds = createInput.TestIds;
        entity.PreparedById = _currentUser.Id;
        entity.Notes = createInput.Notes;

        return entity;
    }

    public async Task<ReportDto> SubmitForApprovalAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = ReportStatus.PendingApproval;

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<ReportDto> ApproveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = ReportStatus.Approved;
        entity.ApprovedById = _currentUser.Id;
        entity.ApprovedDate = DateTime.Now;

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<ReportDto> RejectAsync(Guid id, string? reason)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = ReportStatus.Rejected;
        if (!string.IsNullOrEmpty(reason))
        {
            entity.Notes = (entity.Notes ?? "") + $"\n[Rejected]: {reason}";
        }

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<ReportDto> IssueAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = ReportStatus.Issued;
        entity.IssuedDate = DateTime.Now;
        entity.QRCode = GenerateQRCodeUrl(entity.ReportNumber);

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task SendEmailAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        // TODO: Implement email sending logic
        entity.IsEmailSent = true;
        entity.EmailSentDate = DateTime.Now;

        await Repository.UpdateAsync(entity);
    }

    public async Task<ReportDto?> GetByReportNumberAsync(string reportNumber)
    {
        var entity = await Repository.FirstOrDefaultAsync(x => x.ReportNumber == reportNumber);
        return entity != null ? await MapToGetOutputDtoAsync(entity) : null;
    }

    public async Task<ListResultDto<ReportDto>> GetByTestRequestAsync(Guid testRequestId)
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.TestRequestId == testRequestId)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        return new ListResultDto<ReportDto>(
            ObjectMapper.Map<System.Collections.Generic.List<Report>, System.Collections.Generic.List<ReportDto>>(items)
        );
    }

    private async Task<string> GenerateReportNumberAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("RPT");
            var count = await Repository.CountAsync(x => x.ReportNumber.StartsWith(prefix));
            return _codeGenerator.GenerateCodeWithDate("RPT", count);
        }
    }

    private string GenerateQRCodeUrl(string reportNumber)
    {
        // Generate URL for QR code verification
        return $"/api/app/reports/verify/{reportNumber}";
    }

    public async Task<IRemoteStreamContent> ExportPdfAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        // If PDF already exists, return it
        if (!string.IsNullOrEmpty(entity.PdfPath) && File.Exists(entity.PdfPath))
        {
            var fileStream = new FileStream(entity.PdfPath, FileMode.Open, FileAccess.Read);
            return new RemoteStreamContent(fileStream, $"{entity.ReportNumber}.pdf", "application/pdf");
        }

        // Generate simple PDF content (placeholder - implement actual PDF generation with library like QuestPDF/iTextSharp)
        var pdfContent = GeneratePdfContent(entity);
        var memoryStream = new MemoryStream(pdfContent);

        return new RemoteStreamContent(memoryStream, $"{entity.ReportNumber}.pdf", "application/pdf");
    }

    public async Task<ReportVerifyResultDto> VerifyReportAsync(string reportNumber)
    {
        var entity = await Repository.FirstOrDefaultAsync(x => x.ReportNumber == reportNumber);

        if (entity == null)
        {
            return new ReportVerifyResultDto
            {
                IsValid = false,
                Message = "Report not found. The report number may be invalid or the report does not exist."
            };
        }

        if (entity.Status != ReportStatus.Issued)
        {
            return new ReportVerifyResultDto
            {
                IsValid = false,
                ReportNumber = entity.ReportNumber,
                Title = entity.Title,
                Status = entity.Status,
                Message = "This report has not been officially issued yet."
            };
        }

        return new ReportVerifyResultDto
        {
            IsValid = true,
            ReportNumber = entity.ReportNumber,
            Title = entity.Title,
            CustomerName = entity.CustomerName,
            Status = entity.Status,
            IssuedDate = entity.IssuedDate,
            Message = "This report is valid and has been officially issued."
        };
    }

    private byte[] GeneratePdfContent(Report report)
    {
        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Text("TRUNG TÂM KIỂM NGHIỆM")
                            .Bold().FontSize(16);
                        column.Item().AlignCenter().Text("ĐẮK LẮK")
                            .Bold().FontSize(14);
                        column.Item().Height(10);
                        column.Item().AlignCenter().Text("PHIẾU KẾT QUẢ THỬ NGHIỆM")
                            .Bold().FontSize(18);
                        column.Item().AlignCenter().Text("TEST REPORT")
                            .Italic().FontSize(12);
                        column.Item().Height(20);
                    });

                page.Content()
                    .Column(column =>
                    {
                        // Report info section
                        column.Item().Border(1).Padding(10).Column(innerColumn =>
                        {
                            innerColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Số báo cáo: {report.ReportNumber}").Bold();
                                row.RelativeItem().AlignRight().Text($"Ngày: {report.ReportDate:dd/MM/yyyy}");
                            });

                            innerColumn.Item().Height(10);

                            innerColumn.Item().Text($"Tiêu đề: {report.Title}");
                            innerColumn.Item().Text($"Khách hàng: {report.CustomerName}");

                            innerColumn.Item().Height(10);

                            innerColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Trạng thái: {GetStatusText(report.Status)}");
                                if (report.IssuedDate.HasValue)
                                {
                                    row.RelativeItem().AlignRight().Text($"Ngày phát hành: {report.IssuedDate:dd/MM/yyyy HH:mm}");
                                }
                            });
                        });

                        column.Item().Height(20);

                        // Notes section
                        if (!string.IsNullOrEmpty(report.Notes))
                        {
                            column.Item().Text("Ghi chú:").Bold();
                            column.Item().Border(1).Padding(10).Text(report.Notes);
                        }

                        column.Item().Height(30);

                        // Signature section
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Người lập báo cáo").Bold();
                                c.Item().Height(50);
                                c.Item().AlignCenter().Text("_______________");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignCenter().Text("Người phê duyệt").Bold();
                                c.Item().Height(50);
                                c.Item().AlignCenter().Text("_______________");
                            });
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Trang ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        using var memoryStream = new MemoryStream();
        document.GeneratePdf(memoryStream);
        return memoryStream.ToArray();
    }

    private static string GetStatusText(ReportStatus status)
    {
        return status switch
        {
            ReportStatus.Draft => "Bản nháp",
            ReportStatus.PendingApproval => "Chờ duyệt",
            ReportStatus.Approved => "Đã duyệt",
            ReportStatus.Issued => "Đã phát hành",
            ReportStatus.Rejected => "Từ chối",
            _ => status.ToString()
        };
    }
}
