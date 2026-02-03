using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Chat.Channels.Zalo;

public class ZaloOAClient : IZaloOAClient, ITransientDependency
{
    private const string BaseUrl = "https://openapi.zalo.me/v3.0/oa";

    private readonly HttpClient _httpClient;
    private readonly IZaloTokenManager _tokenManager;
    private readonly ILogger<ZaloOAClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public ZaloOAClient(
        IHttpClientFactory httpClientFactory,
        IZaloTokenManager tokenManager,
        ILogger<ZaloOAClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ZaloOA");
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task<ZaloApiResponse<ZaloSendMessageData>> SendTextMessageAsync(string userId, string text)
    {
        var request = new ZaloSendMessageRequest
        {
            Recipient = new ZaloMessageRecipient { UserId = userId },
            Message = new ZaloOutboundMessage { Text = text }
        };

        return await SendMessageAsync(request);
    }

    public async Task<ZaloApiResponse<ZaloSendMessageData>> SendImageMessageAsync(string userId, string imageUrl)
    {
        var request = new ZaloSendMessageRequest
        {
            Recipient = new ZaloMessageRecipient { UserId = userId },
            Message = new ZaloOutboundMessage
            {
                Attachment = new ZaloOutboundAttachment
                {
                    Type = "template",
                    Payload = new ZaloOutboundPayload
                    {
                        Url = imageUrl
                    }
                }
            }
        };

        return await SendMessageAsync(request);
    }

    public async Task<ZaloApiResponse<ZaloSendMessageData>> SendFileMessageAsync(string userId, string fileUrl)
    {
        var request = new ZaloSendMessageRequest
        {
            Recipient = new ZaloMessageRecipient { UserId = userId },
            Message = new ZaloOutboundMessage
            {
                Attachment = new ZaloOutboundAttachment
                {
                    Type = "file",
                    Payload = new ZaloOutboundPayload
                    {
                        Url = fileUrl
                    }
                }
            }
        };

        return await SendMessageAsync(request);
    }

    public async Task<ZaloApiResponse<ZaloUserProfile>> GetUserProfileAsync(string userId)
    {
        try
        {
            var accessToken = await _tokenManager.GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("access_token", accessToken);

            var dataJson = JsonSerializer.Serialize(new { user_id = userId });
            var url = $"{BaseUrl}/getprofile?data={Uri.EscapeDataString(dataJson)}";

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Zalo getprofile response: {Response}", responseContent);

            return JsonSerializer.Deserialize<ZaloApiResponse<ZaloUserProfile>>(responseContent, JsonOptions)
                   ?? new ZaloApiResponse<ZaloUserProfile> { Error = -1, Message = "Failed to deserialize response" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Zalo user profile for {UserId}", userId);
            return new ZaloApiResponse<ZaloUserProfile>
            {
                Error = -1,
                Message = ex.Message
            };
        }
    }

    private async Task<ZaloApiResponse<ZaloSendMessageData>> SendMessageAsync(ZaloSendMessageRequest request)
    {
        try
        {
            var accessToken = await _tokenManager.GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("access_token", accessToken);

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/message/cs", request, JsonOptions);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Zalo send message response: {Response}", responseContent);

            var result = JsonSerializer.Deserialize<ZaloApiResponse<ZaloSendMessageData>>(responseContent, JsonOptions);

            if (result == null)
            {
                return new ZaloApiResponse<ZaloSendMessageData>
                {
                    Error = -1,
                    Message = "Failed to deserialize response"
                };
            }

            if (result.Error != 0)
            {
                _logger.LogWarning("Zalo API error: {Error} - {Message}", result.Error, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Zalo");
            return new ZaloApiResponse<ZaloSendMessageData>
            {
                Error = -1,
                Message = ex.Message
            };
        }
    }
}
