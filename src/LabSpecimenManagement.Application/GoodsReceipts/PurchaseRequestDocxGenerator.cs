using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabSpecimenManagement.Products;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.GoodsReceipts;

public interface IPurchaseRequestDocxGenerator
{
    Task<string> GenerateAsync(List<PurchaseRequestItemData> items);
}

public class PurchaseRequestItemData
{
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public ProductType ProductType { get; set; }
    public string? CASNumber { get; set; }
    public string? ChemicalFormula { get; set; }
    public string? Concentration { get; set; }
    public string? Manufacturer { get; set; }
}

public class PurchaseRequestDocxGenerator : IPurchaseRequestDocxGenerator, ITransientDependency
{
    private const string TemplateName = "Template_Yêu cầu mua sắm.template.docx";

    private static readonly Dictionary<ProductType, string> ProductTypeLabels = new()
    {
        { ProductType.Chemical, "Hóa chất" },
        { ProductType.Biological, "Sinh phẩm" },
        { ProductType.Supply, "Vật tư tiêu hao" },
        { ProductType.Standard, "Chất chuẩn" },
        { ProductType.Solvent, "Dung môi" },
        { ProductType.Equipment, "Thiết bị phòng Lab" },
        { ProductType.PreparedSolution, "Dung dịch pha" },
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PurchaseRequestDocxGenerator> _logger;

    public PurchaseRequestDocxGenerator(
        IWebHostEnvironment environment,
        ILogger<PurchaseRequestDocxGenerator> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public Task<string> GenerateAsync(List<PurchaseRequestItemData> items)
    {
        var templatePath = Path.Combine(_environment.WebRootPath, "templates", TemplateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        var tempDir = Path.Combine(_environment.WebRootPath, "temp");
        Directory.CreateDirectory(tempDir);

        var fileName = $"{Guid.NewGuid():N}_YeuCauMuaSam.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        File.Copy(templatePath, outputPath, true);
        ProcessDocument(outputPath, items);

        return Task.FromResult(fileName);
    }

    private void ProcessDocument(string filePath, List<PurchaseRequestItemData> items)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var body = document.MainDocumentPart!.Document.Body!;

        foreach (var paragraph in body.Descendants<Paragraph>().ToList())
            MergeSplitPlaceholders(paragraph);

        ProcessMainTable(body, items);
        ProcessAppendixTable(body, items);
        ReplaceDatePlaceholders(body);

        document.MainDocumentPart.Document.Save();
    }

    private void ProcessMainTable(Body body, List<PurchaseRequestItemData> items)
    {
        Table? mainTable = null;
        TableRow? groupTemplateRow = null;
        TableRow? itemTemplateRow = null;

        foreach (var table in body.Descendants<Table>())
        {
            var rows = table.Elements<TableRow>().ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                if (GetRowText(rows[i]).Contains("{{G_STT}}"))
                {
                    mainTable = table;
                    groupTemplateRow = rows[i];
                    if (i + 1 < rows.Count)
                        itemTemplateRow = rows[i + 1];
                    break;
                }
            }
            if (mainTable != null) break;
        }

        if (mainTable == null || groupTemplateRow == null || itemTemplateRow == null)
        {
            _logger.LogWarning("Main table template rows not found in purchase request template");
            return;
        }

        var groups = items
            .GroupBy(x => x.ProductType)
            .OrderBy(g => (int)g.Key)
            .ToList();

        int groupNumber = 0;
        foreach (var group in groups)
        {
            groupNumber++;
            var groupLabel = ProductTypeLabels.GetValueOrDefault(group.Key, group.Key.ToString());

            var newGroupRow = (TableRow)groupTemplateRow.CloneNode(true);
            ReplaceInElement(newGroupRow, "{{G_STT}}", ToRomanNumeral(groupNumber));
            ReplaceInElement(newGroupRow, "{{ProductTypeName}}", groupLabel);
            mainTable.InsertBefore(newGroupRow, groupTemplateRow);

            int itemNumber = 0;
            foreach (var item in group)
            {
                itemNumber++;
                var newItemRow = (TableRow)itemTemplateRow.CloneNode(true);
                ReplaceInElement(newItemRow, "{{STT}}", itemNumber.ToString());
                ReplaceInElement(newItemRow, "{{ProductName}}", item.ProductName);
                ReplaceInElement(newItemRow, "{{BaseUnitName}}", item.UnitName);
                ReplaceInElement(newItemRow, "{{AvailableQuantity}}", FormatQuantity(item.Quantity));
                mainTable.InsertBefore(newItemRow, groupTemplateRow);
            }
        }

        groupTemplateRow.Remove();
        itemTemplateRow.Remove();
    }

    private void ProcessAppendixTable(Body body, List<PurchaseRequestItemData> items)
    {
        Table? appendixTable = null;
        TableRow? templateRow = null;

        foreach (var table in body.Descendants<Table>())
        {
            foreach (var row in table.Elements<TableRow>())
            {
                if (GetRowText(row).Contains("{{StandardName}}"))
                {
                    appendixTable = table;
                    templateRow = row;
                    break;
                }
            }
            if (appendixTable != null) break;
        }

        if (appendixTable == null || templateRow == null)
        {
            _logger.LogWarning("Appendix table template row not found in purchase request template");
            return;
        }

        int stt = 0;
        foreach (var item in items)
        {
            stt++;
            var newRow = (TableRow)templateRow.CloneNode(true);
            ReplaceInElement(newRow, "{{STT}}", stt.ToString());
            ReplaceInElement(newRow, "{{StandardName}}", item.ProductName);
            ReplaceInElement(newRow, "{{CASNumber}}", item.CASNumber ?? "");
            ReplaceInElement(newRow, "{{ChemicalFormula}}", item.ChemicalFormula ?? "");
            ReplaceInElement(newRow, "{{Concentration}}", item.Concentration ?? "");
            ReplaceInElement(newRow, "{{StandardUnitName}}", item.UnitName);
            ReplaceInElement(newRow, "{{StandardQuantity}}", FormatQuantity(item.Quantity));
            ReplaceInElement(newRow, "{{Manufacturer}}", item.Manufacturer ?? "");
            appendixTable.InsertBefore(newRow, templateRow);
        }

        templateRow.Remove();
    }

    private static void ReplaceDatePlaceholders(Body body)
    {
        var now = DateTime.Now;
        var replacements = new Dictionary<string, string>
        {
            { "{{Date}}", now.Day.ToString() },
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

    private static string FormatQuantity(decimal quantity)
    {
        return quantity == Math.Floor(quantity)
            ? ((int)quantity).ToString()
            : quantity.ToString("0.##");
    }

    private static string ToRomanNumeral(int number)
    {
        if (number <= 0) return number.ToString();

        var result = new StringBuilder();
        var values = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        var symbols = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

        for (int i = 0; i < values.Length; i++)
        {
            while (number >= values[i])
            {
                result.Append(symbols[i]);
                number -= values[i];
            }
        }
        return result.ToString();
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
