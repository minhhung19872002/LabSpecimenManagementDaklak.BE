using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.Import;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/import")]
[Authorize(LabSpecimenManagementPermissions.Import.Default)]
public class ImportController : LabSpecimenManagementController
{
    private readonly IImportAppService _importAppService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ImportController(
        IImportAppService importAppService,
        IWebHostEnvironment webHostEnvironment)
    {
        _importAppService = importAppService;
        _webHostEnvironment = webHostEnvironment;
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

    /// <summary>
    /// Get template for importing TestMethods with Indicators
    /// </summary>
    [HttpGet("test-methods-with-indicators/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestMethods)]
    public virtual IActionResult GetTestMethodWithIndicatorsTemplate()
    {
        var bytes = _importAppService.GetTestMethodWithIndicatorsTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_PhuongPhapThu.xlsx");
    }

    #endregion

    #region TestMethod with Indicators Import

    /// <summary>
    /// Preview TestMethod with Indicators import - validates data without saving
    /// </summary>
    [HttpPost("test-methods-with-indicators/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestMethods)]
    [DisableValidation]
    public virtual async Task<TestMethodImportPreviewDto> PreviewTestMethodsWithIndicatorsAsync(IFormFile file)
    {
        ValidateFile(file);
        // Convert to byte array to avoid ABP validation issues with Stream
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.PreviewTestMethodsWithIndicatorsAsync(fileBytes);
    }

    /// <summary>
    /// Import TestMethods with Indicators (upsert logic)
    /// </summary>
    [HttpPost("test-methods-with-indicators")]
    [Authorize(LabSpecimenManagementPermissions.Import.TestMethods)]
    [DisableValidation]
    public virtual async Task<ImportResultDto> ImportTestMethodsWithIndicatorsAsync(IFormFile file)
    {
        ValidateFile(file);
        // Convert to byte array to avoid ABP validation issues with Stream
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.ImportTestMethodsWithIndicatorsAsync(fileBytes);
    }

    #endregion

    #region Standard with Indicators Import

    /// <summary>
    /// Get template for importing Standards with Indicators
    /// </summary>
    [HttpGet("standards-with-indicators/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.Standards)]
    public virtual IActionResult GetStandardWithIndicatorsTemplate()
    {
        var bytes = _importAppService.GetStandardWithIndicatorsTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_TieuChuan.xlsx");
    }

    /// <summary>
    /// Preview Standard with Indicators import - validates data without saving
    /// </summary>
    [HttpPost("standards-with-indicators/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.Standards)]
    [DisableValidation]
    public virtual async Task<StandardImportPreviewDto> PreviewStandardsWithIndicatorsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.PreviewStandardsWithIndicatorsAsync(fileBytes);
    }

    /// <summary>
    /// Import Standards with Indicators (upsert logic)
    /// </summary>
    [HttpPost("standards-with-indicators")]
    [Authorize(LabSpecimenManagementPermissions.Import.Standards)]
    [DisableValidation]
    public virtual async Task<ImportResultDto> ImportStandardsWithIndicatorsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.ImportStandardsWithIndicatorsAsync(fileBytes);
    }

    #endregion

    #region Organization + TestRequest Import (Legacy Data)

    /// <summary>
    /// Get template for importing Organization + TestRequest from legacy system
    /// </summary>
    [HttpGet("organization-test-requests/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.Customers)]
    public virtual IActionResult GetOrganizationTestRequestTemplate()
    {
        var bytes = _importAppService.GetOrganizationTestRequestTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_CoSo_YeuCau.xlsx");
    }

    /// <summary>
    /// Preview Organization + TestRequest import - validates data without saving
    /// Organization upsert: match by OrganizationName (trim, case-insensitive)
    /// TestRequest: always create new with status = Completed
    /// </summary>
    [HttpPost("organization-test-requests/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.Customers)]
    [DisableValidation]
    public virtual async Task<OrganizationTestRequestImportPreviewDto> PreviewOrganizationTestRequestsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.PreviewOrganizationTestRequestsAsync(fileBytes);
    }

    /// <summary>
    /// Import Organization + TestRequest from Excel file
    /// Organization upsert: match by OrganizationName (trim, case-insensitive)
    /// TestRequest: always create new with status = Completed
    /// </summary>
    [HttpPost("organization-test-requests")]
    [Authorize(LabSpecimenManagementPermissions.Import.Customers)]
    [DisableValidation]
    public virtual async Task<ImportResultDto> ImportOrganizationTestRequestsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.ImportOrganizationTestRequestsAsync(fileBytes);
    }

    #endregion

    #region IndicatorGroup + Indicator Import

    /// <summary>
    /// Get template for importing IndicatorGroup + Indicator
    /// </summary>
    [HttpGet("indicator-groups/template")]
    [Authorize(LabSpecimenManagementPermissions.Import.Default)]
    public virtual IActionResult GetIndicatorGroupTemplate()
    {
        var bytes = _importAppService.GetIndicatorGroupTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_NhomChiTieu.xlsx");
    }

    /// <summary>
    /// Preview IndicatorGroup + Indicator import - validates data without saving
    /// IndicatorGroup upsert: match by Name (trim, case-insensitive)
    /// Indicator upsert: match by Name (trim, case-insensitive)
    /// </summary>
    [HttpPost("indicator-groups/preview")]
    [Authorize(LabSpecimenManagementPermissions.Import.Default)]
    [DisableValidation]
    public virtual async Task<IndicatorGroupImportPreviewDto> PreviewIndicatorGroupsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.PreviewIndicatorGroupsAsync(fileBytes);
    }

    /// <summary>
    /// Import IndicatorGroup + Indicator from Excel file
    /// IndicatorGroup upsert: match by Name (trim, case-insensitive)
    /// Indicator upsert: match by Name (trim, case-insensitive)
    /// </summary>
    [HttpPost("indicator-groups")]
    [Authorize(LabSpecimenManagementPermissions.Import.Default)]
    [DisableValidation]
    public virtual async Task<ImportResultDto> ImportIndicatorGroupsAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.ImportIndicatorGroupsAsync(fileBytes);
    }

    #endregion

    #region Specimen Import for TestRequestForm

    /// <summary>
    /// Get template for importing Specimens in TestRequestForm
    /// Template columns: Tên mẫu, Loại mẫu, Địa chỉ lấy mẫu, Lượng mẫu, Tình trạng mẫu, Chỉ tiêu kiểm nghiệm
    /// </summary>
    [HttpGet("specimens-for-test-request/template")]
    [Authorize] // Any authenticated user can download template
    public virtual IActionResult GetSpecimenForTestRequestTemplate()
    {
        var templatePath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath,
            "templates", "Meta Data Template", "Phiếu thử nghiệm.xlsx");

        if (!System.IO.File.Exists(templatePath))
        {
            return NotFound("Template file not found");
        }

        var bytes = System.IO.File.ReadAllBytes(templatePath);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Template_PhieuThuNghiem.xlsx");
    }

    /// <summary>
    /// Preview Specimens for TestRequest import - validates data without saving
    /// Groups rows by SampleName, validates Indicators exist
    /// </summary>
    [HttpPost("specimens-for-test-request/preview")]
    [Authorize] // Any authenticated user can preview
    [DisableValidation]
    public virtual async Task<SpecimenForTestRequestImportPreviewDto> PreviewSpecimensForTestRequestAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.PreviewSpecimensForTestRequestAsync(fileBytes);
    }

    #endregion

    #region SampleTypeCategory Import

    /// <summary>
    /// Get template for importing SampleTypeCategory
    /// Template columns: Tên Danh mục loại, Mô tả
    /// </summary>
    [HttpGet("sample-type-categories/template")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
    public virtual IActionResult GetSampleTypeCategoryTemplate()
    {
        var bytes = _importAppService.GetSampleTypeCategoryTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Danh_muc_loai_mau.xlsx");
    }

    /// <summary>
    /// Preview SampleTypeCategory import - validates data without saving
    /// Upsert: match by Name (normalized, case-insensitive)
    /// </summary>
    [HttpPost("sample-type-categories/preview")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    [DisableValidation]
    public virtual async Task<SampleTypeCategoryImportPreviewDto> PreviewSampleTypeCategoriesAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.PreviewSampleTypeCategoriesAsync(fileBytes);
    }

    /// <summary>
    /// Import SampleTypeCategories from Excel file
    /// Upsert logic: match by Name (normalized, case-insensitive)
    /// </summary>
    [HttpPost("sample-type-categories")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    [DisableValidation]
    public virtual async Task<ImportResultDto> ImportSampleTypeCategoriesAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.ImportSampleTypeCategoriesAsync(fileBytes);
    }

    #endregion

    #region SpecimenType Import

    /// <summary>
    /// Get template for importing SpecimenType
    /// Template columns: Tên Danh mục loại, Nền mẫu, Tên chỉ tiêu
    /// </summary>
    [HttpGet("specimen-types/template")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
    public virtual IActionResult GetSpecimenTypeTemplate()
    {
        var bytes = _importAppService.GetSpecimenTypeTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Nen_Mau.xlsx");
    }

    /// <summary>
    /// Preview SpecimenType import - validates data without saving
    /// Upsert: match by (CategoryName + SpecimenTypeName) normalized
    /// Auto-generate unique Code for new SpecimenTypes
    /// </summary>
    [HttpPost("specimen-types/preview")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    [DisableValidation]
    public virtual async Task<SpecimenTypeImportPreviewDto> PreviewSpecimenTypesAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.PreviewSpecimenTypesAsync(fileBytes);
    }

    /// <summary>
    /// Import SpecimenTypes from Excel file
    /// Upsert logic: match by (CategoryName + SpecimenTypeName) normalized
    /// Auto-generate unique Code for new SpecimenTypes
    /// </summary>
    [HttpPost("specimen-types")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    [DisableValidation]
    public virtual async Task<ImportResultDto> ImportSpecimenTypesAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _importAppService.ImportSpecimenTypesAsync(fileBytes);
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
