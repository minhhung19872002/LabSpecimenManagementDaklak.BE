using System;
using System.Text;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Utility class to convert numbers to Vietnamese text
/// Example: 1416000 -> "Một triệu bốn trăm mười sáu ngàn đồng chẵn"
/// </summary>
public static class NumberToVietnameseText
{
    private static readonly string[] Ones =
    {
        "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"
    };

    private static readonly string[] Units =
    {
        "", "ngàn", "triệu", "tỷ", "ngàn tỷ", "triệu tỷ"
    };

    /// <summary>
    /// Convert a number to Vietnamese text with currency suffix
    /// </summary>
    /// <param name="number">The number to convert</param>
    /// <returns>Vietnamese text representation with "đồng chẵn" suffix</returns>
    public static string Convert(decimal number)
    {
        if (number == 0)
            return "Không đồng";

        var intNumber = (long)Math.Abs(number);
        var result = ConvertToText(intNumber);

        // Capitalize first letter
        if (!string.IsNullOrEmpty(result))
        {
            result = char.ToUpper(result[0]) + result.Substring(1);
        }

        // Add suffix
        result += " đồng";

        // Check if there's decimal part
        var decimalPart = Math.Abs(number) - intNumber;
        if (decimalPart == 0)
        {
            result += " chẵn";
        }

        if (number < 0)
        {
            result = "Âm " + result.ToLower();
        }

        return result;
    }

    private static string ConvertToText(long number)
    {
        if (number == 0)
            return "không";

        var result = new StringBuilder();
        var unitIndex = 0;
        var isFirst = true;

        while (number > 0)
        {
            var group = (int)(number % 1000);
            number /= 1000;

            if (group > 0)
            {
                var groupText = ConvertGroup(group, !isFirst && number > 0);

                if (!string.IsNullOrEmpty(groupText))
                {
                    var unitText = Units[unitIndex];
                    var part = groupText + (string.IsNullOrEmpty(unitText) ? "" : " " + unitText);

                    if (result.Length > 0)
                    {
                        result.Insert(0, part + " ");
                    }
                    else
                    {
                        result.Append(part);
                    }
                }
                isFirst = false;
            }
            else if (!isFirst && number > 0)
            {
                // Handle groups of zeros in the middle (e.g., 1000000 = một triệu)
            }

            unitIndex++;
        }

        return result.ToString().Trim();
    }

    private static string ConvertGroup(int number, bool hasHigherUnit)
    {
        if (number == 0)
            return "";

        var result = new StringBuilder();
        var hundreds = number / 100;
        var tens = (number % 100) / 10;
        var ones = number % 10;

        // Hundreds
        if (hundreds > 0)
        {
            result.Append(Ones[hundreds] + " trăm");
        }
        else if (hasHigherUnit && (tens > 0 || ones > 0))
        {
            result.Append("không trăm");
        }

        // Tens
        if (tens > 1)
        {
            result.Append((result.Length > 0 ? " " : "") + Ones[tens] + " mươi");
        }
        else if (tens == 1)
        {
            result.Append((result.Length > 0 ? " " : "") + "mười");
        }
        else if (tens == 0 && hundreds > 0 && ones > 0)
        {
            result.Append(" lẻ");
        }

        // Ones
        if (ones > 0)
        {
            if (ones == 1)
            {
                if (tens > 1)
                    result.Append(" mốt");
                else if (tens == 1)
                    result.Append(" một");
                else
                    result.Append((result.Length > 0 ? " " : "") + "một");
            }
            else if (ones == 4)
            {
                if (tens > 0)
                    result.Append(" tư");
                else
                    result.Append((result.Length > 0 ? " " : "") + "bốn");
            }
            else if (ones == 5)
            {
                if (tens > 0)
                    result.Append(" lăm");
                else
                    result.Append((result.Length > 0 ? " " : "") + "năm");
            }
            else
            {
                result.Append((result.Length > 0 ? " " : "") + Ones[ones]);
            }
        }

        return result.ToString();
    }
}
