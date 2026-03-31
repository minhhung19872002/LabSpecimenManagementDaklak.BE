using System;
using System.IO;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Files;

/// <summary>
/// Service xử lý upload/download files
/// Lưu file vào thư mục wwwroot/uploads/samples/
/// </summary>
public class FileAppService : ApplicationService, IFileAppService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<FileAppService> _logger;
    private const string UploadFolder = "uploads/samples";
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".pdf", ".docx" };
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

    public FileAppService(
        IWebHostEnvironment webHostEnvironment,
        ILogger<FileAppService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    /// <summary>
    /// Upload file và lưu vào thư mục uploads
    /// </summary>
    public async Task<FileUploadResultDto> UploadAsync(string fileName, byte[] fileContent)
    {
        // Validate file name
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("fileName", "Tên file không được để trống.");
        }

        // Validate extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!IsAllowedExtension(extension))
        {
            throw new ValidationException("fileName", $"Chỉ cho phép upload file ({string.Join(", ", AllowedExtensions)}).");
        }

        // Validate file size
        if (fileContent.Length > MaxFileSize)
        {
            throw new ValidationException("fileSize", $"Kích thước file không được vượt quá {MaxFileSize / (1024 * 1024)} MB.");
        }

        // Generate unique file ID
        var fileId = Guid.NewGuid().ToString("N");
        var savedFileName = $"{fileId}{extension}";

        // Ensure upload directory exists
        var uploadPath = GetUploadPath();
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Save file
        var filePath = Path.Combine(uploadPath, savedFileName);
        try
        {
            await File.WriteAllBytesAsync(filePath, fileContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FileName}", fileName);
            throw new Common.Exceptions.InvalidOperationException("Không thể lưu file. Vui lòng thử lại.");
        }

        _logger.LogInformation("File uploaded successfully: {FileId}, Original name: {FileName}", fileId, fileName);

        return new FileUploadResultDto
        {
            FileId = fileId,
            FileName = fileName,
            FileSize = fileContent.Length,
            Url = $"/api/v1/files/{fileId}"
        };
    }

    /// <summary>
    /// Lấy file theo FileId
    /// Tìm trong wwwroot/uploads/ (cả samples và testing-images)
    /// </summary>
    public Task<(Stream? FileStream, string FileName, string ContentType)> GetFileAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return Task.FromResult<(Stream?, string, string)>((null, string.Empty, string.Empty));
        }

        var webRootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;

        // Search in wwwroot/uploads/samples/ folder first (for sample attachments)
        var uploadPath = Path.Combine(webRootPath, UploadFolder);
        foreach (var ext in AllowedExtensions)
        {
            var filePath = Path.Combine(uploadPath, $"{fileId}{ext}");
            if (File.Exists(filePath))
            {
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var contentType = GetContentType(ext);
                var fileName = $"{fileId}{ext}";
                return Task.FromResult<(Stream?, string, string)>((stream, fileName, contentType));
            }
        }

        // Search in wwwroot/uploads/testing-images/ folder (for testing images)
        var testingImagesPath = Path.Combine(webRootPath, "uploads", "testing-images");
        if (Directory.Exists(testingImagesPath))
        {
            foreach (var ext in AllowedExtensions)
            {
                var searchPattern = $"{fileId}{ext}";
                var files = Directory.GetFiles(testingImagesPath, searchPattern, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    var filePath = files[0];
                    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var contentType = GetContentType(ext);
                    var fileName = Path.GetFileName(filePath);
                    return Task.FromResult<(Stream?, string, string)>((stream, fileName, contentType));
                }
            }
        }

        // Search in wwwroot/uploads/indicator-images/ folder (for indicator images)
        var indicatorImagesPath = Path.Combine(webRootPath, "uploads", "indicator-images");
        if (Directory.Exists(indicatorImagesPath))
        {
            foreach (var ext in AllowedExtensions)
            {
                var searchPattern = $"{fileId}{ext}";
                var files = Directory.GetFiles(indicatorImagesPath, searchPattern, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    var filePath = files[0];
                    var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var contentType = GetContentType(ext);
                    var fileName = Path.GetFileName(filePath);
                    return Task.FromResult<(Stream?, string, string)>((stream, fileName, contentType));
                }
            }
        }

        return Task.FromResult<(Stream?, string, string)>((null, string.Empty, string.Empty));
    }

    /// <summary>
    /// Xóa file theo FileId
    /// </summary>
    public Task DeleteAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return Task.CompletedTask;
        }

        var uploadPath = GetUploadPath();

        // Find and delete file with any extension
        foreach (var ext in AllowedExtensions)
        {
            var filePath = Path.Combine(uploadPath, $"{fileId}{ext}");
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted: {FileId}", fileId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file: {FileId}", fileId);
                    throw new Common.Exceptions.InvalidOperationException("Không thể xóa file. Vui lòng thử lại.");
                }
                break;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Kiểm tra file có tồn tại không
    /// </summary>
    public Task<bool> ExistsAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return Task.FromResult(false);
        }

        var uploadPath = GetUploadPath();

        foreach (var ext in AllowedExtensions)
        {
            var filePath = Path.Combine(uploadPath, $"{fileId}{ext}");
            if (File.Exists(filePath))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    #region Private Methods

    private string GetUploadPath()
    {
        return Path.Combine(_webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath, UploadFolder);
    }

    private static bool IsAllowedExtension(string extension)
    {
        return Array.Exists(AllowedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    #endregion
}
