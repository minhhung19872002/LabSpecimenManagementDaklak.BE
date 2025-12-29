using System;
using System.Net.Http.Headers;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace LabSpecimenManagement;

/// <summary>
/// Authorization filter for Hangfire Dashboard with Basic Authentication
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    // Default credentials - should be moved to appsettings.json in production
    private const string Username = "hangfire";
    private const string Password = "Hangfire@2025";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check for Basic Authentication header
        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            // Request authentication
            SetAuthenticationChallenge(httpContext);
            return false;
        }

        // Decode and validate credentials
        try
        {
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var credentials = decodedCredentials.Split(':', 2);

            if (credentials.Length == 2 &&
                credentials[0] == Username &&
                credentials[1] == Password)
            {
                return true;
            }
        }
        catch
        {
            // Invalid Base64 encoding
        }

        // Invalid credentials - request authentication again
        SetAuthenticationChallenge(httpContext);
        return false;
    }

    private void SetAuthenticationChallenge(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
    }
}
