using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// Configuration endpoints for frontend
/// </summary>
[RemoteService]
[Route("api/v1/config")]
public class ConfigController : LabSpecimenManagementController
{
    private readonly IConfiguration _configuration;

    public ConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get document editor provider configuration
    /// </summary>
    [HttpGet("document-editor")]
    [AllowAnonymous]
    public ActionResult<DocumentEditorConfigDto> GetDocumentEditorConfig()
    {
        var provider = _configuration["DocumentEditor:Provider"] ?? "OnlyOffice";

        return new DocumentEditorConfigDto
        {
            Provider = provider
        };
    }
}

/// <summary>
/// Document editor configuration DTO
/// </summary>
public class DocumentEditorConfigDto
{
    /// <summary>
    /// Document editor provider: "OnlyOffice" or "Collabora"
    /// </summary>
    public string Provider { get; set; } = "OnlyOffice";
}
