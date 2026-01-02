using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Volo.Abp.Security.Claims;

namespace LabSpecimenManagement.Hubs;

/// <summary>
/// Custom UserIdProvider for SignalR that works with ABP Framework
/// ABP uses AbpClaimTypes.UserId instead of the default ClaimTypes.NameIdentifier
/// </summary>
public class AbpUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Try ABP's UserId claim first
        var userId = connection.User?.FindFirst(AbpClaimTypes.UserId)?.Value;

        // Fallback to standard claims if ABP claim not found
        if (string.IsNullOrEmpty(userId))
        {
            userId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        // Also try "sub" claim (used by OpenIddict/OAuth)
        if (string.IsNullOrEmpty(userId))
        {
            userId = connection.User?.FindFirst("sub")?.Value;
        }

        return userId;
    }
}
