using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Service for converting documents to PDF using Collabora Online
/// Uses the /cool/convert-to endpoint for direct conversion without opening editor
/// </summary>
public class CollaboraPdfConversionService : IPdfConversionService, ITransientDependency
{
    private readonly ILogger<CollaboraPdfConversionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _collaboraUrl;

    public CollaboraPdfConversionService(
        ILogger<CollaboraPdfConversionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Collabora");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        // Get Collabora URL from config (default to standard Collabora port)
        _collaboraUrl = configuration["Collabora:Url"] ?? "http://localhost:9980";
    }

    /// <summary>
    /// Check if Collabora service is available
    /// </summary>
    public bool IsLibreOfficeAvailable()
    {
        try
        {
            // Collabora health check endpoint
            var response = _httpClient.GetAsync($"{_collaboraUrl}/hosting/discovery").Result;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Collabora service is not available at {Url}", _collaboraUrl);
            return false;
        }
    }

    /// <summary>
    /// Convert DOCX to PDF using Collabora /cool/convert-to API
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

        // Output path
        var pdfPath = outputPdfPath ?? Path.Combine(inputDir, $"{inputFileName}.pdf");

        _logger.LogInformation("Converting DOCX to PDF using Collabora: {Input} -> {Output}", inputDocxPath, pdfPath);

        try
        {
            // Read file bytes
            var fileBytes = await File.ReadAllBytesAsync(inputDocxPath);

            // Call Collabora convert-to API
            var pdfBytes = await CallConvertToApiAsync(fileBytes, Path.GetFileName(inputDocxPath));

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw new Exceptions.InvalidOperationException("Collabora trả về file PDF rỗng");
            }

            // Save PDF to file
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            _logger.LogInformation("PDF generated successfully: {Path} ({Size} bytes)", pdfPath, pdfBytes.Length);
            return pdfPath;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Collabora service at {Url}", _collaboraUrl);
            throw new Exceptions.InvalidOperationException("Không thể kết nối đến Collabora. Vui lòng kiểm tra service đang chạy.");
        }
        catch (Exception ex) when (ex is not BusinessException)
        {
            _logger.LogError(ex, "Error converting DOCX to PDF using Collabora");
            throw new Exceptions.InvalidOperationException($"Lỗi khi chuyển đổi sang PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Call Collabora /cool/convert-to API
    /// This endpoint accepts file upload directly and returns the converted file
    /// </summary>
    private async Task<byte[]> CallConvertToApiAsync(byte[] fileBytes, string fileName)
    {
        // Collabora convert-to endpoint: /cool/convert-to/{format}
        var convertUrl = $"{_collaboraUrl}/cool/convert-to/pdf";

        _logger.LogInformation("Collabora convert URL: {Url}", convertUrl);
        _logger.LogInformation("File size: {Size} bytes, File name: {Name}", fileBytes.Length, fileName);

        // Create multipart form data
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(fileBytes);

        // Set content type for DOCX
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        // Add file with name "data" (required by Collabora API)
        content.Add(fileContent, "data", fileName);

        var response = await _httpClient.PostAsync(convertUrl, content);

        _logger.LogInformation("Collabora response status: {StatusCode}", response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Collabora conversion failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            throw new Exceptions.InvalidOperationException($"Collabora conversion failed: {response.StatusCode} - {errorContent}");
        }

        // Response is the PDF file directly
        var pdfBytes = await response.Content.ReadAsByteArrayAsync();

        _logger.LogInformation("Received PDF from Collabora: {Size} bytes", pdfBytes.Length);

        return pdfBytes;
    }
}
