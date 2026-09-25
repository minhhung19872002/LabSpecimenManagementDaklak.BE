using System;
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

    public async Task NotifyAsync(ExternalConsumptionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_uatApiUrl)) return;

        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_uatApiUrl.TrimEnd('/')}/api/v1/testing/external-consumption";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "External consumption notification failed. Status: {Status}, Body: {Body}",
                    response.StatusCode, body);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send external consumption notification to UAT");
        }
    }
}
