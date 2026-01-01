using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.Import;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/import")]
[Authorize(LabSpecimenManagementPermissions.Import.Default)]
public class ImportController : LabSpecimenManagementController
{
    private readonly IImportAppService _importAppService;

    public ImportController(IImportAppService importAppService)
    {
        _importAppService = importAppService;
    }

    #region Preview Endpoints

    [HttpPost("test-requests/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestRequests)]
    public virtual async Task<ImportPreviewDto<TestRequestImportRow>> PreviewTestRequestsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.PreviewTestRequestsAsync(stream);
    }

    [HttpPost("specimens/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.Specimens)]
    public virtual async Task<ImportPreviewDto<SpecimenImportRow>> PreviewSpecimensAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.PreviewSpecimensAsync(stream);
    }

    [HttpPost("customers/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.Customers)]
    public virtual async Task<ImportPreviewDto<CustomerImportRow>> PreviewCustomersAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.PreviewCustomersAsync(stream);
    }

    [HttpPost("test-methods/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestMethods)]
    public virtual async Task<ImportPreviewDto<TestMethodImportRow>> PreviewTestMethodsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.PreviewTestMethodsAsync(stream);
    }

    #endregion

    #region Import Endpoints

    [HttpPost("test-requests")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestRequests)]
    public virtual async Task<ImportResultDto> ImportTestRequestsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.ImportTestRequestsAsync(stream);
    }

    [HttpPost("specimens")]
    [Authorize(LabSpecimenManagementPermissions.Import.Specimens)]
    public virtual async Task<ImportResultDto> ImportSpecimensAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.ImportSpecimensAsync(stream);
    }

    [HttpPost("customers")]
    [Authorize(LabSpecimenManagementPermissions.Import.Customers)]
    public virtual async Task<ImportResultDto> ImportCustomersAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.ImportCustomersAsync(stream);
    }

    [HttpPost("test-methods")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestMethods)]
    public virtual async Task<ImportResultDto> ImportTestMethodsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var stream = file.OpenReadStream();
        return await _importAppService.ImportTestMethodsAsync(stream);
    }

    #endregion

    #region Template Endpoints

    [HttpGet("test-requests/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestRequests)]
    public virtual IActionResult GetTestRequestTemplate()
    {
        var bytes = _importAppService.GetTestRequestTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_YeuCauThuNghiem.xlsx");
    }

    [HttpGet("specimens/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.Specimens)]
    public virtual IActionResult GetSpecimenTemplate()
    {
        var bytes = _importAppService.GetSpecimenTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Mau.xlsx");
    }

    [HttpGet("customers/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.Customers)]
    public virtual IActionResult GetCustomerTemplate()
    {
        var bytes = _importAppService.GetCustomerTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_KhachHang.xlsx");
    }

    [HttpGet("test-methods/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestMethods)]
    public virtual IActionResult GetTestMethodTemplate()
    {
        var bytes = _importAppService.GetTestMethodTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_PhuongPhapThuNghiem.xlsx");
    }

    #endregion

    #region Helper Methods

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("file", "Vui lòng chọn file Excel để import.");
        }

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new ValidationException("file", "Chỉ hỗ trợ file Excel (.xlsx, .xls).");
        }

        // Max 10MB
        if (file.Length > 10 * 1024 * 1024)
        {
            throw new ValidationException("file", "File không được vượt quá 10MB.");
        }
    }

    #endregion
}
