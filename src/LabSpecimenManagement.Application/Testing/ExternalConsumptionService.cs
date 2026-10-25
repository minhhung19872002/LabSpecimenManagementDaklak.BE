using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Testing;

// TODO: EXTERNAL-CONSUMPTION — remove this entire file after BA confirms
public class ExternalConsumptionService : ITransientDependency
{
    private const string EndpointPath = "/api/v1/testing/external-consumption";

    private readonly ILogger<ExternalConsumptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _uatApiUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExternalConsumptionService(
        ILogger<ExternalConsumptionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ExternalConsumption");
        // Timeout ngắn để UAT treo không làm chậm submit trên PROD
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _uatApiUrl = configuration["ExternalConsumption:UatApiUrl"];
    }

    /// <summary>
    /// Gọi từ luồng submit thật: fire-and-forget, không bao giờ throw.
    /// URL rỗng trong config = tắt.
    /// </summary>
    public async Task NotifyAsync(ExternalConsumptionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_uatApiUrl)) return;

        var result = await SendAsync(_uatApiUrl, request);
        if (!result.Success)
        {
            _logger.LogWarning(
                "External consumption notification failed. Url: {Url}, Status: {Status}, Error: {Error}, Body: {Body}",
                result.Url, result.StatusCode, result.Error, result.ResponseBody);
        }
    }

    /// <summary>
    /// Gửi payload sang UAT và trả về kết quả chi tiết (dùng cho endpoint test ẩn).
    /// Không throw; lỗi nằm trong result.Error.
    /// </summary>
    public async Task<ExternalConsumptionForwardResultDto> SendAsync(
        string? targetUrl, ExternalConsumptionRequestDto request)
    {
        var baseUrl = string.IsNullOrWhiteSpace(targetUrl) ? _uatApiUrl : targetUrl;
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var result = new ExternalConsumptionForwardResultDto { RequestJson = json };

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            result.Error = "Chưa có URL UAT: truyền targetUrl hoặc set config ExternalConsumption:UatApiUrl";
            return result;
        }

        result.Url = $"{baseUrl.TrimEnd('/')}{EndpointPath}";
        var sw = Stopwatch.StartNew();
        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(result.Url, content);

            result.StatusCode = (int)response.StatusCode;
            result.ResponseBody = await response.Content.ReadAsStringAsync();
            result.Success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            result.Error = ex.GetType().Name + ": " + ex.Message;
            _logger.LogWarning(ex, "Failed to send external consumption request to {Url}", result.Url);
        }
        finally
        {
            sw.Stop();
            result.ElapsedMs = sw.ElapsedMilliseconds;
        }

        return result;
    }
}
