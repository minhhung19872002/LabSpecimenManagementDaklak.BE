using System.Threading.Tasks;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Service for generating unique entity codes
/// Centralizes code generation logic for all entities
/// </summary>
public interface ICodeGeneratorService
{
    /// <summary>
    /// Generates a unique code with date-based prefix
    /// Format: {prefix}-{date}-{sequence}
    /// Example: SPE-20241231-0001, TEST-20241231-0001
    /// </summary>
    /// <param name="prefix">The code prefix (e.g., SPE, TEST, NTP)</param>
    /// <param name="existingCountWithPrefix">Current count of entities with same prefix and date</param>
    /// <param name="dateFormat">Date format string (default: yyyyMMdd)</param>
    /// <param name="sequencePadding">Padding for sequence number (default: 4 digits)</param>
    /// <returns>Generated unique code</returns>
    string GenerateCodeWithDate(string prefix, int existingCountWithPrefix, string dateFormat = "yyyyMMdd", int sequencePadding = 4);

    /// <summary>
    /// Generates a unique code without date component
    /// Format: {prefix}-{sequence}
    /// Example: CUST-0001
    /// </summary>
    /// <param name="prefix">The code prefix (e.g., CUST)</param>
    /// <param name="existingCount">Current total count of entities</param>
    /// <param name="sequencePadding">Padding for sequence number (default: 4 digits)</param>
    /// <returns>Generated unique code</returns>
    string GenerateCode(string prefix, int existingCount, int sequencePadding = 4);

    /// <summary>
    /// Generates the prefix pattern for querying existing codes with today's date
    /// </summary>
    /// <param name="prefix">The code prefix</param>
    /// <param name="dateFormat">Date format string</param>
    /// <returns>Prefix pattern for today (e.g., "SPE-20241231")</returns>
    string GetTodayPrefix(string prefix, string dateFormat = "yyyyMMdd");
}
