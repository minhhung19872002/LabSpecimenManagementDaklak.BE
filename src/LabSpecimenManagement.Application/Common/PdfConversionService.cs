using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Service for converting documents to PDF using Gotenberg API
/// Gotenberg uses LibreOffice internally for document conversion
/// </summary>
public class PdfConversionService : IPdfConversionService, ITransientDependency
{
    private readonly ILogger<PdfConversionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _gotenbergUrl;

    public PdfConversionService(
        ILogger<PdfConversionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Gotenberg");

        // Get Gotenberg URL from config, default to localhost:3000 for development
        _gotenbergUrl = configuration["Gotenberg:Url"] ?? "http://localhost:3000";
    }

    /// <summary>
    /// Check if Gotenberg service is available
    /// </summary>
    public bool IsLibreOfficeAvailable()
    {
        try
        {
            var response = _httpClient.GetAsync($"{_gotenbergUrl}/health").Result;
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gotenberg service is not available at {Url}", _gotenbergUrl);
            return false;
        }
    }

    /// <summary>
    /// Convert DOCX to PDF using Gotenberg LibreOffice API
    /// </summary>
    public async Task<string> ConvertDocxToPdfAsync(string inputDocxPath, string? outputPdfPath = null)
    {
        if (!File.Exists(inputDocxPath))
        {
            throw new UserFriendlyException($"File không tồn tại: {inputDocxPath}");
        }

        var inputDir = Path.GetDirectoryName(inputDocxPath) ?? throw new UserFriendlyException("Không thể xác định thư mục input");
        var inputFileName = Path.GetFileNameWithoutExtension(inputDocxPath);

        // Output path
        var pdfPath = outputPdfPath ?? Path.Combine(inputDir, $"{inputFileName}.pdf");

        _logger.LogInformation("Converting DOCX to PDF using Gotenberg: {Input} -> {Output}", inputDocxPath, pdfPath);

        try
        {
            // Gotenberg LibreOffice convert endpoint
            var convertUrl = $"{_gotenbergUrl}/forms/libreoffice/convert";

            using var content = new MultipartFormDataContent();

            // Read file and add to form
            var fileBytes = await File.ReadAllBytesAsync(inputDocxPath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

            // Gotenberg requires the file field name to match the filename
            var fileName = Path.GetFileName(inputDocxPath);
            content.Add(fileContent, "files", fileName);

            // Optional: Set PDF format (can add more options like landscape, page ranges, etc.)
            // content.Add(new StringContent("PDF/A-1a"), "pdfa");

            // Send request
            var response = await _httpClient.PostAsync(convertUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gotenberg conversion failed: {StatusCode} - {Error}", response.StatusCode, errorMessage);
                throw new UserFriendlyException($"Chuyển đổi PDF thất bại: {response.StatusCode}");
            }

            // Save PDF response to file
            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            _logger.LogInformation("PDF generated successfully: {Path} ({Size} bytes)", pdfPath, pdfBytes.Length);
            return pdfPath;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Gotenberg service at {Url}", _gotenbergUrl);
            throw new UserFriendlyException("Không thể kết nối đến service chuyển đổi PDF. Vui lòng kiểm tra Gotenberg đang chạy.");
        }
        catch (Exception ex) when (ex is not UserFriendlyException)
        {
            _logger.LogError(ex, "Error converting DOCX to PDF");
            throw new UserFriendlyException($"Lỗi khi chuyển đổi sang PDF: {ex.Message}");
        }
    }
}
