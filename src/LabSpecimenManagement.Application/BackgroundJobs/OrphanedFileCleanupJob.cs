using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

/// <summary>
/// Hangfire job để dọn dẹp các file rác (orphaned files):
/// - File đã upload nhưng không được tham chiếu trong database
/// - Upload folders: xóa file >= 2 ngày tuổi
/// - Temp folder: xóa file >= 1 ngày tuổi
/// - Chạy lúc 2:00 AM hàng ngày
/// </summary>
public class OrphanedFileCleanupJob : ITransientDependency
{
    private readonly IRepository<TestRequestSampleAttachment, Guid> _sampleAttachmentRepository;
    private readonly IRepository<SampleTestingImage, Guid> _sampleTestingImageRepository;
    private readonly IRepository<IndicatorImage, Guid> _indicatorImageRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<Report, Guid> _reportRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<OrphanedFileCleanupJob> _logger;

    // Config
    private const int UPLOAD_FILE_AGE_THRESHOLD_DAYS = 2;
    private const int TEMP_FILE_AGE_THRESHOLD_DAYS = 1;
    private const int BATCH_SIZE = 500;

    private static readonly string[] UPLOAD_FOLDERS = { "samples", "testing-images", "indicator-images" };
    private static readonly string[] IMAGE_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
    private static readonly string[] TEMP_EXTENSIONS = { ".docx", ".pdf", ".doc", ".xlsx", ".xls" };

    public OrphanedFileCleanupJob(
        IRepository<TestRequestSampleAttachment, Guid> sampleAttachmentRepository,
        IRepository<SampleTestingImage, Guid> sampleTestingImageRepository,
        IRepository<IndicatorImage, Guid> indicatorImageRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Report, Guid> reportRepository,
        IWebHostEnvironment environment,
        ILogger<OrphanedFileCleanupJob> logger)
    {
        _sampleAttachmentRepository = sampleAttachmentRepository;
        _sampleTestingImageRepository = sampleTestingImageRepository;
        _indicatorImageRepository = indicatorImageRepository;
        _testRequestRepository = testRequestRepository;
        _reportRepository = reportRepository;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Dọn dẹp các file rác không được tham chiếu trong database
    /// </summary>
    [UnitOfWork]
    public virtual async Task ExecuteAsync()
    {
        _logger.LogInformation("OrphanedFileCleanupJob: Starting orphaned file cleanup...");

        var deletedCount = 0;
        var totalSizeFreed = 0L;
        var skippedCount = 0;
        var errorCount = 0;

        try
        {
            // Load all referenced file IDs vào HashSet để check nhanh (O(1) lookup)
            var referencedFileIds = await LoadAllReferencedFileIdsAsync();
            var referencedPdfPaths = await LoadAllReferencedPdfPathsAsync();

            _logger.LogInformation(
                "Loaded {FileIdCount} referenced file IDs and {PdfCount} referenced PDF paths",
                referencedFileIds.Count,
                referencedPdfPaths.Count);

            // 1. Cleanup upload folders (images) - 2 days threshold
            var uploadCutoffDate = DateTime.Now.AddDays(-UPLOAD_FILE_AGE_THRESHOLD_DAYS);
            foreach (var folder in UPLOAD_FOLDERS)
            {
                var folderPath = Path.Combine(_environment.WebRootPath, "uploads", folder);
                var result = await CleanupFolderAsync(
                    folderPath,
                    IMAGE_EXTENSIONS,
                    uploadCutoffDate,
                    referencedFileIds,
                    referencedPdfPaths,
                    isUploadFolder: true);

                deletedCount += result.Deleted;
                totalSizeFreed += result.SizeFreed;
                skippedCount += result.Skipped;
                errorCount += result.Errors;
            }

            // 2. Cleanup temp folder (documents) - 1 day threshold
            var tempCutoffDate = DateTime.Now.AddDays(-TEMP_FILE_AGE_THRESHOLD_DAYS);
            var tempFolderPath = Path.Combine(_environment.WebRootPath, "temp");
            var tempResult = await CleanupFolderAsync(
                tempFolderPath,
                TEMP_EXTENSIONS,
                tempCutoffDate,
                referencedFileIds,
                referencedPdfPaths,
                isUploadFolder: false);

            deletedCount += tempResult.Deleted;
            totalSizeFreed += tempResult.SizeFreed;
            skippedCount += tempResult.Skipped;
            errorCount += tempResult.Errors;

            _logger.LogInformation(
                "OrphanedFileCleanupJob: Completed. Deleted: {DeletedCount} files, Freed: {SizeMB:F2} MB, Skipped: {SkippedCount}, Errors: {ErrorCount}",
                deletedCount,
                totalSizeFreed / (1024.0 * 1024.0),
                skippedCount,
                errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrphanedFileCleanupJob: Critical error during cleanup");
            throw;
        }
    }

    /// <summary>
    /// Cleanup một folder cụ thể
    /// </summary>
    private async Task<(int Deleted, long SizeFreed, int Skipped, int Errors)> CleanupFolderAsync(
        string folderPath,
        string[] allowedExtensions,
        DateTime cutoffDate,
        HashSet<string> referencedFileIds,
        HashSet<string> referencedPdfPaths,
        bool isUploadFolder)
    {
        var deleted = 0;
        var sizeFreed = 0L;
        var skipped = 0;
        var errors = 0;

        if (!Directory.Exists(folderPath))
        {
            _logger.LogDebug("Folder does not exist, skipping: {FolderPath}", folderPath);
            return (deleted, sizeFreed, skipped, errors);
        }

        _logger.LogInformation("Scanning folder: {FolderPath}", folderPath);

        // Scan recursive để tìm files trong subfolders (testing-images, indicator-images lưu theo taskId/indicatorId)
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        var processedCount = 0;

        foreach (var filePath in files)
        {
            // Batch size limit để tránh lock quá lâu
            if (processedCount >= BATCH_SIZE)
            {
                _logger.LogInformation("Reached batch size limit ({BatchSize}), will continue in next run", BATCH_SIZE);
                break;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var extension = fileInfo.Extension.ToLowerInvariant();

                // Skip if file extension not in allowed list
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogDebug("Skipping file with unsupported extension: {FilePath}", filePath);
                    skipped++;
                    continue;
                }

                // Skip if file is newer than threshold
                if (fileInfo.CreationTime > cutoffDate)
                {
                    _logger.LogDebug("Skipping recent file: {FilePath}", filePath);
                    skipped++;
                    continue;
                }

                // Check if file is referenced
                var fileName = Path.GetFileName(filePath);
                var fileId = Path.GetFileNameWithoutExtension(filePath);

                bool isReferenced;
                if (isUploadFolder)
                {
                    // Upload folder: check bằng fileId (GUID)
                    isReferenced = referencedFileIds.Contains(fileId);
                }
                else
                {
                    // Temp folder: check bằng full path hoặc relative path trong PdfPath
                    var relativePath = GetRelativePath(filePath);
                    isReferenced = referencedPdfPaths.Any(p =>
                        p != null && (p.Contains(fileName) || p.Contains(relativePath)));
                }

                if (isReferenced)
                {
                    _logger.LogDebug("File is referenced, keeping: {FilePath}", filePath);
                    skipped++;
                    continue;
                }

                // Delete orphaned file
                var size = fileInfo.Length;
                File.Delete(filePath);
                deleted++;
                sizeFreed += size;
                processedCount++;

                _logger.LogInformation("Deleted orphaned file: {FilePath} (Size: {Size} bytes)", filePath, size);
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex, "Failed to process file: {FilePath}", filePath);
            }
        }

        return (deleted, sizeFreed, skipped, errors);
    }

