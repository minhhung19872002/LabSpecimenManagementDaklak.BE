using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Filters;

/// <summary>
/// Filter to handle exceptions and return them in standard ApiResponse format
/// </summary>
public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        var exception = context.Exception;
        var errors = new Dictionary<string, List<string>>();
        int statusCode;

        switch (exception)
        {
            case AbpValidationException validationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                foreach (var error in validationException.ValidationErrors)
                {
                    var memberName = error.MemberNames.FirstOrDefault() ?? "general";
                    if (!errors.ContainsKey(memberName))
                        errors[memberName] = new List<string>();
                    errors[memberName].Add(error.ErrorMessage ?? "Validation error");
                }
                break;

            case EntityNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                errors["id"] = new List<string> { "Resource not found" };
                break;

            case AbpAuthorizationException:
                statusCode = (int)HttpStatusCode.Forbidden;
                errors["authorization"] = new List<string> { "You do not have permission to perform this action" };
                break;

            case UserFriendlyException userFriendlyException:
                statusCode = (int)HttpStatusCode.BadRequest;
                errors["general"] = new List<string> { userFriendlyException.Message };
                break;

            case BusinessException businessException:
                statusCode = (int)HttpStatusCode.BadRequest;
                errors[businessException.Code ?? "business"] = new List<string> { businessException.Message };
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                errors["server"] = new List<string> { "An unexpected error occurred" };
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        var response = new ApiResponse<object>
        {
            Data = null,
            Success = false,
            StatusCode = statusCode,
            Errors = errors
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = 200 // Always return 200, actual status in body
        };
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
