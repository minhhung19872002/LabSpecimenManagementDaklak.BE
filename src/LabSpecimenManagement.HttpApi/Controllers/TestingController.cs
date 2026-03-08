using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/testing")]
[Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
public class TestingController : LabSpecimenManagementController
{
    private readonly ITestingAppService _testingAppService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public TestingController(
        ITestingAppService testingAppService,
        IWebHostEnvironment webHostEnvironment)
    {
        _testingAppService = testingAppService;
        _webHostEnvironment = webHostEnvironment;
    }

    /// <summary>
    /// Get filtered and paged list of testing tasks
    /// </summary>
    [HttpGet]
    public virtual async Task<PagedResultDto<TestingListDto>> GetListAsync([FromQuery] GetTestingListInput input)
    {
        return await _testingAppService.GetListAsync(input);
    }

    /// <summary>
    /// Get status statistics for testing tasks
    /// </summary>
    [HttpGet("statistics")]
    public virtual async Task<TestingStatusStatisticsDto> GetStatusStatisticsAsync([FromQuery] TestingTabType tab)
    {
        return await _testingAppService.GetStatusStatisticsAsync(tab);
    }

    /// <summary>
    /// Get sample assignment log for testing page (query from LaboratoryTaskIndicator)
    /// </summary>
    [HttpGet("sample-assignment-log")]
    public virtual async Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync([FromQuery] GetSampleAssignmentLogInput input)
    {
        return await _testingAppService.GetSampleAssignmentLogAsync(input);
    }

    /// <summary>
    /// Get statistics for sample assignment log (by QLKTDisplayStatus)
    /// </summary>
    [HttpGet("sample-assignment-log/statistics")]
    public virtual async Task<SampleAssignmentLogStatisticsDto> GetSampleAssignmentLogStatisticsAsync()
    {
        return await _testingAppService.GetSampleAssignmentLogStatisticsAsync();
    }

    /// <summary>
    /// Get testing task detail by LaboratoryTask ID
    /// </summary>
    /// <param name="id">The LaboratoryTask ID</param>
    /// <param name="viewMode">Optional view mode - 'my' (only user's data), 'all' (full data)</param>
    [HttpGet("{id}")]
    public virtual async Task<TestingDetailDto> GetAsync(Guid id, [FromQuery] string? viewMode = null)
    {
        // Convert string viewMode to enum
        TestingTabType? tabType = viewMode?.ToLower() switch
        {
            "my" => TestingTabType.MyAssignments,
            "all" => TestingTabType.All,
            _ => null
        };
        return await _testingAppService.GetAsync(id, tabType);
    }

    /// <summary>
    /// Save results (draft) for indicators
    /// </summary>
    [HttpPost("{id}/save-results")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task SaveResultsAsync(Guid id, [FromBody] SaveResultsInput input)
    {
        await _testingAppService.SaveResultsAsync(id, input);
    }

    /// <summary>
    /// Submit indicators for approval
    /// </summary>
    [HttpPost("{id}/submit-approval")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task SubmitForApprovalAsync(Guid id, [FromBody] SubmitForApprovalInput input)
    {
        await _testingAppService.SubmitForApprovalAsync(id, input);
    }

    /// <summary>
    /// Get rejection history for an indicator
    /// </summary>
    [HttpGet("indicators/{indicatorId}/rejection-history")]
    public virtual async Task<List<RejectionHistoryDto>> GetRejectionHistoryAsync(Guid indicatorId)
    {
        return await _testingAppService.GetRejectionHistoryAsync(indicatorId);
    }

    /// <summary>
    /// Check-in indicators (bắt đầu thử nghiệm - ấn "Thực hiện")
    /// </summary>
    [HttpPost("{id}/check-in")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task CheckInIndicatorsAsync(Guid id, [FromBody] CheckInIndicatorsInput input)
    {
        await _testingAppService.CheckInIndicatorsAsync(id, input);
    }

    /// <summary>
    /// Submit results for approval (gửi kết quả cho QLKT)
    /// </summary>
    [HttpPost("{id}/submit-results")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task SubmitResultsAsync(Guid id, [FromBody] SubmitResultsInput input)
    {
        await _testingAppService.SubmitResultsAsync(id, input);
    }

    /// <summary>
    /// Upload sample testing image (before/after testing) - single file
    /// </summary>
    [HttpPost("{id}/samples/{sampleId}/images")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task<SampleTestingImageDto> UploadSampleTestingImageAsync(
        Guid id,
        Guid sampleId,
        [FromQuery] SampleTestingImageType imageType,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("file", "Vui lòng chọn file để upload");
        }

        // Validate file type (only images)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ValidationException("file", "Chỉ chấp nhận file hình ảnh (jpg, jpeg, png, gif, bmp, webp)");
        }

        // Generate unique file path - save to wwwroot/uploads/testing-images/
        var fileId = Guid.NewGuid().ToString("N");
        var fileName = file.FileName;
        var webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "testing-images", id.ToString());
        var filePath = Path.Combine(uploadsFolder, $"{fileId}{fileExtension}");

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Store relative path for database
        var relativePath = Path.Combine("uploads", "testing-images", id.ToString(), $"{fileId}{fileExtension}");

        var input = new UploadSampleTestingImageInput
        {
            SampleId = sampleId,
            ImageType = imageType
        };

        return await _testingAppService.UploadSampleTestingImageAsync(
            id,
            input,
            fileId,
            fileName,
            relativePath,
            file.Length
        );
    }

    /// <summary>
    /// Upload multiple sample testing images (before/after testing) - batch upload
    /// </summary>
    [HttpPost("{id}/samples/{sampleId}/images/batch")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task<List<SampleTestingImageDto>> UploadSampleTestingImagesAsync(
        Guid id,
        Guid sampleId,
        [FromQuery] SampleTestingImageType imageType,
        List<IFormFile> files)
    {
        if (files == null || !files.Any())
        {
            throw new ValidationException("files", "Vui lòng chọn ít nhất 1 file để upload");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var results = new List<SampleTestingImageDto>();
        var webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "testing-images", id.ToString());

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
            {
                continue;
            }

            // Validate file type (only images)
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                continue; // Skip non-image files
            }

            // Generate unique file path
            var fileId = Guid.NewGuid().ToString("N");
            var fileName = file.FileName;
            var filePath = Path.Combine(uploadsFolder, $"{fileId}{fileExtension}");

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Store relative path for database
            var relativePath = Path.Combine("uploads", "testing-images", id.ToString(), $"{fileId}{fileExtension}");

            var input = new UploadSampleTestingImageInput
            {
                SampleId = sampleId,
                ImageType = imageType
            };

            try
            {
                var result = await _testingAppService.UploadSampleTestingImageAsync(
                    id,
                    input,
                    fileId,
                    fileName,
                    relativePath,
                    file.Length
                );
                results.Add(result);
            }
            catch (Common.Exceptions.InvalidOperationException)
            {
                // Max images reached, stop uploading
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Delete sample testing image
    /// </summary>
    [HttpDelete("samples/images/{imageId}")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task DeleteSampleTestingImageAsync(Guid imageId)
    {
        await _testingAppService.DeleteSampleTestingImageAsync(imageId);
    }

    /// <summary>
    /// Get sample testing images
    /// </summary>
    [HttpGet("{id}/samples/{sampleId}/images")]
    public virtual async Task<List<SampleTestingImageDto>> GetSampleTestingImagesAsync(
        Guid id,
        Guid sampleId,
        [FromQuery] SampleTestingImageType? imageType = null)
    {
        return await _testingAppService.GetSampleTestingImagesAsync(id, sampleId, imageType);
    }

    #region Indicator Image Endpoints

    /// <summary>
    /// Upload indicator image - single file
    /// </summary>
    [HttpPost("{id}/indicators/{indicatorId}/images")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task<IndicatorImageDto> UploadIndicatorImageAsync(
        Guid id,
        Guid indicatorId,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("file", "Vui lòng chọn file để upload");
        }

        // Validate file type (only images)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ValidationException("file", "Chỉ chấp nhận file hình ảnh (jpg, jpeg, png, gif, bmp, webp)");
        }

        // Generate unique file ID and path
        var fileId = Guid.NewGuid().ToString("N");
        var webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "indicator-images", id.ToString());

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{fileId}{fileExtension}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        var relativePath = $"uploads/indicator-images/{id}/{fileName}";

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Save to database
        return await _testingAppService.UploadIndicatorImageAsync(
            id,
            indicatorId,
            fileId,
            file.FileName,
            relativePath,
            file.Length);
    }

    /// <summary>
    /// Upload multiple indicator images - batch upload
    /// </summary>
    [HttpPost("{id}/indicators/{indicatorId}/images/batch")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task<List<IndicatorImageDto>> UploadIndicatorImagesAsync(
        Guid id,
        Guid indicatorId,
        List<IFormFile> files)
    {
        if (files == null || !files.Any())
        {
            throw new ValidationException("files", "Vui lòng chọn ít nhất 1 file để upload");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var results = new List<IndicatorImageDto>();
        var webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "indicator-images", id.ToString());

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
            {
                continue;
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                continue; // Skip invalid files
            }

            // Generate unique file ID and path
            var fileId = Guid.NewGuid().ToString("N");
            var fileName = $"{fileId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            var relativePath = $"uploads/indicator-images/{id}/{fileName}";

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save to database
            var result = await _testingAppService.UploadIndicatorImageAsync(
                id,
                indicatorId,
                fileId,
                file.FileName,
                relativePath,
                file.Length);

            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Delete indicator image
    /// </summary>
    [HttpDelete("indicators/images/{imageId}")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task DeleteIndicatorImageAsync(Guid imageId)
    {
        await _testingAppService.DeleteIndicatorImageAsync(imageId);
    }

    /// <summary>
    /// Get indicator images
    /// </summary>
    [HttpGet("indicators/{indicatorId}/images")]
    public virtual async Task<List<IndicatorImageDto>> GetIndicatorImagesAsync(Guid indicatorId)
    {
        return await _testingAppService.GetIndicatorImagesAsync(indicatorId);
    }

    #endregion

    #region Sample Received

    /// <summary>
    /// KTV nhận mẫu - đánh dấu mẫu đã nhận trước khi thực hiện thí nghiệm
    /// </summary>
    [HttpPost("{id}/samples/{sampleId}/receive")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task ReceiveSampleAsync(Guid id, Guid sampleId)
    {
        await _testingAppService.ReceiveSampleAsync(id, sampleId);
    }

    /// <summary>
    /// Kiểm tra mẫu đã được nhận chưa
    /// </summary>
    [HttpGet("{id}/samples/{sampleId}/is-received")]
    public virtual async Task<bool> IsSampleReceivedAsync(Guid id, Guid sampleId)
    {
        return await _testingAppService.IsSampleReceivedAsync(id, sampleId);
    }

    #endregion

    #region Cannot Test (KTV báo không TN được)

    /// <summary>
    /// KTV báo không thể thử nghiệm chỉ tiêu
    /// </summary>
    [HttpPost("{id}/indicators/{indicatorId}/cannot-test")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task<TestingIndicatorDto> MarkCannotTestAsync(
        Guid id,
        Guid indicatorId,
        [FromBody] CannotTestDto input)
    {
        return await _testingAppService.MarkCannotTestAsync(id, indicatorId, input);
    }

    /// <summary>
    /// Upload hình ảnh cho rejection history - single file
    /// </summary>
    [HttpPost("rejection-histories/{rejectionHistoryId}/images")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task<RejectionHistoryImageDto> UploadRejectionHistoryImageAsync(
        Guid rejectionHistoryId,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("file", "Vui lòng chọn file để upload");
        }

        // Validate file type (only images)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new ValidationException("file", "Chỉ chấp nhận file hình ảnh (jpg, jpeg, png, gif, bmp, webp)");
        }

        // Generate unique file ID and path
        var fileId = Guid.NewGuid().ToString("N");
        var webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "rejection-history-images");

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{fileId}{fileExtension}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        var relativePath = $"uploads/rejection-history-images/{fileName}";

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Save to database
        return await _testingAppService.UploadRejectionHistoryImageAsync(
            rejectionHistoryId,
            fileId,
            file.FileName,
            relativePath,
            file.Length);
    }

    /// <summary>
    /// Upload multiple rejection history images - batch upload
    /// </summary>
    [HttpPost("rejection-histories/{rejectionHistoryId}/images/batch")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task<List<RejectionHistoryImageDto>> UploadRejectionHistoryImagesAsync(
        Guid rejectionHistoryId,
        List<IFormFile> files)
    {
        if (files == null || !files.Any())
        {
            throw new ValidationException("files", "Vui lòng chọn ít nhất 1 file để upload");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var results = new List<RejectionHistoryImageDto>();
        var webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "rejection-history-images");

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
            {
                continue;
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                continue; // Skip invalid files
            }

            // Generate unique file ID and path
            var fileId = Guid.NewGuid().ToString("N");
            var fileName = $"{fileId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            var relativePath = $"uploads/rejection-history-images/{fileName}";

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                // Save to database
                var result = await _testingAppService.UploadRejectionHistoryImageAsync(
                    rejectionHistoryId,
                    fileId,
                    file.FileName,
                    relativePath,
                    file.Length);

                results.Add(result);
            }
            catch (ValidationException)
            {
                // Max images reached
                break;
            }
        }

        return results;
    }

    /// <summary>
    /// Xóa hình ảnh rejection history
    /// </summary>
    [HttpDelete("rejection-histories/images/{imageId}")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task DeleteRejectionHistoryImageAsync(Guid imageId)
    {
        await _testingAppService.DeleteRejectionHistoryImageAsync(imageId);
    }

    #endregion
}
