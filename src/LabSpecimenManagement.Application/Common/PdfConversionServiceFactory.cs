using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Factory for creating PDF conversion service based on configuration
/// Supports: Gotenberg (LibreOffice), OnlyOffice, Collabora
/// </summary>
public static class PdfConversionServiceFactory
{
    /// <summary>
    /// Register the appropriate PDF conversion service based on configuration
    /// </summary>
    public static IServiceCollection AddPdfConversionService(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["PdfConversion:Provider"]?.ToLowerInvariant() ?? "gotenberg";

        switch (provider)
        {
            case "onlyoffice":
                services.AddTransient<IPdfConversionService, OnlyOfficePdfConversionService>();
                break;
            case "collabora":
                services.AddTransient<IPdfConversionService, CollaboraPdfConversionService>();
                break;
            case "gotenberg":
            default:
                services.AddTransient<IPdfConversionService, PdfConversionService>();
                break;
        }

        return services;
    }
}
