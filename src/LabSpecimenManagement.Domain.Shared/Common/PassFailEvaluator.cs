using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Đánh giá Đạt/Không đạt của kết quả so với giới hạn tiêu chuẩn.
/// Port 1-1 từ FE <c>src/utils/formatters.ts</c> (evaluatePassFail, parseResultToNumber,
/// parseResultWithOperator, convertHtmlToMathExpression) để kết luận trong file Excel
/// khớp với cột "Kết luận" trên trang chi tiết task / QLCL. Sửa logic ở đây thì sửa cả FE.
/// </summary>
public static class PassFailEvaluator
{
    /// <summary>Giá trị đại diện cho kết quả dạng text "dương tính/phát hiện" (Number.MAX_SAFE_INTEGER bên FE).</summary>
    public const double PositiveTextValue = 9007199254740991d;

    private static readonly string[] NegativeResultTexts =
    {
        "âm tính",
        "không phát hiện",
        "am tinh",
        "khong phat hien",
        "negative",
        "not detected",
        "nd",
    };

    private static readonly Dictionary<char, char> SuperscriptMap = new()
    {
        ['⁰'] = '0', ['¹'] = '1', ['²'] = '2', ['³'] = '3', ['⁴'] = '4',
        ['⁵'] = '5', ['⁶'] = '6', ['⁷'] = '7', ['⁸'] = '8', ['⁹'] = '9',
        ['⁻'] = '-', ['⁺'] = '+',
    };

    private static readonly Dictionary<char, char> SubscriptMap = new()
    {
        ['₀'] = '0', ['₁'] = '1', ['₂'] = '2', ['₃'] = '3', ['₄'] = '4',
        ['₅'] = '5', ['₆'] = '6', ['₇'] = '7', ['₈'] = '8', ['₉'] = '9',
    };

    /// <summary>
    /// true = Đạt, false = Không đạt, null = không xác định (không có kết quả / không có giới hạn / không parse được).
    /// </summary>
    public static bool? Evaluate(
        string? result,
        string? comparisonOperator,
        string? allowedLimit,
        string? lowerOperator = null,
        string? lowerLimit = null,
        string? upperOperator = null,
        string? upperLimit = null)
    {
        if (string.IsNullOrEmpty(result)) return null;

        var (resultOp, parsedValue) = ParseResultWithOperator(result);
        if (parsedValue == null) return null;
        var resultValue = parsedValue.Value;

        // --- Range comparison ---
        if (!string.IsNullOrEmpty(lowerLimit) && !string.IsNullOrEmpty(upperLimit))
        {
            var lowerVal = ParseResultToNumber(ConvertHtmlToMathExpression(lowerLimit));
            var upperVal = ParseResultToNumber(ConvertHtmlToMathExpression(upperLimit));
            if (lowerVal == null || upperVal == null) return null;

            var lowerOp = string.IsNullOrEmpty(lowerOperator) ? ">" : lowerOperator;
            var upperOp = string.IsNullOrEmpty(upperOperator) ? "<" : upperOperator;

            if (resultOp != null)
            {
                return ComparePrefixedValue(resultOp, resultValue, lowerOp, lowerVal.Value)
                    && ComparePrefixedValue(resultOp, resultValue, upperOp, upperVal.Value);
            }

            return ComparePlainValue(resultValue, lowerOp, lowerVal.Value)
                && ComparePlainValue(resultValue, upperOp, upperVal.Value);
        }

        // --- Single limit comparison ---
        if (string.IsNullOrEmpty(comparisonOperator) || string.IsNullOrEmpty(allowedLimit)) return null;

        var limitValue = ParseResultToNumber(ConvertHtmlToMathExpression(allowedLimit));
        if (limitValue == null) return null;

        return resultOp != null
            ? ComparePrefixedValue(resultOp, resultValue, comparisonOperator, limitValue.Value)
            : ComparePlainValue(resultValue, comparisonOperator, limitValue.Value);
    }

    /// <summary>
    /// Tách toán tử tiền tố của kết quả: "&lt; 5" → ("&lt;", 5); "200" → (null, 200); "Âm tính" → (null, 0).
    /// </summary>
    public static (string? Operator, double? Value) ParseResultWithOperator(string? result, int decimalPlaces = 4)
    {
        if (string.IsNullOrEmpty(result)) return (null, null);

        var trimmed = result.Trim();
        if (trimmed == "") return (null, null);

        var prefixMatch = Regex.Match(trimmed, @"^([<>≤≥]=?)\s*(.+)$");
        if (prefixMatch.Success)
        {
            var value = ParseResultToNumber(prefixMatch.Groups[2].Value, decimalPlaces);
            if (value != null)
            {
                return (NormalizeOperator(prefixMatch.Groups[1].Value), value);
            }
        }

        return (null, ParseResultToNumber(result, decimalPlaces));
    }

