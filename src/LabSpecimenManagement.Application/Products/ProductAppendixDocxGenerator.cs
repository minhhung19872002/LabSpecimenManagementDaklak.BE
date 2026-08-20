using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Products;

public interface IProductAppendixDocxGenerator
{
    Task<string> GenerateAsync(List<ProductAppendixWarehouseGroup> warehouseGroups);
}

public class ProductAppendixItemData
{
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? CASNumber { get; set; }
    public string? ChemicalFormula { get; set; }
    public string? Concentration { get; set; }
    public string? Manufacturer { get; set; }
}

public class ProductAppendixWarehouseGroup
{
    public string WarehouseName { get; set; } = string.Empty;
    public List<ProductAppendixItemData> Items { get; set; } = new();
}

public class ProductAppendixDocxGenerator : IProductAppendixDocxGenerator, ITransientDependency
{
    private const string TemplateName = "Template_Phụ lục vật tư.template.docx";

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProductAppendixDocxGenerator> _logger;

    public ProductAppendixDocxGenerator(
        IWebHostEnvironment environment,
        ILogger<ProductAppendixDocxGenerator> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task<string> GenerateAsync(List<ProductAppendixWarehouseGroup> warehouseGroups)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "templates", TemplateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        var tempDir = Path.Combine(_environment.WebRootPath, "temp");
        Directory.CreateDirectory(tempDir);

        var fileName = $"{Guid.NewGuid():N}_PhuLucVatTu.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        File.Copy(templatePath, outputPath, true);
        ProcessDocument(outputPath, warehouseGroups);

        return Task.FromResult(fileName);
    }

    private void ProcessDocument(string filePath, List<ProductAppendixWarehouseGroup> warehouseGroups)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var body = document.MainDocumentPart!.Document.Body!;

        foreach (var paragraph in body.Descendants<Paragraph>().ToList())
            MergeSplitPlaceholders(paragraph);

        ProcessTable(body, warehouseGroups);
        ReplaceDatePlaceholders(body);

        document.MainDocumentPart.Document.Save();
    }

    private void ProcessTable(Body body, List<ProductAppendixWarehouseGroup> warehouseGroups)
    {
        Table? table = null;
        TableRow? groupTemplateRow = null;
        TableRow? itemTemplateRow = null;

        foreach (var t in body.Descendants<Table>())
        {
            var rows = t.Elements<TableRow>().ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                if (GetRowText(rows[i]).Contains("{{WarehouseName}}"))
                {
                    table = t;
                    groupTemplateRow = rows[i];
                    if (i + 1 < rows.Count)
                        itemTemplateRow = rows[i + 1];
                    break;
                }
            }
            if (table != null) break;
        }

        if (table == null || groupTemplateRow == null || itemTemplateRow == null)
        {
            _logger.LogWarning("Template rows not found in product appendix template");
            return;
        }

        foreach (var group in warehouseGroups)
        {
            var newGroupRow = (TableRow)groupTemplateRow.CloneNode(true);
            ReplaceInElement(newGroupRow, "{{WarehouseName}}", group.WarehouseName);
            table.InsertBefore(newGroupRow, groupTemplateRow);

            int stt = 0;
            foreach (var item in group.Items)
            {
                stt++;
                var newRow = (TableRow)itemTemplateRow.CloneNode(true);
                ReplaceInElement(newRow, "{{STT}}", stt.ToString());
                ReplaceInElement(newRow, "{{StandardName}}", item.ProductName);
                ReplaceInElement(newRow, "{{CASNumber}}", item.CASNumber ?? "");
                ReplaceInElement(newRow, "{{ChemicalFormula}}", item.ChemicalFormula ?? "");
                ReplaceInElement(newRow, "{{Concentration}}", item.Concentration ?? "");
                ReplaceInElement(newRow, "{{StandardUnitName}}", item.UnitName);
                ReplaceInElement(newRow, "{{StandardQuantity}}", "");
                ReplaceInElement(newRow, "{{Manufacturer}}", item.Manufacturer ?? "");
                table.InsertBefore(newRow, groupTemplateRow);
            }
        }

        groupTemplateRow.Remove();
        itemTemplateRow.Remove();
    }

    private static void ReplaceDatePlaceholders(Body body)
    {
        var now = DateTime.Now;
        var replacements = new Dictionary<string, string>
        {
            { "{{Date}}", now.Day.ToString() },
            { "{{ Date}}", now.Day.ToString() },
            { "{{Month}}", now.Month.ToString() },
            { "{{Year}}", now.Year.ToString() },
        };

        foreach (var text in body.Descendants<Text>())
        {
            foreach (var kvp in replacements)
            {
                if (text.Text.Contains(kvp.Key))
                    text.Text = text.Text.Replace(kvp.Key, kvp.Value);
            }
        }
    }

    private static void ReplaceInElement(OpenXmlElement element, string placeholder, string value)
    {
        foreach (var text in element.Descendants<Text>())
        {
            if (text.Text.Contains(placeholder))
            {
                text.Text = text.Text.Replace(placeholder, value);
                text.Space = SpaceProcessingModeValues.Preserve;
            }
        }
    }

    private static string GetRowText(TableRow row)
    {
        return string.Concat(row.Descendants<Text>().Select(t => t.Text));
    }

    private static void MergeSplitPlaceholders(Paragraph para)
    {
        bool merged;
        do
        {
            merged = false;
            var runs = para.Elements<Run>().ToList();
            if (runs.Count < 2) return;

            var fullText = string.Concat(runs.Select(r => r.InnerText));
            if (!fullText.Contains("{{") || !fullText.Contains("}}")) return;

            var startIdx = fullText.IndexOf("{{");
            while (startIdx >= 0)
            {
                var endIdx = fullText.IndexOf("}}", startIdx);
                if (endIdx < 0) break;

                endIdx += 2;
                var placeholder = fullText.Substring(startIdx, endIdx - startIdx);

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

                if (startRunIdx >= 0 && endRunIdx >= 0 && startRunIdx != endRunIdx)
                {
                    var firstRun = runs[startRunIdx];
                    var firstTextElement = firstRun.GetFirstChild<Text>();
                    if (firstTextElement != null)
                    {
                        var beforePlaceholder = firstTextElement.Text.Substring(0, startPosInRun);
                        var afterPlaceholder = "";

                        var lastRun = runs[endRunIdx];
                        var lastText = lastRun.InnerText;
                        var lastRunStart = runs.Take(endRunIdx).Sum(r => r.InnerText.Length);
                        var lastPlaceholderEnd = endIdx - lastRunStart;
                        if (lastPlaceholderEnd < lastText.Length)
                        {
                            afterPlaceholder = lastText.Substring(lastPlaceholderEnd);
                        }

                        firstTextElement.Text = beforePlaceholder + placeholder + afterPlaceholder;
                        firstTextElement.Space = SpaceProcessingModeValues.Preserve;

                        for (int i = endRunIdx; i > startRunIdx; i--)
                            runs[i].Remove();
                    }

                    merged = true;
                    break;
                }

                startIdx = fullText.IndexOf("{{", endIdx);
            }
        } while (merged);
    }
}
