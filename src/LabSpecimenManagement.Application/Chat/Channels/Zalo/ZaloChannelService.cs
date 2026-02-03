using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Chat.Channels.Zalo;

/// <summary>
/// Implementation của IChannelMessageSender cho Zalo OA
/// </summary>
public class ZaloChannelService : IChannelMessageSender, ITransientDependency
{
    private readonly IZaloOAClient _zaloClient;
    private readonly ILogger<ZaloChannelService> _logger;

    public ChannelType ChannelType => ChannelType.Zalo;

    public ZaloChannelService(
        IZaloOAClient zaloClient,
        ILogger<ZaloChannelService> logger)
    {
        _zaloClient = zaloClient;
        _logger = logger;
    }

    public async Task<ChannelSendResult> SendTextAsync(string externalContactId, string content)
    {
        _logger.LogInformation("Sending text message to Zalo user {UserId}", externalContactId);

        var response = await _zaloClient.SendTextMessageAsync(externalContactId, content);

        if (response.Error == 0)
        {
            return ChannelSendResult.Ok(response.Data?.MessageId);
        }

        _logger.LogWarning("Failed to send text to Zalo user {UserId}: {Error} - {Message}",
            externalContactId, response.Error, response.Message);

        return ChannelSendResult.Fail(
            response.Message ?? "Unknown Zalo API error",
            response.Error.ToString()
        );
    }

    public async Task<ChannelSendResult> SendImageAsync(string externalContactId, string imageUrl, string? caption = null)
    {
        _logger.LogInformation("Sending image message to Zalo user {UserId}", externalContactId);

        var response = await _zaloClient.SendImageMessageAsync(externalContactId, imageUrl);

        if (response.Error == 0)
        {
            return ChannelSendResult.Ok(response.Data?.MessageId);
        }

        _logger.LogWarning("Failed to send image to Zalo user {UserId}: {Error} - {Message}",
            externalContactId, response.Error, response.Message);

        return ChannelSendResult.Fail(
            response.Message ?? "Unknown Zalo API error",
            response.Error.ToString()
        );
    }

    public async Task<ChannelSendResult> SendFileAsync(string externalContactId, string fileUrl, string fileName)
    {
        _logger.LogInformation("Sending file message to Zalo user {UserId}", externalContactId);

        var response = await _zaloClient.SendFileMessageAsync(externalContactId, fileUrl);

        if (response.Error == 0)
        {
            return ChannelSendResult.Ok(response.Data?.MessageId);
        }

        _logger.LogWarning("Failed to send file to Zalo user {UserId}: {Error} - {Message}",
            externalContactId, response.Error, response.Message);

        return ChannelSendResult.Fail(
            response.Message ?? "Unknown Zalo API error",
            response.Error.ToString()
        );
    }
}
