using NCalc;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Helper class for parsing scientific notation and mathematical expressions in test results
/// Uses NCalc library for expression evaluation
/// </summary>
public static class ResultParserHelper
{
    /// <summary>
    /// Parse result string to decimal number
    /// Supports formats:
    /// - Plain number: "200", "0.5", "1,5"
    /// - Scientific notation: "2,0 x 10^2", "1.5 x 10^-3", "2,0x10^2"
    /// - With multiplication sign variations: "x", "×", "*"
    /// </summary>
    /// <param name="result">Result string to parse</param>
    /// <param name="decimalPlaces">Number of decimal places (default: 4)</param>
    /// <returns>Parsed decimal rounded to specified decimal places, or null if cannot parse</returns>
    /// <example>
    /// ParseResultToNumber("2,0 x 10^2") // returns 200.0000m
    /// ParseResultToNumber("1,5 x 10^-3") // returns 0.0015m
    /// ParseResultToNumber("200") // returns 200.0000m
    /// ParseResultToNumber("Âm tính") // returns null
    /// </example>
    public static decimal? ParseResultToNumber(string? result, int decimalPlaces = 4)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        // Trim and normalize the string
        var normalized = result.Trim();

        // Try scientific notation first: "2,0 x 10^2", "2.0 × 10^-3", etc.
        var scientificPattern = @"^([+-]?\d+[,.]?\d*)\s*[x×*]\s*10\^([+-]?\d+)$";
        var match = Regex.Match(normalized, scientificPattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            // Parse mantissa (replace comma with dot for Vietnamese format)
            var mantissaStr = match.Groups[1].Value.Replace(",", ".");
            var exponentStr = match.Groups[2].Value;

            if (decimal.TryParse(mantissaStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var mantissa) &&
                int.TryParse(exponentStr, out var exponent))
            {
                try
                {
                    var value = mantissa * (decimal)Math.Pow(10, exponent);
                    return Math.Round(value, decimalPlaces);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        // Replace Vietnamese decimal separator (comma) with dot
        normalized = normalized.Replace(",", ".");

        // Remove whitespace
        normalized = Regex.Replace(normalized, @"\s", "");

        // Try parsing as plain number first (fastest)
        if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var plainNumber))
        {
            return Math.Round(plainNumber, decimalPlaces);
        }

        // Try NCalc for more complex expressions
        try
        {
            var expression = new Expression(normalized);
            var evalResult = expression.Evaluate();

            if (evalResult is double doubleValue)
            {
                return Math.Round((decimal)doubleValue, decimalPlaces);
            }

            if (evalResult is int intValue)
            {
                return Math.Round((decimal)intValue, decimalPlaces);
            }

            if (evalResult is decimal decimalValue)
            {
                return Math.Round(decimalValue, decimalPlaces);
            }

            if (evalResult is float floatValue)
            {
                return Math.Round((decimal)floatValue, decimalPlaces);
            }

            if (evalResult is long longValue)
            {
                return Math.Round((decimal)longValue, decimalPlaces);
            }
        }
        catch
        {
            // Expression evaluation failed - not a valid mathematical expression
        }

        // Cannot parse - return null (e.g., "Âm tính", "Dương tính", chemical formulas)
        return null;
    }

    /// <summary>
    /// Compare result value with limit
    /// </summary>
    /// <param name="result">Result string (can be scientific notation)</param>
    /// <param name="limit">Limit value (decimal)</param>
    /// <param name="decimalPlaces">Decimal places for comparison (default: 4)</param>
    /// <returns>Comparison result object</returns>
    /// <example>
    /// CompareResultWithLimit("2,0 x 10^2", 500m)
    /// // returns { ParsedValue: 200m, IsWithinLimit: true, CanCompare: true }
    ///
    /// CompareResultWithLimit("2,0 x 10^3", 500m)
    /// // returns { ParsedValue: 2000m, IsWithinLimit: false, CanCompare: true }
    ///
    /// CompareResultWithLimit("Âm tính", 500m)
    /// // returns { ParsedValue: null, IsWithinLimit: null, CanCompare: false }
    /// </example>
    public static ResultComparisonResult CompareResultWithLimit(
        string? result,
        decimal? limit,
        int decimalPlaces = 4)
    {
        var parsedValue = ParseResultToNumber(result, decimalPlaces);

        // Cannot compare if result is not a number
        if (parsedValue == null)
        {
            return new ResultComparisonResult
            {
                ParsedValue = null,
                IsWithinLimit = null,
                CanCompare = false
            };
        }

        // Cannot compare if limit is not defined
        if (limit == null)
        {
            return new ResultComparisonResult
            {
                ParsedValue = parsedValue,
                IsWithinLimit = null,
                CanCompare = false
            };
        }

        return new ResultComparisonResult
        {
            ParsedValue = parsedValue,
            IsWithinLimit = parsedValue <= limit,
            CanCompare = true
        };
    }
}

/// <summary>
/// Result of comparing a test result value with a limit
/// </summary>
public class ResultComparisonResult
{
    /// <summary>
    /// The parsed numeric value (or null if not parseable)
    /// </summary>
    public decimal? ParsedValue { get; set; }

    /// <summary>
    /// True if result is less than or equal to limit, false if result exceeds limit, null if cannot compare
    /// </summary>
    public bool? IsWithinLimit { get; set; }

    /// <summary>
    /// True if both values are numeric and can be compared
    /// </summary>
    public bool CanCompare { get; set; }
}
