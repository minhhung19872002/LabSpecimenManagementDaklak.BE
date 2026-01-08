using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using LabSpecimenManagement.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Service for converting documents to PDF using OnlyOffice Document Server
/// OnlyOffice provides better rendering for complex Word documents including vertical alignment in merged cells
///
/// OnlyOffice Conversion API requires:
/// 1. File must be accessible via URL (OnlyOffice downloads it)
/// 2. POST request to /ConvertService.ashx with XML or JSON body
/// 3. Response contains URL to download converted file
/// </summary>
public class OnlyOfficePdfConversionService : IPdfConversionService, ITransientDependency
{
    private readonly ILogger<OnlyOfficePdfConversionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _onlyOfficeUrl;
    private readonly string _backendUrl;

    public OnlyOfficePdfConversionService(
        ILogger<OnlyOfficePdfConversionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("OnlyOffice");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        // Get OnlyOffice URL from config
        _onlyOfficeUrl = configuration["OnlyOffice:Url"] ?? "http://localhost:8088";
        // Backend URL that OnlyOffice can access to download files
        _backendUrl = configuration["OnlyOffice:CallbackUrl"] ?? "http://host.docker.internal:44381";
    }

    /// <summary>
    /// Check if OnlyOffice service is available
    /// </summary>
    public bool IsLibreOfficeAvailable()
    {
        try
        {
            var response = _httpClient.GetAsync($"{_onlyOfficeUrl}/healthcheck").Result;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OnlyOffice service is not available at {Url}", _onlyOfficeUrl);
            return false;
        }
    }

