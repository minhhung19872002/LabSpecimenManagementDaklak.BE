using System;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Implementation of code generation service
/// Provides unified code generation for all entities
/// </summary>
public class CodeGeneratorService : ICodeGeneratorService, ITransientDependency
{
    /// <inheritdoc/>
    public string GenerateCodeWithDate(string prefix, int existingCountWithPrefix, string dateFormat = "yyyyMMdd", int sequencePadding = 4)
    {
        var today = DateTime.Now;
        var dateStr = today.ToString(dateFormat);
        var sequence = (existingCountWithPrefix + 1).ToString($"D{sequencePadding}");
        return $"{prefix}-{dateStr}-{sequence}";
    }

    /// <inheritdoc/>
    public string GenerateCode(string prefix, int existingCount, int sequencePadding = 4)
    {
        var sequence = (existingCount + 1).ToString($"D{sequencePadding}");
        return $"{prefix}-{sequence}";
    }

    /// <inheritdoc/>
    public string GetTodayPrefix(string prefix, string dateFormat = "yyyyMMdd")
    {
        var today = DateTime.Now;
        return $"{prefix}-{today.ToString(dateFormat)}";
    }
}