    /// <summary>
    /// Parse kết quả sang số (giống FE parseResultToNumber với returnZeroForText = true):
    /// số thường, "2,0 x 10^2", "1.5e3", biểu thức "10^2*5+4"; "Âm tính"/"Không phát hiện" → 0;
    /// text khác → <see cref="PositiveTextValue"/>.
    /// </summary>
    public static double? ParseResultToNumber(string? result, int decimalPlaces = 4)
    {
        if (string.IsNullOrEmpty(result)) return 0;

        var normalized = result.Trim();
        if (normalized == "") return 0;

        // Giới hạn độ dài expression để tránh DoS
        if (normalized.Length > 100) return 0;

        if (IsNegativeResultText(normalized)) return 0;

        var hasLetters = Regex.IsMatch(normalized, "[a-zA-ZÀ-ỹ]");
        var looksLikeScientific = Regex.IsMatch(normalized, @"^\d.*[eExX×].*\d", RegexOptions.IgnoreCase);
        if (hasLetters && !looksLikeScientific) return PositiveTextValue;

        normalized = normalized.Replace(",", ".");
        normalized = Regex.Replace(normalized, "[×✕✖⨉·•]", "*");
        normalized = Regex.Replace(normalized, @"[\s  -​  　]", "");
        normalized = Regex.Replace(normalized, @"(\d|\))[xX](\d|\(|\^)", "$1*$2");
        normalized = Regex.Replace(normalized, @"(\d)[xX](10\^)", "$1*$2", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"(\d)[xX](\d)", "$1*$2");

        // "1.5e3", "2E-2", "1e+5"
        var sciE = Regex.Match(normalized, @"^([+-]?(\d+(\.\d*)?|\.\d+))[eE]([+-]?\d+)$");
        if (sciE.Success
            && TryParseDouble(sciE.Groups[1].Value, out var mantissaE)
            && int.TryParse(sciE.Groups[4].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var exponentE))
        {
            return Round(mantissaE * Math.Pow(10, exponentE), decimalPlaces);
        }

        // "2.0*10^2", "2.0*10^-2"
        var sci = Regex.Match(normalized, @"^([+-]?(\d+(\.\d*)?|\.\d+))\*10\^([+-]?\d+)$");
        if (sci.Success
            && TryParseDouble(sci.Groups[1].Value, out var mantissa)
            && int.TryParse(sci.Groups[4].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var exponent))
        {
            return Round(mantissa * Math.Pow(10, exponent), decimalPlaces);
        }

        if (Regex.IsMatch(normalized, @"^[+-]?(\d+(\.\d*)?|\.\d+)$") && TryParseDouble(normalized, out var plain))
        {
            return Round(plain, decimalPlaces);
        }

        // 10^-2 → 10^(0-2) để bộ tính biểu thức xử lý được
        normalized = Regex.Replace(normalized, @"\^-(\d+)", "^(0-$1)");

        var evalResult = EvaluateMathExpression(normalized);
        if (evalResult != null && double.IsFinite(evalResult.Value))
        {
            return Round(evalResult.Value, decimalPlaces);
        }

        return IsNegativeResultText(result.Trim()) ? 0 : PositiveTextValue;
    }

    /// <summary>
    /// Chuyển HTML từ rich text editor sang biểu thức toán: "10&lt;sup&gt;-2&lt;/sup&gt;" → "10^(-2)", "2³" → "2^3".
    /// </summary>
    public static string ConvertHtmlToMathExpression(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        var result = Regex.Replace(html, "^<p>|</p>$", "", RegexOptions.IgnoreCase).Trim();

        result = Regex.Replace(result, "<sup>([^<]*)</sup>", m =>
        {
            var content = m.Groups[1].Value.Trim();
            return content.Length > 1 || content.StartsWith("-") ? $"^({content})" : $"^{content}";
        }, RegexOptions.IgnoreCase);

        result = Regex.Replace(result, "<sub>([^<]*)</sub>", "$1", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, "<[^>]+>", "");

        result = Regex.Replace(result, "[⁰¹²³⁴⁵⁶⁷⁸⁹⁻⁺]+", m =>
        {
            var converted = new string(m.Value.Select(c => SuperscriptMap.TryGetValue(c, out var d) ? d : c).ToArray());
            return converted.Length > 1 || converted.StartsWith("-") ? $"^({converted})" : $"^{converted}";
        });

        result = Regex.Replace(result, "[₀₁₂₃₄₅₆₇₈₉]+",
            m => new string(m.Value.Select(c => SubscriptMap.TryGetValue(c, out var d) ? d : c).ToArray()));

        result = result
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Replace("&nbsp;", " ");

        return result.Trim();
    }

    private static bool IsNegativeResultText(string text)
    {
        var lowerText = text.ToLowerInvariant().Trim();
        return NegativeResultTexts.Any(neg => lowerText.Contains(neg));
    }

    private static string NormalizeOperator(string op) => op.Trim() switch
    {
        "≤" => "<=",
        "≥" => ">=",
        var x => x
    };

    private static bool IsUpperBoundOp(string op) => NormalizeOperator(op) is "<=" or "<";

