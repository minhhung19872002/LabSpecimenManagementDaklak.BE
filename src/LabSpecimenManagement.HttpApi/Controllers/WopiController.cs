using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// WOPI (Web Application Open Platform Interface) endpoints for Collabora Online integration
/// Implements the WOPI protocol to allow Collabora to read/write documents
/// Note: This controller does NOT use ABP's response wrapper - WOPI protocol requires raw JSON responses
/// </summary>
[Route("wopi")]
[ApiController]
[IgnoreAntiforgeryToken] // WOPI requests don't have CSRF tokens
public class WopiController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WopiController> _logger;

    public WopiController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<WopiController> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// WOPI CheckFileInfo - Returns information about a file
    /// GET /wopi/files/{fileId}
    /// </summary>
    [HttpGet("files/{fileId}")]
    [AllowAnonymous] // WOPI requests come from Collabora server
    public IActionResult CheckFileInfo(string fileId)
    {
        _logger.LogInformation("[WOPI CheckFileInfo] fileId: {FileId}", fileId);

        var fileName = DecodeFileId(fileId);
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        var filePath = Path.Combine(tempDir, fileName);

        // Security check
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(tempDir)))
        {
            _logger.LogWarning("[WOPI CheckFileInfo] Security check failed for: {FileName}", fileName);
            return NotFound();
        }

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("[WOPI CheckFileInfo] File not found: {FilePath}", filePath);
            return NotFound();
        }

        var fileInfo = new FileInfo(filePath);

        // WOPI CheckFileInfo response
        // https://learn.microsoft.com/en-us/microsoft-365/cloud-storage-partner-program/rest/files/checkfileinfo
        var response = new
        {
            // Required properties
            BaseFileName = fileName,
            OwnerId = "system",
            Size = fileInfo.Length,
            UserId = "anonymous",
            Version = fileInfo.LastWriteTimeUtc.Ticks.ToString(),

            // User permissions
            UserCanWrite = true,
            UserCanNotWriteRelative = true,

            // File properties
            ReadOnly = false,
            UserFriendlyName = "User",

            // Optional useful properties
            LastModifiedTime = fileInfo.LastWriteTimeUtc.ToString("O"),
            SupportsUpdate = true,
            SupportsLocks = false,
            SupportsGetLock = false,
            SupportsExtendedLockLength = false,
            SupportsFolders = false,
            SupportsDeleteFile = false,
            SupportsRename = false,

            // Print is handled by Collabora's built-in print dialog
            DisablePrint = false,
            DisableExport = false,
            DisableCopy = false,

            // Enable editing features
            UserCanRename = false,
            SupportsUserInfo = false,

            // PostMessageOrigin for iframe communication - allow all origins in development
            // In production, this should match the frontend origin
            PostMessageOrigin = "*"
        };

        _logger.LogInformation("[WOPI CheckFileInfo] Response: BaseFileName={BaseFileName}, Size={Size}",
            response.BaseFileName, response.Size);

        // Return with PascalCase property names as required by WOPI protocol
        return new JsonResult(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // Use original property names (PascalCase)
        });
    }

    /// <summary>
    /// WOPI GetFile - Returns the binary contents of a file
    /// GET /wopi/files/{fileId}/contents
    /// </summary>
    [HttpGet("files/{fileId}/contents")]
    [AllowAnonymous] // WOPI requests come from Collabora server
    public IActionResult GetFile(string fileId)
    {
        _logger.LogInformation("[WOPI GetFile] fileId: {FileId}", fileId);

        var fileName = DecodeFileId(fileId);
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        var filePath = Path.Combine(tempDir, fileName);

        // Security check
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(tempDir)))
        {
            _logger.LogWarning("[WOPI GetFile] Security check failed for: {FileName}", fileName);
            return NotFound();
        }

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("[WOPI GetFile] File not found: {FilePath}", filePath);
            return NotFound();
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        _logger.LogInformation("[WOPI GetFile] Serving file: {FileName}, Size: {Size} bytes", fileName, fileBytes.Length);

        return File(fileBytes, "application/octet-stream");
    }

    /// <summary>
    /// WOPI PutFile - Updates the binary contents of a file
    /// POST /wopi/files/{fileId}/contents
    /// </summary>
    [HttpPost("files/{fileId}/contents")]
    [AllowAnonymous] // WOPI requests come from Collabora server
    public async Task<IActionResult> PutFile(string fileId)
    {
        _logger.LogInformation("[WOPI PutFile] fileId: {FileId}", fileId);

        var fileName = DecodeFileId(fileId);
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        var filePath = Path.Combine(tempDir, fileName);

        // Security check
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(tempDir)))
        {
            _logger.LogWarning("[WOPI PutFile] Security check failed for: {FileName}", fileName);
            return NotFound();
        }

        // Read the file content from request body
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        _logger.LogInformation("[WOPI PutFile] Received {Size} bytes for file: {FileName}", fileBytes.Length, fileName);

        // Save the file
        await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

        var fileInfo = new FileInfo(filePath);

        // Return updated file info
        var response = new
        {
            LastModifiedTime = fileInfo.LastWriteTimeUtc.ToString("O"),
            ItemVersion = fileInfo.LastWriteTimeUtc.Ticks.ToString()
        };

        _logger.LogInformation("[WOPI PutFile] File saved successfully: {FileName}", fileName);

        // Return with PascalCase property names as required by WOPI protocol
        return new JsonResult(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // Use original property names (PascalCase)
        });
    }

    /// <summary>
    /// Get Collabora editor URL for a file
    /// </summary>
    [HttpGet("editor-url/{fileName}")]
    [Authorize]
    public IActionResult GetEditorUrl(string fileName)
    {
        _logger.LogInformation("[WOPI GetEditorUrl] fileName: {FileName}", fileName);

        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        var filePath = Path.Combine(tempDir, fileName);

        // Security check
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(tempDir)))
        {
            return BadRequest(new { error = "Tên file không hợp lệ" });
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "File không tồn tại" });
        }

        var collaboraServerUrl = _configuration["Collabora:ServerUrl"] ?? "http://localhost:9980";
        var wopiBaseUrl = _configuration["Collabora:WopiBaseUrl"] ?? _configuration["App:SelfUrl"] ?? "http://localhost:44301";

        // Get discovery.xml to find the correct editor URL
        var extension = Path.GetExtension(fileName).ToLowerInvariant().TrimStart('.');
        var editorAction = GetEditorAction(extension);

        // Encode file name for WOPI
        var fileId = EncodeFileId(fileName);
        var wopiSrc = $"{wopiBaseUrl}/wopi/files/{fileId}";

        // Build Collabora editor URL
        // Format: https://collabora-server/browser/{version}/cool.html?WOPISrc={wopi_url}
        var editorUrl = $"{collaboraServerUrl}/browser/dist/cool.html?WOPISrc={Uri.EscapeDataString(wopiSrc)}";

        _logger.LogInformation("[WOPI GetEditorUrl] EditorUrl: {EditorUrl}", editorUrl);

        var response = new CollaboraEditorConfigDto
        {
            EditorUrl = editorUrl,
            WopiSrc = wopiSrc,
            FileName = fileName,
            FileExtension = extension
        };

        return Ok(response);
    }

    /// <summary>
    /// Encode file name to be used as WOPI file ID (URL-safe)
    /// </summary>
    private static string EncodeFileId(string fileName)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(fileName);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Decode WOPI file ID back to file name
    /// </summary>
    private static string DecodeFileId(string fileId)
    {
        // Add padding if needed
        var padded = fileId.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        var bytes = Convert.FromBase64String(padded);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Get the editor action type based on file extension
    /// </summary>
    private static string GetEditorAction(string extension)
    {
        return extension switch
        {
            "doc" or "docx" or "odt" or "rtf" => "edit",
            "xls" or "xlsx" or "ods" => "edit",
            "ppt" or "pptx" or "odp" => "edit",
            "pdf" => "view",
            _ => "edit"
        };
    }
}

/// <summary>
/// DTO for Collabora editor configuration
/// </summary>
public class CollaboraEditorConfigDto
{
    public string EditorUrl { get; set; } = string.Empty;
    public string WopiSrc { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
}
