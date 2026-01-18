using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// DTO for form template information
/// </summary>
public class FormTemplateDto
{
    public string FileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO for OnlyOffice editor configuration
/// </summary>
public class OnlyOfficeEditorConfigDto
{
    [JsonPropertyName("documentServerUrl")]
    public string DocumentServerUrl { get; set; } = string.Empty;

    [JsonPropertyName("config")]
    public OnlyOfficeConfig Config { get; set; } = new();
}

public class OnlyOfficeConfig
{
    [JsonPropertyName("document")]
    public OnlyOfficeDocument Document { get; set; } = new();

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = "word";

    [JsonPropertyName("editorConfig")]
    public OnlyOfficeEditorSettings EditorConfig { get; set; } = new();

    [JsonPropertyName("height")]
    public string Height { get; set; } = "100%";

    [JsonPropertyName("width")]
    public string Width { get; set; } = "100%";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "desktop";

    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

public class OnlyOfficeDocument
{
    [JsonPropertyName("fileType")]
    public string FileType { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public OnlyOfficePermissions Permissions { get; set; } = new();
}

public class OnlyOfficePermissions
{
    [JsonPropertyName("comment")]
    public bool Comment { get; set; } = true;

    [JsonPropertyName("copy")]
    public bool Copy { get; set; } = true;

    [JsonPropertyName("download")]
    public bool Download { get; set; } = true;

    [JsonPropertyName("edit")]
    public bool Edit { get; set; } = true;

    [JsonPropertyName("fillForms")]
    public bool FillForms { get; set; } = true;

    [JsonPropertyName("modifyContentControl")]
    public bool ModifyContentControl { get; set; } = true;

    [JsonPropertyName("modifyFilter")]
    public bool ModifyFilter { get; set; } = true;

    [JsonPropertyName("print")]
    public bool Print { get; set; } = true;

    [JsonPropertyName("review")]
    public bool Review { get; set; } = true;
}

public class OnlyOfficeEditorSettings
{
    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; set; } = string.Empty;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "vi";

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "edit";

    [JsonPropertyName("user")]
    public OnlyOfficeUser? User { get; set; }

    [JsonPropertyName("customization")]
    public OnlyOfficeCustomization Customization { get; set; } = new();
}

public class OnlyOfficeUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "1";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Editor";
}

public class OnlyOfficeCustomization
{
    [JsonPropertyName("autosave")]
    public bool Autosave { get; set; }

    [JsonPropertyName("forcesave")]
    public bool ForceSave { get; set; }

    [JsonPropertyName("chat")]
    public bool Chat { get; set; }

    [JsonPropertyName("comments")]
    public bool Comments { get; set; }

    [JsonPropertyName("compactHeader")]
    public bool CompactHeader { get; set; }

    [JsonPropertyName("feedback")]
    public bool Feedback { get; set; }

    [JsonPropertyName("help")]
    public bool Help { get; set; }

    [JsonPropertyName("goback")]
    public bool Goback { get; set; }
}

/// <summary>
/// DTO for OnlyOffice callback
/// </summary>
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
