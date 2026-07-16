using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LabSpecimenManagement.Middlewares;

/// <summary>
/// ETag/304 cho các endpoint master data (danh mục ít thay đổi).
/// Cách hoạt động: buffer response, tính SHA-256 của body làm weak ETag.
/// Nếu client gửi If-None-Match trùng -> trả 304 body rỗng, browser dùng bản cache.
/// Data đổi -> hash đổi -> trả 200 + data mới ngay, không có cửa sổ stale.
/// Chỉ áp dụng cho GET + status 200 + path nằm trong allowlist tường minh bên dưới;
/// mọi request khác đi qua nguyên vẹn không buffer, không thêm header.
/// </summary>
public class MasterDataEtagMiddleware
{
    // Allowlist path tường minh (không wildcard) — đối chiếu từ FE services:
    // masterDataService.ts, organizationService.ts, userService.ts, prefetchService.ts
    private static readonly HashSet<string> EtagPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/customers/active",
        "/api/v1/departments/active",
        "/api/v1/form-templates",
        "/api/v1/indicator-groups/active",
        "/api/v1/indicators/get-active",
        "/api/v1/laboratories/active",
        "/api/v1/sample-conditions/active",
        "/api/v1/sample-type-categories/active",
        "/api/v1/specimen-types/active",
        "/api/v1/specimen-types/with-indicators",
        "/api/v1/standards/active",
        "/api/v1/standards/with-indicators",
        "/api/v1/storage-locations/active",
        "/api/v1/test-methods/active",
        "/api/v1/test-methods/with-indicators",
        "/api/v1/users/lookup",
        "/api/app/organization/active-organizations",
    };

    private readonly RequestDelegate _next;

    public MasterDataEtagMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            || !EtagPaths.Contains(context.Request.Path.Value ?? string.Empty))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            context.Response.Body = originalBody;

            if (context.Response.StatusCode != StatusCodes.Status200OK || buffer.Length == 0)
            {
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody);
                return;
            }

            buffer.Position = 0;
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(buffer));
            var etag = $"W/\"{hash}\"";

            // no-cache = được cache nhưng phải revalidate mỗi lần (khác no-store là cấm cache)
            context.Response.Headers.CacheControl = "private, no-cache";
            context.Response.Headers.ETag = etag;

            var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();
            if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch.Contains(hash, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.Headers.ContentType = default;
                context.Response.Headers.ContentLength = default;
                return;
            }

            context.Response.ContentLength = buffer.Length;
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
