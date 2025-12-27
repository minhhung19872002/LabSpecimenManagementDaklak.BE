using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Testing;
using Microsoft.AspNetCore.Authorization;
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

    public TestingController(ITestingAppService testingAppService)
    {
        _testingAppService = testingAppService;
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
            throw new UserFriendlyException("Vui lòng chọn file để upload");
        }

        // Validate file type (only images)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            throw new UserFriendlyException("Chỉ chấp nhận file hình ảnh (jpg, jpeg, png, gif, bmp, webp)");
        }

        // Generate unique file path
        var fileId = Guid.NewGuid().ToString("N");
        var fileName = file.FileName;
        var uploadsFolder = Path.Combine("uploads", "testing-images", id.ToString());
        var filePath = Path.Combine(uploadsFolder, $"{fileId}{fileExtension}");

        // Ensure directory exists
        Directory.CreateDirectory(uploadsFolder);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

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
            filePath,
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
            throw new UserFriendlyException("Vui lòng chọn ít nhất 1 file để upload");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        var results = new List<SampleTestingImageDto>();
        var uploadsFolder = Path.Combine("uploads", "testing-images", id.ToString());

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
                    filePath,
                    file.Length
                );
                results.Add(result);
            }
            catch (UserFriendlyException)
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
}
