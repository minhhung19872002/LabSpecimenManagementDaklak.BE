using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Chat.Channels.Zalo;

public class ZaloTokenManager : IZaloTokenManager, ITransientDependency
{
    private const string CacheKey = "zalo_oa_access_token";
    private const string TokenUrl = "https://oauth.zaloapp.com/v4/oa/access_token";

    private readonly IDistributedCache _cache;
    private readonly ZaloOASettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZaloTokenManager> _logger;

    public ZaloTokenManager(
        IDistributedCache cache,
        IOptions<ZaloOASettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<ZaloTokenManager> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient("ZaloOA");
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        // Try get from cache
        var cached = await _cache.GetStringAsync(CacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        // Refresh token
        return await RefreshAccessTokenAsync();
    }

    public async Task<string> RefreshAccessTokenAsync()
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = _settings.RefreshToken,
                ["app_id"] = _settings.AppId,
                ["grant_type"] = "refresh_token"
            });

            // Add secret key to header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("secret_key", _settings.SecretKey);

            var response = await _httpClient.PostAsync(TokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Zalo token refresh response: {Response}", responseContent);

            var result = JsonSerializer.Deserialize<ZaloTokenResponse>(responseContent);

            if (string.IsNullOrEmpty(result?.AccessToken))
            {
                _logger.LogError("Failed to refresh Zalo access token. Response: {Response}", responseContent);
                throw new InvalidOperationException($"Failed to refresh Zalo access token: {result?.ErrorDescription ?? "Unknown error"}");
            }

            // Cache with 1 hour expiry (Zalo token valid for ~24h, refresh early)
            await _cache.SetStringAsync(CacheKey, result.AccessToken, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            _logger.LogInformation("Zalo access token refreshed successfully");

            return result.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Zalo access token");
            throw;
        }
    }
}

internal class ZaloTokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int? ExpiresIn { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
}
