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
    Task<byte[]> GeneratePdfAsync(string customerName, List<QuotationEmailItemDto> items, decimal totalPrice);
}

public class QuotationPdfGenerator : IQuotationPdfGenerator, ITransientDependency
{
    private readonly IWebHostEnvironment _environment;
    private readonly IPdfConversionService _pdfConversionService;
    private readonly ILogger<QuotationPdfGenerator> _logger;

    private const string TemplateFileName = "Form bao gia.docx";

    public QuotationPdfGenerator(
        IWebHostEnvironment environment,
        IPdfConversionService pdfConversionService,
        ILogger<QuotationPdfGenerator> logger)
    {
        _environment = environment;
        _pdfConversionService = pdfConversionService;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfAsync(string customerName, List<QuotationEmailItemDto> items, decimal totalPrice)
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

            // 5. Build placeholder values
            var placeholders = new Dictionary<string, string>
            {
                { "{{CustomerName}}", customerName },
                { "{{CurrentDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
                { "{{TotalPrice}}", FormatCurrency(totalPrice) },
                { "{{Day}}", DateTime.Now.Day.ToString() },
                { "{{Month}}", DateTime.Now.Month.ToString() },
                { "{{Year}}", DateTime.Now.Year.ToString() }
            };

            // 6. Process document - replace placeholders and fill table
            ProcessDocument(outputPath, placeholders, items, totalPrice);

            // 7. Convert to PDF
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);

            // 8. Read PDF bytes
            var pdfBytes = await File.ReadAllBytesAsync(pdfPath);

            // 9. Cleanup temp files
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

    private void ProcessDocument(string filePath, Dictionary<string, string> placeholders, List<QuotationEmailItemDto> items, decimal totalPrice)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var mainPart = document.MainDocumentPart;
        var body = mainPart?.Document?.Body;
        if (body == null || mainPart == null) return;

        // Process table first
        ProcessTable(body, items, totalPrice);

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

    private void ProcessTable(Body body, List<QuotationEmailItemDto> items, decimal totalPrice)
    {
        // Find the data table (should have columns: STT, Chỉ tiêu, Phương pháp, Số mẫu, Đơn giá, Thành tiền)
        var tables = body.Descendants<Table>().ToList();

        foreach (var table in tables)
        {
            var rows = table.Elements<TableRow>().ToList();
            if (rows.Count < 2) continue;

            // Find template row (row with {{No}} placeholder)
            TableRow? templateRow = null;
            int templateRowIndex = -1;

            for (int i = 0; i < rows.Count; i++)
            {
                var rowText = string.Concat(rows[i].Descendants<Text>().Select(t => t.Text));
                if (rowText.Contains("{{No}}") || rowText.Contains("{{STT}}"))
                {
                    templateRow = rows[i];
                    templateRowIndex = i;
                    break;
                }
            }

            if (templateRow == null) continue;

            // Clone and fill rows for each item
            // Insert after template row, keeping track of last inserted row
            OpenXmlElement lastInsertedElement = templateRow;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var newRow = (TableRow)templateRow.CloneNode(true);

                // Replace placeholders in the new row
                foreach (var text in newRow.Descendants<Text>().ToList())
                {
                    if (string.IsNullOrEmpty(text.Text)) continue;

                    text.Text = text.Text
                        .Replace("{{No}}", item.No.ToString())
                        .Replace("{{STT}}", item.No.ToString())
                        .Replace("{{Parameter}}", item.Parameter)
                        .Replace("{{ChiTieu}}", item.Parameter)
                        .Replace("{{TestMethod}}", item.TestMethod)
                        .Replace("{{PhuongPhap}}", item.TestMethod)
                        .Replace("{{NumberOfSample}}", item.NumberOfSample.ToString())
                        .Replace("{{SoMau}}", item.NumberOfSample.ToString())
                        .Replace("{{UnitPrice}}", FormatCurrency(item.UnitPrice))
                        .Replace("{{DonGia}}", FormatCurrency(item.UnitPrice))
                        .Replace("{{Price}}", FormatCurrency(item.Price))
                        .Replace("{{ThanhTien}}", FormatCurrency(item.Price));
                }

                // Insert new row after last inserted element
                table.InsertAfter(newRow, lastInsertedElement);
                lastInsertedElement = newRow;
            }

            // Remove template row
            templateRow.Remove();

            // Find and update total row
            var allRows = table.Elements<TableRow>().ToList();
            foreach (var row in allRows)
            {
                foreach (var text in row.Descendants<Text>().ToList())
                {
                    if (text.Text.Contains("{{TotalPrice}}") || text.Text.Contains("{{TongTien}}"))
                    {
                        text.Text = text.Text
                            .Replace("{{TotalPrice}}", FormatCurrency(totalPrice))
                            .Replace("{{TongTien}}", FormatCurrency(totalPrice));
                    }
                }
            }

            break; // Only process first matching table
        }
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("N0", new CultureInfo("vi-VN"));
    }
}
