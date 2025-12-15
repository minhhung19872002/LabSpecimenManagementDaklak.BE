using System.Collections.Generic;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Type of data</typeparam>
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, List<string>>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Data = data,
            Success = true,
            StatusCode = statusCode,
            Errors = null
        };
    }

    public static ApiResponse<T> Created(T data)
    {
        return Ok(data, 201);
    }

    public static ApiResponse<T> Fail(Dictionary<string, List<string>> errors, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Data = default,
            Success = false,
            StatusCode = statusCode,
            Errors = errors
        };
    }

    public static ApiResponse<T> Fail(string field, string message, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Data = default,
            Success = false,
            StatusCode = statusCode,
            Errors = new Dictionary<string, List<string>>
            {
                { field, new List<string> { message } }
            }
        };
    }

    public static ApiResponse<T> NotFound(string message = "Resource not found")
    {
        return Fail("id", message, 404);
    }

    public static ApiResponse<T> ServerError(string message = "Internal server error")
    {
        return Fail("server", message, 500);
    }
}

/// <summary>
/// Non-generic API response for operations without return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(int statusCode = 200)
    {
        return new ApiResponse
        {
            Data = null,
            Success = true,
            StatusCode = statusCode,
            Errors = null
        };
    }

    public static ApiResponse NoContent()
    {
        return Ok(204);
    }

    public new static ApiResponse Fail(Dictionary<string, List<string>> errors, int statusCode = 400)
    {
        return new ApiResponse
        {
            Data = null,
            Success = false,
            StatusCode = statusCode,
            Errors = errors
        };
    }

    public new static ApiResponse Fail(string field, string message, int statusCode = 400)
    {
        return new ApiResponse
        {
            Data = null,
            Success = false,
            StatusCode = statusCode,
            Errors = new Dictionary<string, List<string>>
            {
                { field, new List<string> { message } }
            }
        };
    }
}
