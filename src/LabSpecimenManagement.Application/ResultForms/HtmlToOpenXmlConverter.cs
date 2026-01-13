using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// Helper class to convert simple HTML formatting to OpenXML runs
/// Supports: bold (b/strong), italic (i/em), underline (u), subscript (sub), superscript (sup)
/// </summary>
public static class HtmlToOpenXmlConverter
{
    /// <summary>
    /// Converts HTML formatted text to a list of OpenXML Run elements
    /// </summary>
    /// <param name="html">HTML text with formatting tags</param>
    /// <param name="baseProperties">Optional base RunProperties to apply to all runs</param>
    /// <returns>List of Run elements</returns>
    public static List<Run> ConvertHtmlToRuns(string? html, RunProperties? baseProperties = null)
    {
        var runs = new List<Run>();

        if (string.IsNullOrEmpty(html))
        {
            return runs;
        }

        // Remove any whitespace-only content
        var trimmedHtml = html.Trim();
        if (string.IsNullOrEmpty(trimmedHtml))
        {
            return runs;
        }

        // Check if there are any HTML tags
        if (!ContainsHtmlTags(trimmedHtml))
        {
            // Plain text - create single run
            var plainRun = CreateRun(trimmedHtml, baseProperties);
            runs.Add(plainRun);
            return runs;
        }

        // Parse HTML and create runs
        ParseHtml(trimmedHtml, runs, new FormattingContext(), baseProperties);

        return runs;
    }

    /// <summary>
    /// Converts HTML formatted text to plain text (strips all tags)
    /// </summary>
    public static string ConvertHtmlToPlainText(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Remove all HTML tags
        return Regex.Replace(html, "<[^>]+>", string.Empty);
    }

    /// <summary>
    /// Checks if text contains HTML tags
    /// </summary>
    public static bool ContainsHtmlTags(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return Regex.IsMatch(text, "<[^>]+>");
    }

    private static void ParseHtml(string html, List<Run> runs, FormattingContext context, RunProperties? baseProperties)
    {
        var position = 0;
        while (position < html.Length)
        {
            // Find next tag
            var tagStart = html.IndexOf('<', position);

            if (tagStart == -1)
            {
                // No more tags - add remaining text
                var remainingText = html.Substring(position);
                if (!string.IsNullOrEmpty(remainingText))
                {
                    var run = CreateRun(remainingText, baseProperties, context);
                    runs.Add(run);
                }
                break;
            }

            // Add text before tag
            if (tagStart > position)
            {
                var textBeforeTag = html.Substring(position, tagStart - position);
                if (!string.IsNullOrEmpty(textBeforeTag))
                {
                    var run = CreateRun(textBeforeTag, baseProperties, context);
                    runs.Add(run);
                }
            }

            // Find end of tag
            var tagEnd = html.IndexOf('>', tagStart);
            if (tagEnd == -1)
            {
                // Invalid HTML - add rest as text
                var remainingText = html.Substring(tagStart);
                if (!string.IsNullOrEmpty(remainingText))
                {
                    var run = CreateRun(remainingText, baseProperties, context);
                    runs.Add(run);
                }
                break;
            }

            // Parse tag
            var tag = html.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim().ToLowerInvariant();
            var isClosingTag = tag.StartsWith("/");
            if (isClosingTag)
            {
                tag = tag.Substring(1);
            }

            // Update context based on tag
            UpdateContext(tag, isClosingTag, context);

            position = tagEnd + 1;
        }
    }

    private static void UpdateContext(string tag, bool isClosingTag, FormattingContext context)
    {
        switch (tag)
        {
            case "b":
            case "strong":
                context.Bold = !isClosingTag;
                break;
            case "i":
            case "em":
                context.Italic = !isClosingTag;
                break;
            case "u":
                context.Underline = !isClosingTag;
                break;
            case "sub":
                context.Subscript = !isClosingTag;
                break;
            case "sup":
                context.Superscript = !isClosingTag;
                break;
            case "br":
                // Line break - handled separately
                break;
        }
    }

    private static Run CreateRun(string text, RunProperties? baseProperties, FormattingContext? context = null)
    {
        var run = new Run();
        var runProps = new RunProperties();

        // Clone base properties if provided
        if (baseProperties != null)
        {
            foreach (var child in baseProperties.ChildElements)
            {
                runProps.Append(child.CloneNode(true));
            }
        }

        // Apply context formatting
        if (context != null)
        {
            if (context.Bold)
            {
                runProps.Append(new Bold());
            }
            if (context.Italic)
            {
                runProps.Append(new Italic());
            }
            if (context.Underline)
            {
                runProps.Append(new Underline { Val = UnderlineValues.Single });
            }
            if (context.Subscript)
            {
                runProps.Append(new VerticalTextAlignment { Val = VerticalPositionValues.Subscript });
            }
            if (context.Superscript)
            {
                runProps.Append(new VerticalTextAlignment { Val = VerticalPositionValues.Superscript });
            }
        }

        if (runProps.HasChildren)
        {
            run.Append(runProps);
        }

        // Handle line breaks in text
        var lines = text.Split(new[] { "<br>", "<br/>", "<br />" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(lines[i]))
            {
                run.Append(new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
            }
            if (i < lines.Length - 1)
            {
                run.Append(new Break());
            }
        }

        return run;
    }

    private class FormattingContext
    {
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Subscript { get; set; }
        public bool Superscript { get; set; }
    }
}