    /// <summary>
    /// Load tất cả file IDs được tham chiếu trong DB vào HashSet
    /// </summary>
    private async Task<HashSet<string>> LoadAllReferencedFileIdsAsync()
    {
        var fileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // TestRequestSampleAttachments
        var sampleAttachments = await _sampleAttachmentRepository.GetQueryableAsync();
        var sampleFileIds = sampleAttachments.Select(x => x.FileId).ToList();
        foreach (var id in sampleFileIds.Where(id => !string.IsNullOrEmpty(id)))
        {
            fileIds.Add(id);
        }

        // SampleTestingImages
        var testingImages = await _sampleTestingImageRepository.GetQueryableAsync();
        var testingFileIds = testingImages.Select(x => x.FileId).ToList();
        foreach (var id in testingFileIds.Where(id => !string.IsNullOrEmpty(id)))
        {
            fileIds.Add(id);
        }

        // IndicatorImages
        var indicatorImages = await _indicatorImageRepository.GetQueryableAsync();
        var indicatorFileIds = indicatorImages.Select(x => x.FileId).ToList();
        foreach (var id in indicatorFileIds.Where(id => !string.IsNullOrEmpty(id)))
        {
            fileIds.Add(id);
        }

        // TestRequests.PostalDeliveryImageFileIds (JSON array)
        var testRequests = await _testRequestRepository.GetQueryableAsync();
        var postalImageIds = testRequests
            .Where(x => x.PostalDeliveryImageFileIds != null)
            .Select(x => x.PostalDeliveryImageFileIds)
            .ToList();

        foreach (var jsonArray in postalImageIds.Where(j => !string.IsNullOrEmpty(j)))
        {
            // Parse JSON array: ["id1", "id2", ...]
            try
            {
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonArray!);
                if (ids != null)
                {
                    foreach (var id in ids.Where(id => !string.IsNullOrEmpty(id)))
                    {
                        fileIds.Add(id);
                    }
                }
            }
            catch
            {
                // Fallback: check if contains (legacy format)
                _logger.LogDebug("Failed to parse PostalDeliveryImageFileIds JSON, using fallback");
            }
        }

        return fileIds;
    }

    /// <summary>
    /// Load tất cả PDF paths được tham chiếu trong Report table
    /// </summary>
    private async Task<HashSet<string>> LoadAllReferencedPdfPathsAsync()
    {
        var pdfPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var reports = await _reportRepository.GetQueryableAsync();
        var paths = reports
            .Where(x => x.PdfPath != null)
            .Select(x => x.PdfPath)
            .ToList();

        foreach (var path in paths.Where(p => !string.IsNullOrEmpty(p)))
        {
            pdfPaths.Add(path!);
        }

        return pdfPaths;
    }

    /// <summary>
    /// Lấy relative path từ absolute path
    /// </summary>
    private string GetRelativePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(_environment.WebRootPath))
            return absolutePath;

        if (absolutePath.StartsWith(_environment.WebRootPath, StringComparison.OrdinalIgnoreCase))
        {
            return absolutePath.Substring(_environment.WebRootPath.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return absolutePath;
    }
}
