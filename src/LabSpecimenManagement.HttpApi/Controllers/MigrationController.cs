using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.DataMigration;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/migration")]
[Authorize(LabSpecimenManagementPermissions.Migration.Default)]
public class MigrationController : LabSpecimenManagementController
{
    private readonly IMigrationAppService _migrationAppService;

    public MigrationController(IMigrationAppService migrationAppService)
    {
        _migrationAppService = migrationAppService;
    }

    [HttpPost("vnpt/preview")]
    [Authorize(LabSpecimenManagementPermissions.Migration.Preview)]
    [DisableValidation]
    public async Task<MigrationPreviewDto> PreviewVnptMigrationAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _migrationAppService.PreviewVnptMigrationAsync(fileBytes);
    }

    [HttpPost("vnpt/import")]
    [Authorize(LabSpecimenManagementPermissions.Migration.Import)]
    [DisableValidation]
    public async Task<MigrationResultDto> ImportVnptMigrationAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _migrationAppService.ImportVnptMigrationAsync(fileBytes);
    }

    [HttpPost("vnpt/preview/export")]
    [Authorize(LabSpecimenManagementPermissions.Migration.Preview)]
    [DisableValidation]
    public async Task<IActionResult> ExportPreviewReportAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        var reportBytes = await _migrationAppService.ExportPreviewReportAsync(fileBytes);
        return File(reportBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "migration-preview-report.xlsx");
    }

    [HttpPost("vnpt/rollback")]
    [Authorize(LabSpecimenManagementPermissions.Migration.Import)]
    [DisableValidation]
    public async Task<MigrationRollbackResultDto> RollbackVnptMigrationAsync(IFormFile file)
    {
        ValidateFile(file);
        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        return await _migrationAppService.RollbackVnptMigrationAsync(fileBytes);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ValidationException("Vui lòng chọn file Excel để import.");

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new ValidationException("Chỉ hỗ trợ file Excel (.xlsx, .xls).");

        if (file.Length > 20 * 1024 * 1024)
            throw new ValidationException("File không được vượt quá 20MB.");
    }
}
