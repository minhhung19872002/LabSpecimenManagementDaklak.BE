using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabSpecimenManagement.Common;
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

    private const string TemplateFileName = "Báo giá HỌC VIỆN HÀNH CHÍNH 2025.template.docx";

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
            var totalUnitPrice = samples.Sum(s => s.Indicators.Sum(i => i.UnitPrice));
            var totalAmount = samples.Sum(s => s.Indicators.Sum(i => i.Quantity * i.UnitPrice));

            // 6. Build placeholder values
            var placeholders = new Dictionary<string, string>
            {
                { "{{CustomerName}}", customerName },
                { "{{CustomerNameU}}", customerName.ToUpperInvariant() },
                { "{{CurrentUser}}", currentUserName },
                { "{{CurrentDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
                { "{{TotalSample}}", FormatQuantity(totalSample) },
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

            foreach (var sample in samples)
            {
                var indicatorCount = sample.Indicators.Count;
                var isFirstIndicator = true;

                foreach (var indicator in sample.Indicators)
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

                    // Replace placeholders in the new row
                    foreach (var text in newRow.Descendants<Text>().ToList())
                    {
                        if (string.IsNullOrEmpty(text.Text)) continue;

                        var amount = indicator.Quantity * indicator.UnitPrice;

                        text.Text = text.Text
                            .Replace("{{No}}", rowNumber.ToString())
                            .Replace("{{Sample}}", isFirstIndicator ? sample.SampleName : "")
                            .Replace("{{Indicator}}", indicator.IndicatorName)
                            .Replace("{{Quantity}}", isFirstIndicator ? FormatQuantity(sample.Quantity) : "")
                            .Replace("{{UnitPrice}}", FormatCurrency(indicator.UnitPrice))
                            .Replace("{{Amount}}", FormatCurrency(amount));
                    }

                    // Handle vertical merge for Sample and Quantity columns
                    if (indicatorCount > 1 && cells.Count > 3)
                    {
                        // Merge Sample column (index 1)
                        ApplyVerticalMerge(cells[1], isFirstIndicator);

                        // Merge Quantity column (index 3)
                        ApplyVerticalMerge(cells[3], isFirstIndicator);
                    }

                    table.InsertAfter(newRow, lastInsertedElement);
                    lastInsertedElement = newRow;
                    rowNumber++;
                    isFirstIndicator = false;
                }
            }

            // Remove template row
            templateRow.Remove();

            break; // Only process first matching table
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
