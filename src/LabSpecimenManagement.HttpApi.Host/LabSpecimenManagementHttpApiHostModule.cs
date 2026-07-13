using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.PostgreSql;
using LabSpecimenManagement.BackgroundJobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Email;
using LabSpecimenManagement.EntityFrameworkCore;
using LabSpecimenManagement.Filters;
using LabSpecimenManagement.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.OpenApi.Models;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Timing;
using LabSpecimenManagement.Hubs;
using LabSpecimenManagement.Middlewares;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace LabSpecimenManagement;

// Lab Specimen Management System - ISO/IEC 17025:2017
// Last deployment test: 2025-12-14
[DependsOn(
    typeof(LabSpecimenManagementHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(LabSpecimenManagementApplicationModule),
    typeof(LabSpecimenManagementEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
public class LabSpecimenManagementHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("LabSpecimenManagement");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        // Disable HTTPS requirement for OpenIddict when behind reverse proxy
        // This is safe because nginx handles HTTPS termination
        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            // Allow HTTP requests from nginx reverse proxy
            builder.UseAspNetCore()
                .DisableTransportSecurityRequirement();

            // Set token lifetimes
            // AccessToken: 8 hours for security (short-lived)
            // RefreshToken: 90 days to minimize re-login after deployment
            builder.SetAccessTokenLifetime(TimeSpan.FromHours(8));
            builder.SetRefreshTokenLifetime(TimeSpan.FromDays(90));

            // Use persistent signing/encryption keys to prevent token invalidation on server restart
            // Keys are stored in wwwroot/Keys (matches Dockerfile + K8s PVC volume mount path)
            var keysPath = Path.Combine(hostingEnvironment.WebRootPath ?? Path.Combine(hostingEnvironment.ContentRootPath, "wwwroot"), "Keys");
            if (!Directory.Exists(keysPath))
            {
                Directory.CreateDirectory(keysPath);
            }

            // Add encryption key (for encrypting tokens at rest)
            var encryptionKeyFile = Path.Combine(keysPath, "encryption.key");
            if (File.Exists(encryptionKeyFile))
            {
                var encryptionKey = File.ReadAllBytes(encryptionKeyFile);
                builder.AddEncryptionKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(encryptionKey));
            }
            else
            {
                // Generate and save new encryption key
                var newEncryptionKey = new byte[32]; // 256 bits
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(newEncryptionKey);
                }
                File.WriteAllBytes(encryptionKeyFile, newEncryptionKey);
                builder.AddEncryptionKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(newEncryptionKey));
            }

            // Add signing key (for signing tokens to prevent tampering)
            var signingKeyFile = Path.Combine(keysPath, "signing.key");
            if (File.Exists(signingKeyFile))
            {
                var signingKey = File.ReadAllBytes(signingKeyFile);
                builder.AddSigningKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(signingKey));
            }
            else
            {
                // Generate and save new signing key
                var newSigningKey = new byte[64]; // 512 bits for HMAC-SHA512
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(newSigningKey);
                }
                File.WriteAllBytes(signingKeyFile, newSigningKey);
                builder.AddSigningKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(newSigningKey));
            }
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        ConfigureAuthentication(context);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        ConfigureHealthChecks(context);
        ConfigureApiFilters(context);
        ConfigureEmail(context, configuration);
        ConfigureHangfire(context, configuration);
        ConfigureSignalR(context);
        ConfigureClock();
        ConfigureForwardedHeaders(context);
        ConfigureResponseCompression(context);

        // Register HttpClient for PDF conversion services
        context.Services.AddHttpClient();

        // Register PDF conversion service based on configuration (Gotenberg or OnlyOffice)
        context.Services.AddPdfConversionService(configuration);
    }

    /// <summary>
    /// Nén response (Brotli/gzip) để giảm dung lượng JSON trên đường truyền yếu.
    /// EnableForHttps bắt buộc vì nginx forward X-Forwarded-Proto=https nên middleware
    /// coi request là HTTPS và sẽ bỏ qua nén nếu không bật cờ này.
    /// CompressionLevel.Fastest để không tốn CPU server; SignalR (websocket/event-stream)
    /// không bị ảnh hưởng vì mime type không nằm trong danh sách nén.
    /// </summary>
    private void ConfigureResponseCompression(ServiceConfigurationContext context)
    {
        context.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        // Optimal (quality ~4) nén tốt hơn hẳn Fastest (quality 1) mà CPU tăng không đáng kể;
        // đo thực tế Fastest cho kết quả kém hơn cả gzip trên payload JSON danh mục
        context.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        // Gzip chỉ là fallback cho client cũ không hỗ trợ Brotli;
        // Optimal (level 6) nén tốt hơn Fastest ~9% với chi phí CPU không đáng kể
        context.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });
    }

    /// <summary>
    /// Configure forwarded headers to support HTTPS behind nginx ingress
    /// This allows the backend to correctly detect the original HTTPS scheme from the load balancer
    /// </summary>
    private void ConfigureForwardedHeaders(ServiceConfigurationContext context)
    {
        context.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            // Clear known networks and proxies to accept any proxy (nginx ingress)
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    /// <summary>
    /// Cấu hình ABP Clock sử dụng múi giờ Vietnam (UTC+7)
    /// Lưu ý: Không ảnh hưởng đến Hangfire cronjob vì cronjob đã config TimeZone riêng
    /// </summary>
    private void ConfigureClock()
    {
        Configure<AbpClockOptions>(options =>
        {
            options.Kind = DateTimeKind.Local;
        });
    }

    private void ConfigureSignalR(ServiceConfigurationContext context)
    {
        context.Services.AddSignalR();

        // Register custom UserIdProvider for ABP Framework compatibility
        context.Services.AddSingleton<IUserIdProvider, AbpUserIdProvider>();

        // Register NotificationHubService for real-time notifications
        context.Services.AddTransient<INotificationHubService, NotificationHubService>();
    }

    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        context.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            }));

        context.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
        });
    }

    private void ConfigureEmail(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        context.Services.AddHealthChecks();
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        // Only configure physical file replacement for local development with source code
        // Docker containers only have published DLLs, so skip this configuration
        if (hostingEnvironment.IsDevelopment())
        {
            // Check if we're running in Docker (published app without source directories)
            var domainSharedPath = Path.Combine(hostingEnvironment.ContentRootPath,
                $"..{Path.DirectorySeparatorChar}LabSpecimenManagement.Domain.Shared");

            // Only configure if source directories actually exist
            if (Directory.Exists(domainSharedPath))
            {
                Configure<AbpVirtualFileSystemOptions>(options =>
                {
                    options.FileSets.ReplaceEmbeddedByPhysical<LabSpecimenManagementDomainSharedModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}LabSpecimenManagement.Domain.Shared"));
                    options.FileSets.ReplaceEmbeddedByPhysical<LabSpecimenManagementDomainModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}LabSpecimenManagement.Domain"));
                    options.FileSets.ReplaceEmbeddedByPhysical<LabSpecimenManagementApplicationContractsModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}LabSpecimenManagement.Application.Contracts"));
                    options.FileSets.ReplaceEmbeddedByPhysical<LabSpecimenManagementApplicationModule>(
                        Path.Combine(hostingEnvironment.ContentRootPath,
                            $"..{Path.DirectorySeparatorChar}LabSpecimenManagement.Application"));
                });
            }
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(LabSpecimenManagementApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                    {"LabSpecimenManagement", "LabSpecimenManagement API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "LabSpecimenManagement API", Version = "v1.0" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                var corsOrigins = configuration["App:CorsOrigins"]?
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => o.RemovePostFix("/"))
                    .ToArray() ?? Array.Empty<string>();

                builder
                    .WithOrigins(corsOrigins)
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    // Browser cache kết quả preflight 1h -> bớt 1 round-trip OPTIONS
                    // cho mỗi API call, đáng kể với đường truyền yếu
                    .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });
    }

    private void ConfigureApiFilters(ServiceConfigurationContext context)
    {
        context.Services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ApiResponseWrapperFilter>();
            options.Filters.Add<ApiExceptionFilter>();
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        // Phải đứng trước mọi middleware ghi response body thì mới nén được
        app.UseResponseCompression();

        // ETag/304 cho master data — nằm TRONG compression để hash body chưa nén
        // (ETag ổn định bất kể client hỗ trợ br hay gzip)
        app.UseMiddleware<MasterDataEtagMiddleware>();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.MapAbpStaticAssets();

        // Use forwarded headers from nginx to detect HTTPS
        // This must be before authentication to ensure OpenIddict uses the correct scheme
        app.UseForwardedHeaders();

                // Uploads are handled via Azure Blob Storage, no local file storage needed
        // var webRootPath = env.WebRootPath ?? env.ContentRootPath;
        // var uploadsPath = Path.Combine(webRootPath, "uploads");
        // if (!Directory.Exists(uploadsPath))
        // {
        //     Directory.CreateDirectory(uploadsPath);
        // }
        // app.UseStaticFiles(new StaticFileOptions
        // {
        //     FileProvider = new PhysicalFileProvider(uploadsPath),
        //     RequestPath = "/uploads"
        // });

        // CORS must be before routing to handle preflight OPTIONS requests
        app.UseCors();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }
        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LabSpecimenManagement API");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            c.OAuthScopes("LabSpecimenManagement");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();

        // Hangfire Dashboard (development only or with authentication)
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });

        // Register Hangfire recurring jobs
        ConfigureHangfireRecurringJobs();

        app.UseConfiguredEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapHub<NotificationHub>("/signalr/notifications");
        });
    }

    private void ConfigureHangfireRecurringJobs()
    {
        // Case 3 QLCL: Deadline reminder - chạy lúc 8h sáng hàng ngày (Vietnam timezone UTC+7)
        // CRON: "0 8 * * *" = At 08:00 every day
        RecurringJob.AddOrUpdate<QualityManagerReminderJob>(
            "deadline-reminder-job",
            job => job.SendDeadlineRemindersAsync(),
            "0 8 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Case 4 QLCL: Handover reminder - chạy lúc 9h sáng hàng ngày (Vietnam timezone UTC+7)
        // CRON: "0 9 * * *" = At 09:00 every day
        RecurringJob.AddOrUpdate<QualityManagerReminderJob>(
            "handover-reminder-job",
            job => job.SendHandoverRemindersAsync(),
            "0 9 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Case 3 QLKT: Pending work assignment reminder - chạy lúc 8h sáng hàng ngày
        // Nhắc nhở QLKT khi có công việc chưa phân công >= 2 ngày
        // CRON: "0 8 * * *" = At 08:00 every day
        RecurringJob.AddOrUpdate<TechnicalManagerReminderJob>(
            "pending-work-assignment-job",
            job => job.SendPendingWorkAssignmentRemindersAsync(),
            "0 8 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Case 4 SampleReceiver: Sample return reminder - chạy lúc 8h sáng hàng ngày
        // Nhắc nhở Bộ phận Giao nhận mẫu khi có TestRequest cần trả mẫu (Status = ResultSent)
        // CRON: "0 8 * * *" = At 08:00 every day
        RecurringJob.AddOrUpdate<SampleReceiverReminderJob>(
            "sample-return-reminder-job",
            job => job.SendSampleReturnRemindersAsync(),
            "0 8 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Case 3 KTV: Pending indicator reminder - chạy lúc 8h sáng hàng ngày
        // Nhắc nhở KTV khi có chỉ tiêu chưa thực hiện >= 2 ngày
        // Spam hàng ngày cho đến khi tất cả chỉ tiêu không còn trạng thái "Cần làm"
        // CRON: "0 8 * * *" = At 08:00 every day
        RecurringJob.AddOrUpdate<TechnicianReminderJob>(
            "pending-indicator-reminder-job",
            job => job.SendPendingIndicatorRemindersAsync(),
            "0 8 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Specimen Disposal Reminder: Đánh dấu mẫu cần hủy - chạy lúc 1h sáng hàng ngày
        // Khi HĐ hoàn thành (Status = Completed) + 5 ngày -> update NeedDisposal = true cho các mẫu
        // CRON: "0 1 * * *" = At 01:00 every day
        RecurringJob.AddOrUpdate<SpecimenDisposalReminderJob>(
            "specimen-disposal-reminder-job",
            job => job.MarkSpecimensForDisposalAsync(),
            "0 1 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Orphaned File Cleanup: Dọn dẹp file rác không được tham chiếu trong DB
        // File >= 2 ngày tuổi và không có reference trong DB sẽ bị xóa
        // CRON: "0 2 * * *" = At 02:00 every day
        RecurringJob.AddOrUpdate<OrphanedFileCleanupJob>(
            "orphaned-file-cleanup-job",
            job => job.ExecuteAsync(),
            "0 2 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Account expiration check - deactivate expired accounts
        // CRON: "0 1 * * *" = At 01:00 every day
        RecurringJob.AddOrUpdate<AccountExpirationJob>(
            "account-expiration-job",
            job => job.ExecuteAsync(),
            "0 1 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Department supporter expiration - remove expired department supporters
        // CRON: "0 0 * * *" = At 00:00 every day (midnight Vietnam time)
        RecurringJob.AddOrUpdate<DepartmentSupporterExpirationJob>(
            "department-supporter-expiration-job",
            job => job.ExecuteAsync(),
            "0 0 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Overdue task checker - chuyển Pending task quá hạn sang Overdue
        // CRON: "5 0 * * *" = 00:05 hàng ngày (Vietnam time)
        RecurringJob.AddOrUpdate<OverdueTaskCheckerJob>(
            "overdue-task-checker-job",
            job => job.ExecuteAsync(),
            "5 0 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Temporary access grant expiration - expire overdue grants and revoke permissions
        // CRON: "0 * * * *" = Every hour
        RecurringJob.AddOrUpdate<TemporaryAccessGrantExpirationJob>(
            "temporary-access-grant-expiration-job",
            job => job.ExecuteAsync(),
            "0 * * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            });

        // Goods receipt status auto-update: Ordering -> WaitingReceipt when ExpectedDeliveryDate <= today
        // CRON: "0 0 * * *" = At 00:00 every day (midnight Vietnam time)
        // RecurringJob.AddOrUpdate<GoodsReceiptStatusUpdateJob>(
        //     "goods-receipt-status-update-job",
        //     job => job.ExecuteAsync(),
        //     "0 0 * * *",
        //     new RecurringJobOptions
        //     {
        //         TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
        //     });
    }
}
