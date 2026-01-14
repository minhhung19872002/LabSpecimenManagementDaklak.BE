using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Quotations;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Area("app")]
[Route("api/v1/quotations")]
public class QuotationController : LabSpecimenManagementController
{
    private readonly IQuotationAppService _quotationAppService;

    public QuotationController(IQuotationAppService quotationAppService)
    {
        _quotationAppService = quotationAppService;
    }

    [HttpGet]
    public async Task<PagedResultDto<QuotationListDto>> GetListAsync([FromQuery] GetQuotationListInput input)
    {
        return await _quotationAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public async Task<QuotationDto> GetAsync(Guid id)
    {
        return await _quotationAppService.GetAsync(id);
    }

    [HttpPost]
    public async Task<QuotationDto> CreateAsync([FromBody] CreateUpdateQuotationDto input)
    {
        return await _quotationAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public async Task<QuotationDto> UpdateAsync(Guid id, [FromBody] CreateUpdateQuotationDto input)
    {
        return await _quotationAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        await _quotationAppService.DeleteAsync(id);
    }

    [HttpGet("generate-number")]
    public async Task<string> GenerateQuotationNumberAsync()
    {
        return await _quotationAppService.GenerateQuotationNumberAsync();
    }

    [HttpPost("send-email")]
    public async Task SendEmailAsync([FromBody] SendQuotationEmailDto input)
    {
        await _quotationAppService.SendEmailAsync(input);
    }

    [HttpPost("generate-pdf")]
    public async Task<IActionResult> GeneratePdfAsync([FromBody] GenerateQuotationPdfDto input)
    {
        var pdfData = await _quotationAppService.GeneratePdfAsync(input);
        return File(pdfData, "application/pdf", "BaoGia.pdf");
    }
}
