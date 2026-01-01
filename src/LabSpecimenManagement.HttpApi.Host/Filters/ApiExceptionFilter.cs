using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Filters;

/// <summary>
/// Filter to handle exceptions and return them in standard ApiResponse format
/// Supports custom BusinessException hierarchy and ABP exceptions
/// </summary>
public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        var exception = context.Exception;
        var (statusCode, errors) = HandleException(exception);

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

    private (int statusCode, Dictionary<string, List<string>> errors) HandleException(Exception exception)
    {
        var errors = new Dictionary<string, List<string>>();
        int statusCode;

        switch (exception)
        {
            // Custom Business Exceptions
            case Common.Exceptions.ValidationException validationEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                if (validationEx.Details != null)
                {
                    errors = validationEx.Details;
                }
                else
                {
                    AddError(errors, validationEx.Field ?? "validation", validationEx.Message);
                }
                break;

            case Common.Exceptions.EntityNotFoundException entityNotFoundEx:
                statusCode = (int)HttpStatusCode.NotFound;
                AddError(errors, entityNotFoundEx.Field ?? "id", entityNotFoundEx.Message);
                break;

            case DuplicateEntityException duplicateEx:
                statusCode = (int)HttpStatusCode.Conflict;
                AddError(errors, duplicateEx.Field ?? "duplicate", duplicateEx.Message);
                break;

            case Common.Exceptions.UnauthorizedAccessException unauthorizedEx:
                statusCode = (int)HttpStatusCode.Unauthorized;
                AddError(errors, "authorization", unauthorizedEx.Message);
                break;

            case ForbiddenAccessException forbiddenEx:
                statusCode = (int)HttpStatusCode.Forbidden;
                AddError(errors, "permission", forbiddenEx.Message);
                break;

            case Common.Exceptions.InvalidOperationException invalidOpEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                AddError(errors, invalidOpEx.Field ?? "operation", invalidOpEx.Message);
                break;

            case Common.Exceptions.BusinessException businessEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                if (businessEx.Details != null)
                {
                    errors = businessEx.Details;
                }
                else
                {
                    AddError(errors, businessEx.Field ?? businessEx.ErrorCode, businessEx.Message);
                }
                break;

            // Legacy Validation Exceptions
            case TestRequestValidationException testRequestEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                foreach (var error in testRequestEx.Errors)
                {
                    AddError(errors, error.Key, error.Value);
                }
                break;

            case SpecimenTypeValidationException specimenTypeEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                foreach (var error in specimenTypeEx.Errors)
                {
                    AddError(errors, error.Key, error.Value);
                }
                break;

            // ABP Exceptions
            case AbpValidationException abpValidationEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                foreach (var error in abpValidationEx.ValidationErrors)
                {
                    var memberName = error.MemberNames.FirstOrDefault() ?? "validation";
                    AddError(errors, memberName, error.ErrorMessage ?? "Validation error");
                }
                break;

            case Volo.Abp.Domain.Entities.EntityNotFoundException abpEntityNotFoundEx:
                statusCode = (int)HttpStatusCode.NotFound;
                AddError(errors, "id", GetEntityNotFoundMessage(abpEntityNotFoundEx));
                break;

            case AbpAuthorizationException:
                statusCode = (int)HttpStatusCode.Forbidden;
                AddError(errors, "permission", "Bạn không có quyền thực hiện thao tác này");
                break;

            case UserFriendlyException userFriendlyEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                AddError(errors, userFriendlyEx.Code ?? "general", userFriendlyEx.Message);
                break;

            case Volo.Abp.BusinessException abpBusinessEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                AddError(errors, abpBusinessEx.Code ?? "business", abpBusinessEx.Message);
                break;

            // System Exceptions
            case System.UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                AddError(errors, "authorization", "Bạn cần đăng nhập để thực hiện thao tác này");
                break;

            case ArgumentNullException argNullEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                AddError(errors, argNullEx.ParamName ?? "parameter", $"Tham số '{argNullEx.ParamName}' không được để trống");
                break;

            case ArgumentException argEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                AddError(errors, argEx.ParamName ?? "parameter", argEx.Message);
                break;

            case System.InvalidOperationException sysInvalidOpEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                AddError(errors, "operation", sysInvalidOpEx.Message);
                _logger.LogWarning(sysInvalidOpEx, "Invalid operation exception");
                break;

            case NotImplementedException:
                statusCode = (int)HttpStatusCode.NotImplemented;
                AddError(errors, "feature", "Tính năng này chưa được triển khai");
                break;

            case TimeoutException:
                statusCode = (int)HttpStatusCode.RequestTimeout;
                AddError(errors, "timeout", "Yêu cầu đã hết thời gian chờ, vui lòng thử lại");
                break;

            case OperationCanceledException:
                statusCode = 499; // Client Closed Request
                AddError(errors, "cancelled", "Yêu cầu đã bị hủy");
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                var errorMessage = _environment.IsDevelopment()
                    ? exception.Message
                    : "Đã xảy ra lỗi không mong muốn, vui lòng thử lại sau";
                AddError(errors, "server", errorMessage);
                _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
                break;
        }

        return (statusCode, errors);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.ContainsKey(key))
        {
            errors[key] = new List<string>();
        }
        errors[key].Add(message);
    }

    private static string GetEntityNotFoundMessage(Volo.Abp.Domain.Entities.EntityNotFoundException ex)
    {
        if (ex.EntityType != null)
        {
            var entityName = GetFriendlyEntityName(ex.EntityType.Name);
            return $"{entityName} không tồn tại";
        }
        return "Không tìm thấy dữ liệu";
    }

    private static string GetFriendlyEntityName(string entityTypeName)
    {
        // Map entity names to Vietnamese friendly names
        return entityTypeName switch
        {
            "TestRequest" => "Phiếu yêu cầu",
            "LaboratoryTask" => "Công việc khoa phòng",
            "Customer" => "Khách hàng",
            "Indicator" => "Chỉ tiêu",
            "Laboratory" => "Khoa phòng",
            "TestMethod" => "Phương pháp thử",
            "Standard" => "Tiêu chuẩn",
            "Employee" => "Nhân viên",
            "Specimen" => "Mẫu",
            "SpecimenType" => "Loại mẫu",
            "Department" => "Phòng ban",
            "StorageLocation" => "Vị trí lưu trữ",
            "IndicatorGroup" => "Nhóm chỉ tiêu",
            "Subcontractor" => "Nhà thầu phụ",
            "ResultForm" => "Phiếu kết quả",
            "NCR" => "Phiếu NCR",
            "Report" => "Báo cáo",
            _ => entityTypeName
        };
    }
}
