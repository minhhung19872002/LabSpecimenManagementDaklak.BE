using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Chat.Channels.Zalo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// Controller xử lý webhooks từ các dịch vụ bên ngoài
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ZaloWebhookHandler _zaloHandler;
    private readonly ZaloOASettings _zaloSettings;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        ZaloWebhookHandler zaloHandler,
        IOptions<ZaloOASettings> zaloSettings,
        ILogger<WebhooksController> logger)
    {
        _zaloHandler = zaloHandler;
        _zaloSettings = zaloSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint cho Zalo OA
    /// </summary>
    [HttpPost("zalo")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleZaloWebhook()
    {
        try
        {
            // Read raw body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogDebug("Received Zalo webhook: {Body}", body);

            // Get signature from header
            var signature = Request.Headers["X-ZaloOA-Signature"].ToString();

            // Verify signature
            if (!_zaloHandler.VerifySignature(body, signature))
            {
                _logger.LogWarning("Invalid Zalo webhook signature");
                return Unauthorized();
            }

            // Parse payload
            var payload = JsonSerializer.Deserialize<ZaloWebhookPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload == null)
            {
                _logger.LogWarning("Failed to parse Zalo webhook payload");
                return BadRequest();
            }

            // Process webhook (fire and forget to respond quickly)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _zaloHandler.HandleAsync(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Zalo webhook in background");
                }
            });

            // Always return 200 quickly (Zalo timeout = 5s)
            return Ok(new { status = "received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Zalo webhook");
            // Still return 200 to prevent Zalo from retrying
            return Ok(new { status = "error", message = "Internal processing error" });
        }
    }

    /// <summary>
    /// Webhook verification endpoint cho Zalo OA
    /// Zalo sẽ gọi endpoint này khi setup webhook để verify
    /// </summary>
    [HttpGet("zalo")]
    [AllowAnonymous]
    public IActionResult VerifyZaloWebhook([FromQuery] string? challenge)
    {
        _logger.LogInformation("Zalo webhook verification request received");

        // Return challenge token for Zalo to verify
        if (!string.IsNullOrEmpty(challenge))
        {
            return Ok(challenge);
        }

        return Ok(new { status = "ok" });
    }
}
