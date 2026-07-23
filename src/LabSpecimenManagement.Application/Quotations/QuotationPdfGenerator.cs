using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.ResultForms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Quotations;

public interface IQuotationPdfGenerator
{
    Task<byte[]> GeneratePdfAsync(string customerName, string currentUserName, List<QuotationPdfSampleDto> samples, decimal totalPrice);
}

public class QuotationPdfGenerator : IQuotationPdfGenerator, ITransientDependency
{
    private readonly IWebHostEnvironment _environment;
    private readonly IPdfConversionService _pdfConversionService;
    private readonly ILogger<QuotationPdfGenerator> _logger;

    private const string TemplateFileName = "Template báo giá.template.docx";

    public QuotationPdfGenerator(
        IWebHostEnvironment environment,
        IPdfConversionService pdfConversionService,
        ILogger<QuotationPdfGenerator> logger)
    {
        _environment = environment;
        _pdfConversionService = pdfConversionService;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfAsync(string customerName, string currentUserName, List<QuotationPdfSampleDto> samples, decimal totalPrice)
    {
        // 1. Get template path
        var templatePath = Path.Combine(_environment.WebRootPath ?? "", "templates", TemplateFileName);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template không tồn tại: {TemplateFileName}");
        }

        // 2. Create temp directory if not exists
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        // 3. Generate unique filename
        var fileName = $"{Guid.NewGuid():N}_BaoGia.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        try
        {
            // 4. Copy template to temp
            using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            // 5. Calculate totals
            var totalSample = samples.Sum(s => s.Quantity);
            var totalIndicators = samples.Sum(s => GroupIndicatorsByParentChild(s.Indicators).Count);
            var totalUnitPrice = samples.Sum(s => s.Indicators.Sum(i => i.UnitPrice));
            var totalAmount = samples.Sum(s => s.Quantity * s.Indicators.Sum(i => i.Quantity * i.UnitPrice));

            // 6. Build placeholder values
            var placeholders = new Dictionary<string, string>
            {
                { "{{CustomerName}}", customerName },
                { "{{CustomerNameU}}", customerName.ToUpperInvariant() },
                { "{{CurrentUser}}", currentUserName },
                { "{{CurrentDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
                { "{{TotalSample}}", FormatQuantity(totalSample) },
                { "{{TotalIndicators}}", totalIndicators.ToString() },
                { "{{TotalUnitPrice}}", FormatCurrency(totalUnitPrice) },
                { "{{TotalAmount}}", FormatCurrency(totalAmount) },
                { "{{AmountByText}}", NumberToVietnameseText.Convert(totalAmount) },
                { "{{Day}}", DateTime.Now.Day.ToString("D2") },
                { "{{Month}}", DateTime.Now.Month.ToString("D2") },
                { "{{Year}}", DateTime.Now.Year.ToString() },
                { "{{Date}}", DateTime.Now.Day.ToString("D2") }
            };

            // 7. Process document - replace placeholders and fill table
            ProcessDocument(outputPath, placeholders, samples);

            // 8. Convert to PDF
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);

            // 9. Read PDF bytes
            var pdfBytes = await File.ReadAllBytesAsync(pdfPath);

            // 10. Cleanup temp files
            try
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                if (File.Exists(pdfPath))
                    File.Delete(pdfPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary files");
            }

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quotation PDF");

            // Cleanup on error
            try
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
            catch { }

            throw;
        }
    }

    private void ProcessDocument(string filePath, Dictionary<string, string> placeholders, List<QuotationPdfSampleDto> samples)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var mainPart = document.MainDocumentPart;
        var body = mainPart?.Document?.Body;
        if (body == null || mainPart == null) return;

        // Process table first (before replacing placeholders)
        ProcessTable(body, samples);

        // Then replace placeholders
        ReplacePlaceholders(body, placeholders);

