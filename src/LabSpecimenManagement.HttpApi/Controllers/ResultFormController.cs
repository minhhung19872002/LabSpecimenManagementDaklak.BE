using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.ResultForms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/result-forms")]
public class ResultFormController : LabSpecimenManagementController
{
    private readonly IResultFormAppService _resultFormAppService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ResultFormController(
        IResultFormAppService resultFormAppService,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _resultFormAppService = resultFormAppService;
        _environment = environment;
        _configuration = configuration;
    }

    /// <summary>
    /// Generate result form document from template (BM03)
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateResultFormOutput>> GenerateResultForm([FromBody] GenerateResultFormInput input)
    {
        var result = await _resultFormAppService.GenerateResultFormAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// Get samples for result form selection with testing status
    /// </summary>
    [HttpGet("samples-for-selection/{testRequestId}")]
    public async Task<ActionResult<List<SampleForSelectionDto>>> GetSamplesForSelection(Guid testRequestId)
    {
        var result = await _resultFormAppService.GetSamplesForResultFormAsync(testRequestId);
        return Ok(result);
    }

    /// <summary>
    /// Generate BM01 result form document for single sample
    /// </summary>
    [HttpPost("generate-bm01")]
    public async Task<ActionResult<GenerateResultFormOutput>> GenerateBM01ResultForm([FromBody] GenerateBM01ResultFormInput input)
    {
        var result = await _resultFormAppService.GenerateBM01ResultFormAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// Generate test request form document (BM05 - phiếu yêu cầu XN)
    /// </summary>
    [HttpPost("generate-test-request")]
    public async Task<ActionResult<GenerateTestRequestFormOutput>> GenerateTestRequestForm([FromBody] GenerateTestRequestFormInput input)
    {
        var result = await _resultFormAppService.GenerateTestRequestFormAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// Download generated result form
    /// </summary>
    [HttpGet("download/{fileName}")]
    [AllowAnonymous] // OnlyOffice needs to access this without authentication
    public IActionResult Download(string fileName)
    {
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        var filePath = Path.Combine(tempDir, fileName);

        // Security check: prevent directory traversal
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(tempDir)))
        {
            return BadRequest("Invalid file name");
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found");
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
    /// Get OnlyOffice editor configuration for result form
    /// </summary>
    [HttpGet("editor-config/{fileName}")]
    public ActionResult<OnlyOfficeEditorConfigDto> GetEditorConfig(string fileName)
    {
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        var filePath = Path.Combine(tempDir, fileName);

        // Security check
        var fullPath = Path.GetFullPath(filePath);
        if (!fullPath.StartsWith(Path.GetFullPath(tempDir)))
        {
            return BadRequest("Invalid file name");
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found");
        }

        // Use BackendUrlForDocker for URLs that OnlyOffice (in Docker) needs to access
        var backendUrl = _configuration["OnlyOffice:BackendUrlForDocker"] ?? _configuration["App:SelfUrl"] ?? "https://localhost:44381";
        var onlyOfficeUrl = _configuration["OnlyOffice:DocumentServerUrl"] ?? "http://localhost:8080";

        // Generate safe document key
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fileName));
        var fileHash = Convert.ToHexString(hashBytes)[..16];
        var documentKey = $"{fileHash}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        var onlyOfficeConfig = new OnlyOfficeConfig
        {
            Document = new OnlyOfficeDocument
            {
                FileType = extension.TrimStart('.'),
                Key = documentKey,
                Title = fileName,
                Url = $"{backendUrl}/api/v1/result-forms/download/{Uri.EscapeDataString(fileName)}"
            },
            DocumentType = "word",
            EditorConfig = new OnlyOfficeEditorSettings
            {
                CallbackUrl = "",
                Lang = "vi",
                Mode = "edit",
                User = new OnlyOfficeUser
                {
                    Id = CurrentUser.Id?.ToString() ?? "1",
                    Name = CurrentUser.Name ?? "Editor"
                },
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
        };

        // Generate JWT token for OnlyOffice
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            var token = GenerateOnlyOfficeJwtToken(onlyOfficeConfig, jwtSecret);
            onlyOfficeConfig.Token = token;
        }

        var config = new OnlyOfficeEditorConfigDto
        {
            DocumentServerUrl = onlyOfficeUrl,
            Config = onlyOfficeConfig
        };

        return config;
    }

    private string GenerateOnlyOfficeJwtToken(OnlyOfficeConfig config, string secret)
    {
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
