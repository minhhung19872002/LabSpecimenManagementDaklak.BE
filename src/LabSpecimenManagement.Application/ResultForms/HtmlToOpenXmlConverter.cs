using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// Helper class to convert simple HTML formatting to OpenXML runs
/// Supports: bold (b/strong), italic (i/em), underline (u), strikethrough (s/strike),
/// subscript (sub), superscript (sup), paragraph (p), bullet list (ul/li), ordered list (ol/li)
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
        var context = new FormattingContext();
        ParseHtml(trimmedHtml, runs, context, baseProperties);

        return runs;
    }

    /// <summary>
    /// Converts HTML formatted text to plain text (strips all tags and decodes entities)
    /// </summary>
    public static string ConvertHtmlToPlainText(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Remove all HTML tags
        var stripped = Regex.Replace(html, "<[^>]+>", string.Empty);

        // Decode HTML entities (e.g., &nbsp; -> space)
        return WebUtility.HtmlDecode(stripped);
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

            // Parse tag - extract tag name (ignore attributes like class="...")
            var tagContent = html.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim();
            var isClosingTag = tagContent.StartsWith("/");
            var isSelfClosing = tagContent.EndsWith("/");
            if (isClosingTag)
            {
                tagContent = tagContent.Substring(1);
            }
            // Get just the tag name (before any space/attributes)
            var spaceIdx = tagContent.IndexOf(' ');
            var tag = (spaceIdx >= 0 ? tagContent.Substring(0, spaceIdx) : tagContent).ToLowerInvariant();
            // Remove trailing "/" for self-closing tags like <br/>
            tag = tag.TrimEnd('/');

            // Handle block-level tags that need line breaks
            HandleBlockTag(tag, isClosingTag, isSelfClosing, context, runs, baseProperties);

            // Update inline formatting context
            UpdateContext(tag, isClosingTag, context);

            position = tagEnd + 1;
        }
    }

    /// <summary>
    /// Handle block-level tags (p, ul, ol, li) by inserting line breaks.
    /// TipTap generates <li><p>text</p></li> - we skip the </p> break when inside a list item
    /// to avoid double line breaks.
    /// </summary>
    private static void HandleBlockTag(string tag, bool isClosingTag, bool isSelfClosing, FormattingContext context, List<Run> runs, RunProperties? baseProperties)
    {
        switch (tag)
        {
            case "p":
                if (isClosingTag)
                {
                    // Skip break for </p> inside <li> - the </li> will add its own break
                    // TipTap generates: <li><p>text</p></li>
                    if (context.InListItem)
                        return;

                    // Add line break after paragraph
                    AddBreakRun(runs, baseProperties);
                }
                break;

            case "ul":
                if (!isClosingTag)
                {
                    context.InUnorderedList = true;
                    context.ListItemIndex = 0;
                }
                else
                {
                    context.InUnorderedList = false;
                }
                break;

            case "ol":
                if (!isClosingTag)
                {
                    context.InOrderedList = true;
                    context.ListItemIndex = 0;
                }
                else
                {
                    context.InOrderedList = false;
                }
                break;

            case "li":
                if (!isClosingTag)
                {
                    context.InListItem = true;
                    context.ListItemIndex++;
                    // Add list item prefix
                    string prefix;
                    if (context.InOrderedList)
                    {
                        prefix = $"{context.ListItemIndex}. ";
                    }
                    else
                    {
                        prefix = "• ";
                    }
                    var prefixRun = CreateRun(prefix, baseProperties);
                    runs.Add(prefixRun);
                }
                else
                {
                    context.InListItem = false;
                    // Add line break after list item
                    AddBreakRun(runs, baseProperties);
                }
                break;

            case "br":
                // Self-closing <br> or <br/> - add line break
                if (!isClosingTag || isSelfClosing)
                {
                    AddBreakRun(runs, baseProperties);
                }
                break;
        }
    }

    /// <summary>
    /// Helper to add a Run containing only a Break element
    /// </summary>
    private static void AddBreakRun(List<Run> runs, RunProperties? baseProperties)
    {
        var breakRun = new Run();
        if (baseProperties != null)
        {
            var props = new RunProperties();
            foreach (var child in baseProperties.ChildElements)
                props.Append(child.CloneNode(true));
            breakRun.Append(props);
        }
        breakRun.Append(new Break());
        runs.Add(breakRun);
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
            case "s":
            case "strike":
                context.Strikethrough = !isClosingTag;
                break;
            case "sub":
                context.Subscript = !isClosingTag;
                break;
            case "sup":
                context.Superscript = !isClosingTag;
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
            if (context.Strikethrough)
            {
                runProps.Append(new Strike());
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

        // Decode HTML entities (e.g., &nbsp; -> space, &lt; -> <, &gt; -> >)
        var decodedText = DecodeHtmlEntities(text);

        // Handle \n newlines in text (e.g., from merged note with \n\n separators)
        var lines = decodedText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
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

    /// <summary>
    /// Decode HTML entities to their corresponding characters
    /// </summary>
    private static string DecodeHtmlEntities(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Use WebUtility.HtmlDecode to handle all standard HTML entities
        return WebUtility.HtmlDecode(text);
    }

    private class FormattingContext
    {
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public bool Subscript { get; set; }
        public bool Superscript { get; set; }
        // List context
        public bool InUnorderedList { get; set; }
        public bool InOrderedList { get; set; }
        public bool InListItem { get; set; }
        public int ListItemIndex { get; set; }
    }
}
