using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Filters;

/// <summary>
/// Filter to automatically wrap API responses in the standard ApiResponse format
/// </summary>
public class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var value = objectResult.Value;

            // Skip if already wrapped
            if (value is ApiResponse<object> || IsApiResponse(value?.GetType()))
            {
                await next();
                return;
            }

            // Handle PagedResultDto - convert to our format
            if (value != null && IsPagedResultDto(value.GetType()))
            {
                var pagedResult = ConvertPagedResult(value, context.HttpContext);
                objectResult.Value = pagedResult;
                objectResult.StatusCode = 200;
            }
            // Handle ListResultDto
            else if (value != null && IsListResultDto(value.GetType()))
            {
                var listResult = ConvertListResult(value);
                objectResult.Value = listResult;
                objectResult.StatusCode = 200;
            }
            // Handle normal response
            else
            {
                var statusCode = objectResult.StatusCode ?? 200;
                var wrappedResponse = new ApiResponse<object>
                {
                    Data = value,
                    Success = statusCode >= 200 && statusCode < 300,
                    StatusCode = statusCode,
                    Errors = null
                };
                objectResult.Value = wrappedResponse;
                objectResult.StatusCode = 200; // Always return 200, actual status in body
            }
        }

        await next();
    }

    private static bool IsApiResponse(Type? type)
    {
        if (type == null) return false;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>))
            return true;
        if (type == typeof(ApiResponse))
            return true;
        return type.BaseType != null && IsApiResponse(type.BaseType);
    }

    private static bool IsPagedResultDto(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PagedResultDto<>);
    }

    private static bool IsListResultDto(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListResultDto<>);
    }

    private static object ConvertPagedResult(object value, HttpContext httpContext)
    {
        var type = value.GetType();
        var totalCountProp = type.GetProperty("TotalCount");
        var itemsProp = type.GetProperty("Items");

        var totalCount = (long)(totalCountProp?.GetValue(value) ?? 0);
        var items = itemsProp?.GetValue(value);

        // Get page and limit from query
        var query = httpContext.Request.Query;
        var page = 1;
        var limit = 10;

        if (query.ContainsKey("page") && int.TryParse(query["page"], out var p))
            page = p;
        else if (query.ContainsKey("SkipCount") && query.ContainsKey("MaxResultCount"))
        {
            if (int.TryParse(query["SkipCount"], out var skip) && int.TryParse(query["MaxResultCount"], out var max))
            {
                limit = max > 0 ? max : 10;
                page = (skip / limit) + 1;
            }
        }

        if (query.ContainsKey("limit") && int.TryParse(query["limit"], out var l))
            limit = l;
        else if (query.ContainsKey("MaxResultCount") && int.TryParse(query["MaxResultCount"], out var maxResult))
            limit = maxResult;

        var itemType = type.GetGenericArguments()[0];
        var pagedResponseType = typeof(PagedResponse<>).MakeGenericType(itemType);
        var pagedResponse = Activator.CreateInstance(pagedResponseType);

        pagedResponseType.GetProperty("Items")?.SetValue(pagedResponse, items);
        var meta = new PaginationMeta
        {
            Total = totalCount,
            Page = page,
            Limit = limit
        };
        pagedResponseType.GetProperty("Meta")?.SetValue(pagedResponse, meta);

        var apiResponseType = typeof(ApiResponse<>).MakeGenericType(pagedResponseType);
        var apiResponse = Activator.CreateInstance(apiResponseType);
        apiResponseType.GetProperty("Data")?.SetValue(apiResponse, pagedResponse);
        apiResponseType.GetProperty("Success")?.SetValue(apiResponse, true);
        apiResponseType.GetProperty("StatusCode")?.SetValue(apiResponse, 200);

        return apiResponse!;
    }

    private static object ConvertListResult(object value)
    {
        var type = value.GetType();
        var itemsProp = type.GetProperty("Items");
        var items = itemsProp?.GetValue(value);

        return new ApiResponse<object>
        {
            Data = items,
            Success = true,
            StatusCode = 200,
            Errors = null
        };
    }
}
