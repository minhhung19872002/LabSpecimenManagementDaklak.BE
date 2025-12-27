using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// Controller xử lý upload/download files
/// </summary>
[RemoteService]
[Route("api/v1/files")]
[Authorize]
public class FileController : LabSpecimenManagementController
{
    private readonly IFileAppService _fileAppService;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    public FileController(IFileAppService fileAppService)
    {
        _fileAppService = fileAppService;
    }

    /// <summary>
    /// Upload file hình ảnh
    /// </summary>
    /// <param name="file">File hình ảnh (max 10MB)</param>
    /// <returns>Thông tin file đã upload</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<FileUploadResultDto> UploadAsync(IFormFile file)
    {
        ValidateFile(file);

        using var stream = file.OpenReadStream();
        using var memoryStream = new System.IO.MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var fileContent = memoryStream.ToArray();

        return await _fileAppService.UploadAsync(file.FileName, fileContent);
    }

    /// <summary>
    /// Upload nhiều file hình ảnh
    /// </summary>
    /// <param name="files">Danh sách file hình ảnh (mỗi file max 10MB)</param>
    /// <returns>Danh sách thông tin file đã upload</returns>
    [HttpPost("upload-multiple")]
    [RequestSizeLimit(MaxFileSize * 10)] // Allow up to 10 files
    public async Task<FileUploadResultDto[]> UploadMultipleAsync([FromForm] IFormFile[] files)
    {
        if (files == null || files.Length == 0)
        {
            throw new UserFriendlyException("Vui lòng chọn ít nhất một file để upload.");
        }

        var results = new FileUploadResultDto[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            ValidateFile(files[i]);
            using var stream = files[i].OpenReadStream();
            using var memoryStream = new System.IO.MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            results[i] = await _fileAppService.UploadAsync(files[i].FileName, fileContent);
        }

        return results;
    }

    /// <summary>
    /// Lấy file theo FileId
    /// </summary>
    /// <param name="fileId">ID của file</param>
    /// <returns>File stream</returns>
    [HttpGet("{fileId}")]
    [AllowAnonymous] // Allow anonymous access for image display
    public async Task<IActionResult> GetAsync(string fileId)
    {
        var (fileStream, fileName, contentType) = await _fileAppService.GetFileAsync(fileId);

        if (fileStream == null)
        {
            return NotFound(new { message = "File không tồn tại." });
        }

        return File(fileStream, contentType, fileName);
    }

    /// <summary>
    /// Xóa file theo FileId
    /// </summary>
    /// <param name="fileId">ID của file</param>
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteAsync(string fileId)
    {
        var exists = await _fileAppService.ExistsAsync(fileId);
        if (!exists)
        {
            return NotFound(new { message = "File không tồn tại." });
        }

        await _fileAppService.DeleteAsync(fileId);
        return Ok(new { message = "File đã được xóa." });
    }

    #region Private Methods

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new UserFriendlyException("Vui lòng chọn file để upload.");
        }

        // Validate extension
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            throw new UserFriendlyException($"Chỉ cho phép upload file hình ảnh ({string.Join(", ", AllowedImageExtensions)}).");
        }

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            throw new UserFriendlyException($"Kích thước file không được vượt quá {MaxFileSize / (1024 * 1024)} MB.");
        }
    }

    #endregion
}
