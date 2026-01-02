using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Interface for entities that have a Code property
/// </summary>
public interface IHasCode
{
    string Code { get; }
}

/// <summary>
/// Interface for entities that have a Name property
/// </summary>
public interface IHasName
{
    string Name { get; }
}

/// <summary>
/// Service for validating entity uniqueness and other common validations
/// </summary>
public interface IEntityValidationService
{
    /// <summary>
    /// Validates that an entity matching the predicate does not exist
    /// Throws DuplicateEntityException if a duplicate is found
    /// </summary>
    Task ValidateUniqueAsync<T>(
        IRepository<T, Guid> repository,
        Expression<Func<T, bool>> predicate,
        string field,
        string errorMessage,
        Guid? excludeId = null) where T : class, IEntity<Guid>;

    /// <summary>
    /// Validates that a code is unique for an entity
    /// </summary>
    Task ValidateUniqueCodeAsync<T>(
        IRepository<T, Guid> repository,
        string code,
        string entityName,
        Guid? excludeId = null) where T : class, IEntity<Guid>, IHasCode;

    /// <summary>
    /// Validates that a name is unique for an entity
    /// </summary>
    Task ValidateUniqueNameAsync<T>(
        IRepository<T, Guid> repository,
        string name,
        string entityName,
        Guid? excludeId = null) where T : class, IEntity<Guid>, IHasName;
}

/// <summary>
/// Implementation of entity validation service
/// Provides common validation operations for entities
/// </summary>
public class EntityValidationService : IEntityValidationService, ITransientDependency
{
    /// <inheritdoc/>
    public async Task ValidateUniqueAsync<T>(
        IRepository<T, Guid> repository,
        Expression<Func<T, bool>> predicate,
        string field,
        string errorMessage,
        Guid? excludeId = null) where T : class, IEntity<Guid>
    {
        var query = await repository.GetQueryableAsync();

        // Apply the predicate
        query = query.Where(predicate);

        // Exclude the current entity if updating
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        var exists = query.Any();
        if (exists)
        {
            throw new ValidationException(field, errorMessage);
        }
    }

    /// <inheritdoc/>
    public async Task ValidateUniqueCodeAsync<T>(
        IRepository<T, Guid> repository,
        string code,
        string entityName,
        Guid? excludeId = null) where T : class, IEntity<Guid>, IHasCode
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        await ValidateUniqueAsync(
            repository,
            x => x.Code == code,
            "code",
            $"Mã {entityName} '{code}' đã tồn tại",
            excludeId);
    }

    /// <inheritdoc/>
    public async Task ValidateUniqueNameAsync<T>(
        IRepository<T, Guid> repository,
        string name,
        string entityName,
        Guid? excludeId = null) where T : class, IEntity<Guid>, IHasName
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await ValidateUniqueAsync(
            repository,
            x => x.Name == name,
            "name",
            $"Tên {entityName} '{name}' đã tồn tại",
            excludeId);
    }
}
