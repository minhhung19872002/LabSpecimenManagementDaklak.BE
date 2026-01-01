using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.ResultForms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/form-templates")]
public class FormTemplateController : LabSpecimenManagementController
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPdfConversionService _pdfConversionService;
    private readonly ILogger<FormTemplateController> _logger;

    public FormTemplateController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IPdfConversionService pdfConversionService,
        ILogger<FormTemplateController> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _pdfConversionService = pdfConversionService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of available form templates
    /// Filters out template files (files with .template. in the name)
    /// Only shows original files (e.g., "form.docx", not "form.template.docx")
    /// </summary>
    [HttpGet]
    public ActionResult<List<FormTemplateDto>> GetList()
    {
        var templatesPath = Path.Combine(_environment.WebRootPath ?? "", "templates");

        if (!Directory.Exists(templatesPath))
        {
            return new List<FormTemplateDto>();
        }

        var files = Directory.GetFiles(templatesPath)
            .Where(f => !IsTemplateFile(Path.GetFileName(f))) // Filter out template files
            .Select(f => new FormTemplateDto
            {
                FileName = Path.GetFileName(f),
                Name = Path.GetFileNameWithoutExtension(f)
            })
            .OrderBy(f => f.Name)
            .ToList();

        return files;
    }

    /// <summary>
    /// Checks if a file is a template file based on naming convention.
    /// Template files have the pattern: name.template.extension (e.g., "form.template.docx")
    /// Original files have the pattern: name.extension (e.g., "form.docx")
    /// </summary>
    private static bool IsTemplateFile(string fileName)
    {
        // Check for ".template." pattern in the filename (case-insensitive)
        return fileName.Contains(".template.", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Download a form template by filename
    /// </summary>
    [HttpGet("download/{*fileName}")]
    [AllowAnonymous] // OnlyOffice needs to access this without authentication
    public IActionResult Download(string fileName)
    {
        var templatesPath = Path.Combine(_environment.WebRootPath ?? "", "templates");
        var filePath = Path.Combine(templatesPath, fileName);

        // Security check: prevent directory traversal
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(templatesPath)))
        {
            throw new ValidationException("fileName", "Tên file không hợp lệ");
        }

        if (!System.IO.File.Exists(filePath))
        {
            throw new EntityNotFoundException("Template", fileName);
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// View a form template as PDF (converts DOCX to PDF on-the-fly)
    /// </summary>
    [HttpGet("view-pdf/{*fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> ViewAsPdf(string fileName)
    {
        var templatesPath = Path.Combine(_environment.WebRootPath ?? "", "templates");
        var filePath = Path.Combine(templatesPath, fileName);

        // Security check: prevent directory traversal
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(templatesPath)))
        {
            throw new ValidationException("fileName", "Tên file không hợp lệ");
        }

        if (!System.IO.File.Exists(filePath))
        {
            throw new EntityNotFoundException("Template", fileName);
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // If already PDF, return directly
        if (extension == ".pdf")
        {
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(pdfBytes, "application/pdf");
        }

        // For DOCX files, convert to PDF
        if (extension == ".docx" || extension == ".doc")
        {
            try
            {
                // Create temp directory for PDF output
                var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // Copy to temp with unique name to avoid conflicts
                var tempDocxPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{extension}");
                System.IO.File.Copy(filePath, tempDocxPath, overwrite: true);

                try
                {
                    // Convert to PDF
                    var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(tempDocxPath);
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);

                    // Cleanup temp files
                    if (System.IO.File.Exists(tempDocxPath))
                        System.IO.File.Delete(tempDocxPath);
                    if (System.IO.File.Exists(pdfPath))
                        System.IO.File.Delete(pdfPath);

                    return File(pdfBytes, "application/pdf");
                }
                finally
                {
                    // Ensure cleanup even on error
                    if (System.IO.File.Exists(tempDocxPath))
                        System.IO.File.Delete(tempDocxPath);
                }
            }
            catch (Exception ex)
            {
                throw new Common.Exceptions.InvalidOperationException($"Chuyển đổi sang PDF thất bại: {ex.Message}");
            }
        }

        // For other file types, return as-is
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var fileBytes2 = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes2, contentType, fileName);
    }

    /// <summary>
    /// Get OnlyOffice editor configuration for a template
    /// </summary>
    [HttpGet("editor-config/{*fileName}")]
    public ActionResult<OnlyOfficeEditorConfigDto> GetEditorConfig(string fileName)
    {
        var templatesPath = Path.Combine(_environment.WebRootPath ?? "", "templates");
        var filePath = Path.Combine(templatesPath, fileName);

        // Security check
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(templatesPath)))
        {
            throw new ValidationException("fileName", "Tên file không hợp lệ");
        }

        if (!System.IO.File.Exists(filePath))
        {
            throw new EntityNotFoundException("Template", fileName);
        }

        // Use BackendUrlForDocker for URLs that OnlyOffice (in Docker) needs to access
        var backendUrl = _configuration["OnlyOffice:BackendUrlForDocker"] ?? _configuration["App:SelfUrl"] ?? "https://localhost:44381";
        var onlyOfficeUrl = _configuration["OnlyOffice:DocumentServerUrl"] ?? "http://localhost:8080";
        // Generate safe document key (only alphanumeric characters)
        // OnlyOffice has issues with special characters in document keys
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fileName));
        var fileHash = Convert.ToHexString(hashBytes)[..16];
        var documentKey = $"{fileHash}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var documentType = extension switch
        {
            ".doc" or ".docx" or ".odt" or ".rtf" or ".txt" => "word",
            ".xls" or ".xlsx" or ".ods" or ".csv" => "cell",
            ".ppt" or ".pptx" or ".odp" => "slide",
            _ => "word"
        };

        var onlyOfficeConfig = new OnlyOfficeConfig
        {
            Document = new OnlyOfficeDocument
            {
                FileType = extension.TrimStart('.'),
                Key = documentKey,
                Title = fileName,
                Url = $"{backendUrl}/api/v1/form-templates/download/{Uri.EscapeDataString(fileName)}"
            },
            DocumentType = documentType,
            EditorConfig = new OnlyOfficeEditorSettings
            {
                // Temporarily remove callback to allow editing without callback verification
                // CallbackUrl = $"{backendUrl}/api/v1/form-templates/callback/{Uri.EscapeDataString(fileName)}",
                CallbackUrl = "",
                Lang = "vi",
                Mode = "edit",
                User = new OnlyOfficeUser
                {
                    Id = "1",
                    Name = "Editor"
                },
                Customization = new OnlyOfficeCustomization
                {
                    Autosave = true,
                    Chat = false,
                    Comments = false,
                    CompactHeader = true,
                    CompactToolbar = false,
                    Feedback = false,
                    ForceSave = false,
                    Help = false,
                    HideRightMenu = false,
                    HideRulers = false,
                    IntegrationMode = "embed",
                    Logo = null,
                    MacrosMode = "disable",
                    MentionShare = false,
                    Plugins = false,
                    ReviewDisplay = "original",
                    ToolbarHideFileName = false,
                    ToolbarNoTabs = false,
                    Uiicons = true,
                    Unit = "cm",
                    Zoom = 100
                }
            },
            Height = "100%",
            Width = "100%",
            Type = "desktop"
        };

        // Generate JWT token for OnlyOffice
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];
        string? token = null;
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            token = GenerateOnlyOfficeJwtToken(onlyOfficeConfig, jwtSecret);
            onlyOfficeConfig.Token = token;
        }

        var config = new OnlyOfficeEditorConfigDto
        {
            DocumentServerUrl = onlyOfficeUrl,
            Config = onlyOfficeConfig
        };

        return config;
    }

    /// <summary>
    /// OnlyOffice callback endpoint for saving documents
    /// </summary>
    [HttpPost("callback/{*fileName}")]
    [AllowAnonymous] // OnlyOffice callback doesn't have authentication
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnlyOfficeCallback(string fileName, [FromBody] OnlyOfficeCallbackDto callback)
    {
        // Status codes:
        // 0 - no document with the key identifier
        // 1 - document is being edited
        // 2 - document is ready for saving
        // 3 - document saving error
        // 4 - document is closed with no changes
        // 6 - document is being edited, but current state saved
        // 7 - error while force saving

        _logger.LogInformation("OnlyOffice callback received - Status: {Status}, FileName: {FileName}", callback.Status, fileName);

        // Always respond quickly with success for status 1 (editing) and 4 (closed without changes)
        if (callback.Status == 1 || callback.Status == 4)
        {
            return Ok(new { error = 0 });
        }

        if (callback.Status == 2 || callback.Status == 6)
        {
            // Document is ready for saving or force save
            try
            {
                var templatesPath = Path.Combine(_environment.WebRootPath ?? "", "templates");
                var filePath = Path.Combine(templatesPath, fileName);

                // Security check
                var fullPath = Path.GetFullPath(filePath);
                if (!fullPath.StartsWith(Path.GetFullPath(templatesPath)))
                {
                    _logger.LogWarning("OnlyOffice callback - Security check failed for: {FileName}", fileName);
                    return Ok(new { error = 1 });
                }

                // Download the document from OnlyOffice
                if (!string.IsNullOrEmpty(callback.Url))
                {
                    _logger.LogInformation("OnlyOffice callback - Downloading document from: {Url}", callback.Url);
                    var httpClient = _httpClientFactory.CreateClient();
                    var documentBytes = await httpClient.GetByteArrayAsync(callback.Url);
                    await System.IO.File.WriteAllBytesAsync(filePath, documentBytes);
                    _logger.LogInformation("OnlyOffice callback - Document saved successfully: {FileName}", fileName);
                }

                return Ok(new { error = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnlyOffice callback error");
                return Ok(new { error = 1 });
            }
        }

        // For all other status codes, respond with success
        return Ok(new { error = 0 });
    }

    /// <summary>
    /// Generate JWT token for OnlyOffice Document Server
    /// </summary>
    private string GenerateOnlyOfficeJwtToken(OnlyOfficeConfig config, string secret)
    {
        // OnlyOffice requires the payload to match the config structure EXACTLY
        // The JWT payload must contain the same structure that is sent to DocsAPI.DocEditor
        var payload = new
        {
            document = new
            {
                fileType = config.Document.FileType,
                key = config.Document.Key,
                title = config.Document.Title,
                url = config.Document.Url,
                permissions = new
                {
                    comment = true,
                    copy = true,
                    download = true,
                    edit = true,
                    fillForms = true,
                    modifyContentControl = true,
                    modifyFilter = true,
                    print = true,
                    review = true
                }
            },
            documentType = config.DocumentType,
            editorConfig = new
            {
                callbackUrl = config.EditorConfig.CallbackUrl,
                lang = config.EditorConfig.Lang,
                mode = config.EditorConfig.Mode,
                user = new
                {
                    id = config.EditorConfig.User?.Id ?? "1",
                    name = config.EditorConfig.User?.Name ?? "Editor"
                },
                customization = new
                {
                    autosave = config.EditorConfig.Customization.Autosave,
                    chat = config.EditorConfig.Customization.Chat,
                    comments = config.EditorConfig.Customization.Comments,
                    compactHeader = config.EditorConfig.Customization.CompactHeader,
                    compactToolbar = config.EditorConfig.Customization.CompactToolbar,
                    feedback = config.EditorConfig.Customization.Feedback,
                    forcesave = config.EditorConfig.Customization.ForceSave,
                    help = config.EditorConfig.Customization.Help,
                    hideRightMenu = config.EditorConfig.Customization.HideRightMenu,
                    hideRulers = config.EditorConfig.Customization.HideRulers,
                    integrationMode = config.EditorConfig.Customization.IntegrationMode,
                    macrosMode = config.EditorConfig.Customization.MacrosMode,
                    mentionShare = config.EditorConfig.Customization.MentionShare,
                    plugins = config.EditorConfig.Customization.Plugins,
                    reviewDisplay = config.EditorConfig.Customization.ReviewDisplay,
                    toolbarHideFileName = config.EditorConfig.Customization.ToolbarHideFileName,
                    toolbarNoTabs = config.EditorConfig.Customization.ToolbarNoTabs,
                    unit = config.EditorConfig.Customization.Unit,
                    zoom = config.EditorConfig.Customization.Zoom
                }
            },
            height = config.Height,
            width = config.Width,
            type = config.Type
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Use manual JWT creation for proper JSON serialization
        var header = new { alg = "HS256", typ = "JWT" };
        var headerJson = JsonSerializer.Serialize(header, options);
        var payloadJson = JsonSerializer.Serialize(payload, options);

        var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var signatureInput = $"{headerBase64}.{payloadBase64}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureInput));
        var signatureBase64 = Base64UrlEncode(signatureBytes);

        return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        output = output.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return output;
    }
}