        mainPart.Document?.Save();
    }

    private void ReplacePlaceholders(Body body, Dictionary<string, string> placeholders)
    {
        // Merge split placeholders first
        foreach (var para in body.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Also merge in table cells
        foreach (var cell in body.Descendants<TableCell>())
        {
            foreach (var para in cell.Descendants<Paragraph>())
            {
                MergeSplitPlaceholders(para);
            }
        }

        // Replace placeholders
        foreach (var text in body.Descendants<Text>().ToList())
        {
            if (string.IsNullOrEmpty(text.Text)) continue;

            foreach (var placeholder in placeholders)
            {
                if (text.Text.Contains(placeholder.Key))
                {
                    text.Text = text.Text.Replace(placeholder.Key, placeholder.Value);
                }
            }
        }
    }

    private void MergeSplitPlaceholders(Paragraph para)
    {
        var runs = para.Elements<Run>().ToList();
        if (runs.Count < 2) return;

        var fullText = string.Concat(runs.Select(r => r.InnerText));
        if (!fullText.Contains("{{") || !fullText.Contains("}}")) return;

        // Find placeholder positions
        var startIdx = fullText.IndexOf("{{");
        while (startIdx >= 0)
        {
            var endIdx = fullText.IndexOf("}}", startIdx);
            if (endIdx < 0) break;

            endIdx += 2; // Include the }}
            var placeholder = fullText.Substring(startIdx, endIdx - startIdx);

            // Check if this placeholder is split across multiple runs
            int currentPos = 0;
            int startRunIdx = -1, endRunIdx = -1;
            int startPosInRun = -1;

            for (int i = 0; i < runs.Count; i++)
            {
                var runText = runs[i].InnerText;
                var runStart = currentPos;
                var runEnd = currentPos + runText.Length;

                if (startRunIdx < 0 && runStart <= startIdx && startIdx < runEnd)
                {
                    startRunIdx = i;
                    startPosInRun = startIdx - runStart;
                }

                if (runStart < endIdx && endIdx <= runEnd)
                {
                    endRunIdx = i;
                    break;
                }

                currentPos = runEnd;
            }

            // If split across runs, merge them
            if (startRunIdx >= 0 && endRunIdx >= 0 && startRunIdx != endRunIdx)
            {
                var firstRun = runs[startRunIdx];
                var firstTextElement = firstRun.GetFirstChild<Text>();
                if (firstTextElement != null)
                {
                    // Build the merged text
                    var beforePlaceholder = firstTextElement.Text.Substring(0, startPosInRun);
                    var afterPlaceholder = "";

                    // Get text after the placeholder from the last run
                    var lastRun = runs[endRunIdx];
                    var lastText = lastRun.InnerText;
                    var lastPlaceholderEnd = endIdx - (fullText.Length - lastText.Length - runs.Take(endRunIdx).Sum(r => r.InnerText.Length));
                    if (lastPlaceholderEnd < lastText.Length)
                    {
                        afterPlaceholder = lastText.Substring(lastPlaceholderEnd);
                    }

                    firstTextElement.Text = beforePlaceholder + placeholder + afterPlaceholder;

                    // Remove the merged runs
                    for (int i = endRunIdx; i > startRunIdx; i--)
                    {
                        runs[i].Remove();
                    }
                }
            }

            startIdx = fullText.IndexOf("{{", endIdx);
        }
    }

    private void ProcessTable(Body body, List<QuotationPdfSampleDto> samples)
    {
        var tables = body.Descendants<Table>().ToList();

        foreach (var table in tables)
        {
            var rows = table.Elements<TableRow>().ToList();
            if (rows.Count < 2) continue;

            // Find template row (row with {{No}} placeholder)
            TableRow? templateRow = null;

            for (int i = 0; i < rows.Count; i++)
            {
                var rowText = string.Concat(rows[i].Descendants<Text>().Select(t => t.Text));
                if (rowText.Contains("{{No}}"))
                {
                    templateRow = rows[i];
                    break;
                }
            }

            if (templateRow == null) continue;

            // Insert rows for each sample and its indicators
            OpenXmlElement lastInsertedElement = templateRow;
            int rowNumber = 1;
            var noteColumnIndex = -1;

            // Detect Note column index from template row
            var templateCells = templateRow.Elements<TableCell>().ToList();
            for (int ci = 0; ci < templateCells.Count; ci++)
            {
                var cellText = string.Concat(templateCells[ci].Descendants<Text>().Select(t => t.Text));
                if (cellText.Contains("{{Note}}"))
                {
                    noteColumnIndex = ci;
                    break;
                }
            }

            // Track combo groups for Note column vertical merge
            // Key: comboGroup string, Value: list of inserted rows
            var comboGroupRows = new Dictionary<string, List<TableRow>>();

            foreach (var sample in samples)
            {
                // Group indicators by parent-child relationship
                var groupedIndicators = GroupIndicatorsByParentChild(sample.Indicators);
                var indicatorCount = groupedIndicators.Count;
                var isFirstIndicator = true;
                var sampleComboRows = new Dictionary<string, List<TableRow>>();

                foreach (var indicatorGroup in groupedIndicators)
                {
                    var newRow = (TableRow)templateRow.CloneNode(true);
                    var cells = newRow.Elements<TableCell>().ToList();

                    // Merge split placeholders in table cells first
                    foreach (var cell in cells)
                    {
                        foreach (var para in cell.Descendants<Paragraph>())
                        {
                            MergeSplitPlaceholders(para);
                        }
                    }

                    // Build display name and calculate totals
                    var indicatorDisplayName = indicatorGroup.DisplayName;
                    var indicatorFormattedName = indicatorGroup.FormattedDisplayName;
                    var totalQuantity = indicatorGroup.TotalQuantity;
                    var totalUnitPrice = indicatorGroup.TotalUnitPrice;
                    var amount = totalQuantity * totalUnitPrice * sample.Quantity;
                    var noteValue = indicatorGroup.Note ?? "";

                    // Check if indicator name has HTML or newlines
                    var hasHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(indicatorFormattedName);
                    var hasNewlines = indicatorFormattedName?.Contains('\n') ?? false;

                    // Replace placeholders in the new row
                    foreach (var text in newRow.Descendants<Text>().ToList())
                    {
                        if (string.IsNullOrEmpty(text.Text)) continue;

                        // Handle {{Indicator}} placeholder with HTML/newlines
                        if (text.Text.Contains("{{Indicator}}") && (hasHtml || hasNewlines))
                        {
                            // Replace other placeholders first
                            text.Text = text.Text
                                .Replace("{{No}}", rowNumber.ToString())
                                .Replace("{{Sample}}", isFirstIndicator ? sample.SampleName : "")
                                .Replace("{{Quantity}}", isFirstIndicator ? FormatQuantity(sample.Quantity) : "")
                                .Replace("{{UnitPrice}}", FormatCurrency(totalUnitPrice))
                                .Replace("{{Amount}}", FormatCurrency(amount))
                                .Replace("{{Note}}", noteValue);

                            // Handle {{Indicator}} with HTML/newlines
                            ReplaceTextWithFormattedRuns(text, "{{Indicator}}", indicatorFormattedName ?? indicatorDisplayName);
                        }
                        else
                        {
                            // Simple replacement (no HTML/newlines)
                            text.Text = text.Text
                                .Replace("{{No}}", rowNumber.ToString())
                                .Replace("{{Sample}}", isFirstIndicator ? sample.SampleName : "")
                                .Replace("{{Indicator}}", indicatorDisplayName)
                                .Replace("{{Quantity}}", isFirstIndicator ? FormatQuantity(sample.Quantity) : "")
                                .Replace("{{UnitPrice}}", FormatCurrency(totalUnitPrice))
                                .Replace("{{Amount}}", FormatCurrency(amount))
                                .Replace("{{Note}}", noteValue);
                        }
                    }

                    // Handle vertical merge for Sample and Quantity columns
                    if (indicatorCount > 1 && cells.Count > 3)
                    {
                        // Merge Sample column (index 1)
                        ApplyVerticalMerge(cells[1], isFirstIndicator);

                        // Merge Quantity column (index 3)
                        ApplyVerticalMerge(cells[3], isFirstIndicator);
                    }

                    // Track combo group for Note column merge
                    if (!string.IsNullOrEmpty(indicatorGroup.ComboGroup))
                    {
                        if (!sampleComboRows.ContainsKey(indicatorGroup.ComboGroup))
                            sampleComboRows[indicatorGroup.ComboGroup] = new List<TableRow>();
                        sampleComboRows[indicatorGroup.ComboGroup].Add(newRow);
                    }

                    table.InsertAfter(newRow, lastInsertedElement);
                    lastInsertedElement = newRow;
                    rowNumber++;
                    isFirstIndicator = false;
                }

                // Apply vertical merge for Note column on combo groups within this sample
                if (noteColumnIndex >= 0)
                {
                    foreach (var (_, comboRows) in sampleComboRows)
                    {
                        if (comboRows.Count <= 1) continue;
                        for (int ri = 0; ri < comboRows.Count; ri++)
                        {
                            var rowCells = comboRows[ri].Elements<TableCell>().ToList();
                            if (noteColumnIndex < rowCells.Count)
                            {
                                ApplyVerticalMerge(rowCells[noteColumnIndex], ri == 0);
                                // Clear note text in non-first rows
                                if (ri > 0)
                                {
                                    foreach (var text in rowCells[noteColumnIndex].Descendants<Text>())
                                        text.Text = "";
                                }
                            }
                        }
                    }
                }
            }

            // Remove template row
            templateRow.Remove();

            break; // Only process first matching table
        }
    }

    /// <summary>
    /// Group indicators by parent-child relationship
    /// Parent indicator will include its children: "Parent\n- Child1\n- Child2"
    /// </summary>
    private List<IndicatorGroupResult> GroupIndicatorsByParentChild(List<QuotationPdfIndicatorDto> indicators)
    {
        var result = new List<IndicatorGroupResult>();

        // If no IndicatorId, just return indicators as-is (no grouping possible)
        if (indicators.All(i => !i.IndicatorId.HasValue))
        {
            foreach (var ind in indicators)
            {
                result.Add(new IndicatorGroupResult
                {
                    DisplayName = ind.IndicatorName,
                    FormattedDisplayName = ind.IndicatorFormattedName ?? ind.IndicatorName,
                    TotalQuantity = ind.Quantity,
                    TotalUnitPrice = ind.UnitPrice,
                    Note = ind.Note,
                    ComboGroup = ind.ComboGroup
                });
            }
            return result;
        }

        // Get indicator IDs in this sample
        var indicatorIdsInSample = indicators
            .Where(i => i.IndicatorId.HasValue)
            .Select(i => i.IndicatorId!.Value)
            .ToHashSet();

        // Track which indicators have been processed
        var processedIds = new HashSet<Guid>();

        foreach (var indicator in indicators)
        {
            if (indicator.IndicatorId.HasValue && processedIds.Contains(indicator.IndicatorId.Value))
                continue;

            // Check if this indicator is a child (has parent)
            if (indicator.ParentIndicatorId.HasValue)
            {
                // If parent is also in the sample, this will be processed with parent
                if (indicatorIdsInSample.Contains(indicator.ParentIndicatorId.Value))
                    continue;
            }

            // Check if this indicator has children in the sample
            var childrenInSample = indicators
                .Where(i => i.ParentIndicatorId.HasValue
                    && indicator.IndicatorId.HasValue
                    && i.ParentIndicatorId.Value == indicator.IndicatorId.Value)
                .ToList();

            if (childrenInSample.Any())
            {
                // This is a parent with children - create grouped row
                var namePlainBuilder = new StringBuilder();
                var nameFormattedBuilder = new StringBuilder();
                int totalQuantity = indicator.Quantity;
                decimal totalUnitPrice = indicator.UnitPrice;
                bool hasAnyFormattedName = !string.IsNullOrEmpty(indicator.IndicatorFormattedName);

                namePlainBuilder.Append(indicator.IndicatorName);
                nameFormattedBuilder.Append(indicator.IndicatorFormattedName ?? indicator.IndicatorName);

                foreach (var child in childrenInSample)
                {
                    if (!string.IsNullOrEmpty(child.IndicatorFormattedName))
                        hasAnyFormattedName = true;

                    namePlainBuilder.Append("\n- ");
                    namePlainBuilder.Append(child.IndicatorName);

                    nameFormattedBuilder.Append("\n- ");
                    nameFormattedBuilder.Append(child.IndicatorFormattedName ?? child.IndicatorName);

                    totalQuantity += child.Quantity;
                    totalUnitPrice += child.UnitPrice;

                    if (child.IndicatorId.HasValue)
                        processedIds.Add(child.IndicatorId.Value);
                }

                result.Add(new IndicatorGroupResult
                {
                    DisplayName = namePlainBuilder.ToString(),
                    FormattedDisplayName = hasAnyFormattedName ? nameFormattedBuilder.ToString() : null,
                    TotalQuantity = totalQuantity,
                    TotalUnitPrice = totalUnitPrice,
                    Note = indicator.Note,
                    ComboGroup = indicator.ComboGroup
                });
            }
            else
            {
                // Single indicator (no children or orphan child)
                result.Add(new IndicatorGroupResult
                {
                    DisplayName = indicator.IndicatorName,
                    FormattedDisplayName = indicator.IndicatorFormattedName,
                    TotalQuantity = indicator.Quantity,
                    TotalUnitPrice = indicator.UnitPrice,
                    Note = indicator.Note,
                    ComboGroup = indicator.ComboGroup
                });
            }

            if (indicator.IndicatorId.HasValue)
                processedIds.Add(indicator.IndicatorId.Value);
        }

        return result;
    }

    /// <summary>
    /// Result of grouping indicators by parent-child
    /// </summary>
    private class IndicatorGroupResult
    {
        public string DisplayName { get; set; } = "";
        public string? FormattedDisplayName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalUnitPrice { get; set; }
        public string? Note { get; set; }
        public string? ComboGroup { get; set; }
    }

    /// <summary>
    /// Replace placeholder with formatted HTML content (supports newlines and HTML tags)
    /// </summary>
    private void ReplaceTextWithFormattedRuns(Text textElement, string placeholder, string htmlContent)
    {
        if (!textElement.Text.Contains(placeholder)) return;

        var parent = textElement.Parent;
        if (parent == null) return;

        // Split text by placeholder
        var beforePlaceholder = textElement.Text.Substring(0, textElement.Text.IndexOf(placeholder));
        var afterPlaceholder = textElement.Text.Substring(textElement.Text.IndexOf(placeholder) + placeholder.Length);

        // Get base run properties
        RunProperties? baseProps = null;
        if (parent is Run parentRun)
        {
            baseProps = parentRun.RunProperties?.CloneNode(true) as RunProperties;
        }

        // Convert HTML to runs
        var formattedRuns = HtmlToOpenXmlConverter.ConvertHtmlToRuns(htmlContent, baseProps);

        // Update original text to "before" part
        textElement.Text = beforePlaceholder;

        // Insert formatted runs after the current run
        var insertAfter = parent;
        foreach (var formattedRun in formattedRuns)
        {
            insertAfter.InsertAfterSelf(formattedRun);
            insertAfter = formattedRun;
        }

        // Add "after" text if any
        if (!string.IsNullOrEmpty(afterPlaceholder))
        {
            var afterRun = new Run();
            if (baseProps != null)
            {
                afterRun.Append(baseProps.CloneNode(true));
            }
            afterRun.Append(new Text(afterPlaceholder) { Space = SpaceProcessingModeValues.Preserve });
            insertAfter.InsertAfterSelf(afterRun);
        }
    }

    private static void ApplyVerticalMerge(TableCell cell, bool isFirstRow)
    {
        var cellProps = cell.GetFirstChild<TableCellProperties>();
        if (cellProps == null)
        {
            cellProps = new TableCellProperties();
            cell.InsertAt(cellProps, 0);
        }

        if (isFirstRow)
        {
            // First row of the group - start merge
            cellProps.Append(new VerticalMerge { Val = MergedCellValues.Restart });
        }
        else
        {
            // Subsequent rows - continue merge
            cellProps.Append(new VerticalMerge());
        }
    }

    private static string FormatCurrency(decimal value)
    {
        // Format: 10000 -> "10.000"
        return value.ToString("N0", new CultureInfo("vi-VN"));
    }

    private static string FormatQuantity(int quantity)
    {
        // Format: 1 -> "01", 10 -> "10"
        return quantity < 10 ? $"0{quantity}" : quantity.ToString();
    }
}