    /// <summary>
    /// Convert DOCX to PDF using OnlyOffice Conversion API
    /// </summary>
    public async Task<string> ConvertDocxToPdfAsync(string inputDocxPath, string? outputPdfPath = null)
    {
        if (!File.Exists(inputDocxPath))
        {
            throw new EntityNotFoundException("File", inputDocxPath);
        }

        var inputDir = Path.GetDirectoryName(inputDocxPath)
            ?? throw new Exceptions.InvalidOperationException("Không thể xác định thư mục input");
        var inputFileName = Path.GetFileNameWithoutExtension(inputDocxPath);
        var docxFileName = Path.GetFileName(inputDocxPath);

        // Output path
        var pdfPath = outputPdfPath ?? Path.Combine(inputDir, $"{inputFileName}.pdf");

        _logger.LogInformation("Converting DOCX to PDF using OnlyOffice: {Input} -> {Output}", inputDocxPath, pdfPath);

        try
        {
            // Build the URL where OnlyOffice can download the file
            // Using the ResultFormController endpoint: /api/v1/result-forms/download/{fileName}
            var fileUrl = $"{_backendUrl}/api/v1/result-forms/download/{Uri.EscapeDataString(docxFileName)}";

            _logger.LogInformation("OnlyOffice will download file from: {Url}", fileUrl);

            // Call OnlyOffice Conversion API
            var pdfBytes = await CallConversionApiAsync(fileUrl, docxFileName);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw new Exceptions.InvalidOperationException("OnlyOffice trả về file PDF rỗng");
            }

            // Save PDF to file
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            _logger.LogInformation("PDF generated successfully: {Path} ({Size} bytes)", pdfPath, pdfBytes.Length);
            return pdfPath;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to OnlyOffice service at {Url}", _onlyOfficeUrl);
            throw new Exceptions.InvalidOperationException("Không thể kết nối đến OnlyOffice. Vui lòng kiểm tra service đang chạy.");
        }
        catch (Exception ex) when (ex is not BusinessException)
        {
            _logger.LogError(ex, "Error converting DOCX to PDF using OnlyOffice");
            throw new Exceptions.InvalidOperationException($"Lỗi khi chuyển đổi sang PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Call OnlyOffice Conversion API
    /// Documentation: https://api.onlyoffice.com/editors/conversionapi
    /// </summary>
    private async Task<byte[]> CallConversionApiAsync(string fileUrl, string fileName)
    {
        var conversionUrl = $"{_onlyOfficeUrl}/ConvertService.ashx";

        // Create unique key for this conversion
        var key = Guid.NewGuid().ToString("N");

        // OnlyOffice accepts both JSON and XML format
        // Using JSON format
        var requestBody = new
        {
            async = false,  // Synchronous conversion
            filetype = "docx",
            key = key,
            outputtype = "pdf",
            title = fileName,
            url = fileUrl
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        _logger.LogInformation("OnlyOffice request URL: {Url}", conversionUrl);
        _logger.LogInformation("OnlyOffice request body: {Request}", JsonSerializer.Serialize(requestBody));

        var response = await _httpClient.PostAsync(conversionUrl, jsonContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("OnlyOffice response status: {StatusCode}", response.StatusCode);
        _logger.LogInformation("OnlyOffice response body: {Response}", responseContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OnlyOffice conversion failed: {StatusCode} - {Error}", response.StatusCode, responseContent);
            throw new Exceptions.InvalidOperationException($"OnlyOffice conversion failed: {response.StatusCode}");
        }

        // OnlyOffice returns XML or JSON response depending on request
        // Try parsing as JSON first
        try
        {
            return await ParseJsonResponseAsync(responseContent);
        }
        catch
        {
            // Try XML format
            return await ParseXmlResponseAsync(responseContent);
        }
    }

    /// <summary>
    /// Parse JSON response from OnlyOffice
    /// </summary>
    private async Task<byte[]> ParseJsonResponseAsync(string responseContent)
    {
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        // Check for error
        if (root.TryGetProperty("error", out var errorProp))
        {
            var errorCode = errorProp.GetInt32();
            if (errorCode != 0)
            {
                var errorMessage = GetErrorMessage(errorCode);
                _logger.LogError("OnlyOffice conversion error {Code}: {Message}", errorCode, errorMessage);
                throw new Exceptions.InvalidOperationException($"OnlyOffice error: {errorMessage}");
            }
        }

        // Get file URL from response
        if (root.TryGetProperty("fileUrl", out var fileUrlProp))
        {
            var pdfUrl = fileUrlProp.GetString();
            if (!string.IsNullOrEmpty(pdfUrl))
            {
                _logger.LogInformation("Downloading converted PDF from: {Url}", pdfUrl);
                return await _httpClient.GetByteArrayAsync(pdfUrl);
            }
        }

        throw new Exceptions.InvalidOperationException("OnlyOffice response không chứa fileUrl");
    }

    /// <summary>
    /// Parse XML response from OnlyOffice
    /// </summary>
    private async Task<byte[]> ParseXmlResponseAsync(string responseContent)
    {
        var doc = XDocument.Parse(responseContent);
        var root = doc.Root;

        if (root == null)
        {
            throw new Exceptions.InvalidOperationException("Invalid XML response from OnlyOffice");
        }

        // Check for error
        var errorElement = root.Element("Error");
        if (errorElement != null)
        {
            var errorCode = int.Parse(errorElement.Value);
            if (errorCode != 0)
            {
                var errorMessage = GetErrorMessage(errorCode);
                throw new Exceptions.InvalidOperationException($"OnlyOffice error: {errorMessage}");
            }
        }

        // Get file URL
        var fileUrlElement = root.Element("FileUrl");
        if (fileUrlElement != null && !string.IsNullOrEmpty(fileUrlElement.Value))
        {
            _logger.LogInformation("Downloading converted PDF from: {Url}", fileUrlElement.Value);
            return await _httpClient.GetByteArrayAsync(fileUrlElement.Value);
        }

        throw new Exceptions.InvalidOperationException("OnlyOffice XML response không chứa FileUrl");
    }

    /// <summary>
    /// Get error message from OnlyOffice error code
    /// https://api.onlyoffice.com/editors/conversionapi#error
    /// </summary>
    private static string GetErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            -1 => "Unknown error",
            -2 => "Conversion timeout",
            -3 => "Conversion error",
            -4 => "Error while downloading the document file to be converted",
            -5 => "Incorrect password",
            -6 => "Error while accessing the conversion result database",
            -7 => "Input error",
            -8 => "Invalid token",
            _ => $"Unknown error code: {errorCode}"
        };
    }
}
