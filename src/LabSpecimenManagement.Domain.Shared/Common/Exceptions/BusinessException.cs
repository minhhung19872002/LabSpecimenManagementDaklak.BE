using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Common.Exceptions;

/// <summary>
/// Base exception for business logic errors
/// </summary>
public class BusinessException : Exception
{
    /// <summary>
    /// Error code for categorizing the error
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Field name associated with the error (for validation errors)
    /// </summary>
    public string? Field { get; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, List<string>>? Details { get; }

    public BusinessException(string message, string errorCode = "BusinessError", string? field = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Field = field;
    }

    public BusinessException(string message, Dictionary<string, List<string>> details, string errorCode = "ValidationError")
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

/// <summary>
/// Exception for entity not found errors
/// </summary>
public class EntityNotFoundException : BusinessException
{
    public string EntityType { get; }
    public object? EntityId { get; }

    public EntityNotFoundException(string entityType, object? entityId = null)
        : base($"{entityType} not found", "EntityNotFound", "id")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public EntityNotFoundException(Type entityType, object? entityId = null)
        : this(entityType.Name, entityId)
    {
    }
}

/// <summary>
/// Exception for validation errors
/// </summary>
public class ValidationException : BusinessException
{
    public ValidationException(string field, string message)
        : base(message, "ValidationError", field)
    {
    }

    public ValidationException(Dictionary<string, List<string>> errors)
        : base("One or more validation errors occurred", errors, "ValidationError")
    {
    }
}

/// <summary>
/// Exception for duplicate entity errors
/// </summary>
public class DuplicateEntityException : BusinessException
{
    public DuplicateEntityException(string field, string value)
        : base($"Entity with {field} '{value}' already exists", "DuplicateEntity", field)
    {
    }
}

/// <summary>
/// Exception for unauthorized access
/// </summary>
public class UnauthorizedAccessException : BusinessException
{
    public UnauthorizedAccessException(string message = "You are not authorized to perform this action")
        : base(message, "Unauthorized")
    {
    }
}

/// <summary>
/// Exception for forbidden access (authenticated but not permitted)
/// </summary>
public class ForbiddenAccessException : BusinessException
{
    public ForbiddenAccessException(string message = "You do not have permission to perform this action")
        : base(message, "Forbidden")
    {
    }
}

/// <summary>
/// Exception for invalid operation/state
/// </summary>
public class InvalidOperationException : BusinessException
{
    public InvalidOperationException(string message, string? field = null)
        : base(message, "InvalidOperation", field)
    {
    }
}