    private static bool IsLowerBoundOp(string op) => NormalizeOperator(op) is ">=" or ">";

    private static bool ComparePlainValue(double resultValue, string op, double limitValue) => NormalizeOperator(op) switch
    {
        "<=" => resultValue <= limitValue,
        ">=" => resultValue >= limitValue,
        "<" => resultValue < limitValue,
        ">" => resultValue > limitValue,
        "=" => resultValue == limitValue,
        _ => false
    };

    /// <summary>
    /// Kết quả có tiền tố ("&lt; V", "&gt; V") so với 1 giới hạn — không chứng minh được đạt thì coi là không đạt.
    /// </summary>
    private static bool ComparePrefixedValue(string resultOp, double resultValue, string limitOp, double limitValue)
    {
        var nResultOp = NormalizeOperator(resultOp);

        if (nResultOp is "<" or "<=")
        {
            return IsUpperBoundOp(limitOp) && resultValue <= limitValue;
        }

        if (nResultOp is ">" or ">=")
        {
            return IsLowerBoundOp(limitOp) && resultValue >= limitValue;
        }

        return false;
    }

    /// <summary>
    /// Tính biểu thức +, -, *, /, ^, () bằng shunting-yard (không dùng eval). null nếu không hợp lệ.
    /// </summary>
    private static double? EvaluateMathExpression(string expr)
    {
        expr = Regex.Replace(expr, @"\s", "");
        if (!Regex.IsMatch(expr, @"^[0-9.+\-*/^()]+$")) return null;

        // Tokenize: double = số, string = toán tử / ngoặc
        var tokens = new List<object>();
        var i = 0;
        while (i < expr.Length)
        {
            var ch = expr[i];
            if (char.IsDigit(ch) || ch == '.')
            {
                var sb = new StringBuilder();
                while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                {
                    sb.Append(expr[i]);
                    i++;
                }
                if (!TryParseJsFloat(sb.ToString(), out var num)) return null;
                tokens.Add(num);
                continue;
            }
            tokens.Add(ch.ToString());
            i++;
        }

        const string operators = "+-*/^";
        var precedence = new Dictionary<string, int> { ["+"] = 1, ["-"] = 1, ["*"] = 2, ["/"] = 2, ["^"] = 3 };
        var output = new List<object>();
        var opStack = new Stack<string>();

        bool IsOperatorToken(object t) => t is string s && operators.Contains(s);

        for (var j = 0; j < tokens.Count; j++)
        {
            var token = tokens[j];
            if (token is double)
            {
                output.Add(token);
                continue;
            }

            var tok = (string)token;
            if (tok == "(")
            {
                opStack.Push(tok);
            }
            else if (tok == ")")
            {
                while (opStack.Count > 0 && opStack.Peek() != "(") output.Add(opStack.Pop());
                if (opStack.Count == 0) return null;
                opStack.Pop();
            }
            else
            {
                var isUnaryPosition = j == 0 || (tokens[j - 1] is string prev && (prev == "(" || IsOperatorToken(prev)));
                if (tok == "-" && isUnaryPosition)
                {
                    output.Add(0d);
                }
                else if (tok == "+" && isUnaryPosition)
                {
                    continue;
                }

                while (opStack.Count > 0
                       && opStack.Peek() != "("
                       && (precedence[opStack.Peek()] > precedence[tok]
                           || (precedence[opStack.Peek()] == precedence[tok] && tok != "^")))
                {
                    output.Add(opStack.Pop());
                }
                opStack.Push(tok);
            }
        }

        while (opStack.Count > 0)
        {
            var op = opStack.Pop();
            if (op is "(" or ")") return null;
            output.Add(op);
        }

        var evalStack = new Stack<double>();
        foreach (var token in output)
        {
            if (token is double d)
            {
                evalStack.Push(d);
                continue;
            }

            if (evalStack.Count < 2) return null;
            var b = evalStack.Pop();
            var a = evalStack.Pop();
            switch ((string)token)
            {
                case "+": evalStack.Push(a + b); break;
                case "-": evalStack.Push(a - b); break;
                case "*": evalStack.Push(a * b); break;
                case "/":
                    if (b == 0) return null;
                    evalStack.Push(a / b);
                    break;
                case "^": evalStack.Push(Math.Pow(a, b)); break;
                default: return null;
            }
        }

        return evalStack.Count == 1 ? evalStack.Pop() : null;
    }

    private static bool TryParseDouble(string s, out double value) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    /// <summary>parseFloat của JS: đọc phần số hợp lệ dài nhất ở đầu chuỗi ("1.2.3" → 1.2).</summary>
    private static bool TryParseJsFloat(string s, out double value)
    {
        var m = Regex.Match(s, @"^(\d+(\.\d*)?|\.\d+)");
        if (!m.Success)
        {
            value = 0;
            return false;
        }
        return TryParseDouble(m.Value, out value);
    }

    private static double Round(double value, int decimalPlaces) =>
        Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
}
