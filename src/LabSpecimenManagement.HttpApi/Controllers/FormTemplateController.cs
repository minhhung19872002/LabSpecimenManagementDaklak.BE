using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/form-templates")]
public class FormTemplateController : LabSpecimenManagementController
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public FormTemplateController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _environment = environment;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Get list of available form templates
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
    /// Download a form template by filename
    /// </summary>
    [HttpGet("download/{*fileName}")]
    public IActionResult Download(string fileName)
    {
        var templatesPath = Path.Combine(_environment.WebRootPath ?? "", "templates");
        var filePath = Path.Combine(templatesPath, fileName);

        // Security check: prevent directory traversal
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(templatesPath)))
        {
            return BadRequest("Invalid file name");
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Template not found");
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
            return BadRequest("Invalid file name");
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Template not found");
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

        var config = new OnlyOfficeEditorConfigDto
        {
            DocumentServerUrl = onlyOfficeUrl,
            Config = new OnlyOfficeConfig
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
                    CallbackUrl = $"{backendUrl}/api/v1/form-templates/callback/{Uri.EscapeDataString(fileName)}",
                    Lang = "vi",
                    Mode = "edit",
                    Customization = new OnlyOfficeCustomization
                    {
                        Autosave = false,
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
            }
        };

        return config;
    }

    /// <summary>
    /// OnlyOffice callback endpoint for saving documents
    /// </summary>
    [HttpPost("callback/{*fileName}")]
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

        Console.WriteLine($"OnlyOffice callback received - Status: {callback.Status}, FileName: {fileName}");

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
                    Console.WriteLine($"OnlyOffice callback - Security check failed for: {fileName}");
                    return Ok(new { error = 1 });
                }

                // Download the document from OnlyOffice
                if (!string.IsNullOrEmpty(callback.Url))
                {
                    Console.WriteLine($"OnlyOffice callback - Downloading document from: {callback.Url}");
                    var httpClient = _httpClientFactory.CreateClient();
                    var documentBytes = await httpClient.GetByteArrayAsync(callback.Url);
                    await System.IO.File.WriteAllBytesAsync(filePath, documentBytes);
                    Console.WriteLine($"OnlyOffice callback - Document saved successfully: {fileName}");
                }

                return Ok(new { error = 0 });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnlyOffice callback error: {ex.Message}");
                return Ok(new { error = 1 });
            }
        }

        // For all other status codes, respond with success
        return Ok(new { error = 0 });
    }
}

public class FormTemplateDto
{
    public string FileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class OnlyOfficeEditorConfigDto
{
    public string DocumentServerUrl { get; set; } = string.Empty;
    public OnlyOfficeConfig Config { get; set; } = new();
}

public class OnlyOfficeConfig
{
    public OnlyOfficeDocument Document { get; set; } = new();
    public string DocumentType { get; set; } = "word";
    public OnlyOfficeEditorSettings EditorConfig { get; set; } = new();
    public string Height { get; set; } = "100%";
    public string Width { get; set; } = "100%";
    public string Type { get; set; } = "desktop";
}

public class OnlyOfficeDocument
{
    public string FileType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class OnlyOfficeEditorSettings
{
    public string CallbackUrl { get; set; } = string.Empty;
    public string Lang { get; set; } = "vi";
    public string Mode { get; set; } = "edit";
    public OnlyOfficeCustomization Customization { get; set; } = new();
}

public class OnlyOfficeCustomization
{
    public bool Autosave { get; set; }
    public bool Chat { get; set; }
    public bool Comments { get; set; }
    public bool CompactHeader { get; set; }
    public bool CompactToolbar { get; set; }
    public bool Feedback { get; set; }
    public bool ForceSave { get; set; }
    public bool Help { get; set; }
    public bool HideRightMenu { get; set; }
    public bool HideRulers { get; set; }
    public string IntegrationMode { get; set; } = "embed";
    public object? Logo { get; set; }
    public string MacrosMode { get; set; } = "disable";
    public bool MentionShare { get; set; }
    public bool Plugins { get; set; }
    public string ReviewDisplay { get; set; } = "original";
    public bool ToolbarHideFileName { get; set; }
    public bool ToolbarNoTabs { get; set; }
    public bool Uiicons { get; set; }
    public string Unit { get; set; } = "cm";
    public int Zoom { get; set; } = 100;
}

public class OnlyOfficeCallbackDto
{
    public List<string>? Actions { get; set; }
    public string? ChangesUrl { get; set; }
    public object? ForceSaveType { get; set; }
    public object? History { get; set; }
    public string? Key { get; set; }
    public int Status { get; set; }
    public string? Url { get; set; }
    public List<string>? Users { get; set; }
    public string? UserData { get; set; }
}
