using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// BM03-QT07 "Sổ quản lý mẫu" - phiếu bàn giao KH (1 dòng / mẫu của phiếu yêu cầu)
/// </summary>
public partial class ResultFormAppService
{
    private const string SampleManagementBookRowMarker = "{{TestRequestTable}}";

    // Cột "Tên cơ sở" (0-based) - merge dọc khi phiếu có nhiều mẫu
    private const int SampleManagementBookCustomerColumn = 1;

    private static readonly char[] CellWhitespace = { ' ', ' ' };

    public async Task<GenerateTestRequestFormOutput> GenerateSampleManagementBookAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.GetAsync(testRequestId);
        var samples = (await _sampleRepository.GetListAsync(x => x.TestRequestId == testRequestId && !x.IsCancelled))
            .OrderBy(s => s.SampleCode, StringComparer.Ordinal)
            .ToList();

        var templatePath = Path.Combine(_environment.WebRootPath ?? "", "templates", TemplateConstants.TemplateSampleManagementBook);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", TemplateConstants.TemplateSampleManagementBook);
        }

        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        Directory.CreateDirectory(tempDir);

        var safeContractNumber = (testRequest.ContractNumber ?? testRequest.RequestNumber ?? "unknown")
            .Replace("/", "-").Replace("\\", "-");
        var fileName = $"{Guid.NewGuid():N}_SoQuanLyMau_{safeContractNumber}.docx";
        var outputPath = Path.Combine(tempDir, fileName);
        File.Copy(templatePath, outputPath, overwrite: true);

        // Ngày nhận mẫu = Ngày HĐ, Ngày hẹn trả KQ = Ngày trả KQ (theo mapping BA trên màn chi tiết)
        var contractDate = testRequest.ContractDate?.ToString("dd/MM/yyyy") ?? "";
        var resultDeliveryDate = testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "";

        var rows = samples.Select((sample, index) => new Dictionary<string, string>
        {
            { SampleManagementBookRowMarker, "" },
            { "{{No}}", (index + 1).ToString() },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{NameOfSample}}", sample.SampleName ?? "" },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{FormPrintedDate}}", contractDate },
            { "{{DateReceiving}}", resultDeliveryDate },
            { "{{Quantity}}", sample.SampleQuantity ?? "" },
        }).ToList();

        ProcessSampleManagementBook(outputPath, rows);

        _logger.LogInformation("Sample management book generated: {Path} ({Count} samples)", outputPath, rows.Count);

        return new GenerateTestRequestFormOutput
        {
            FileName = fileName,
            RequestNumber = testRequest.RequestNumber ?? "",
            ContractNumber = testRequest.ContractNumber ?? ""
        };
    }

    private void ProcessSampleManagementBook(string filePath, List<Dictionary<string, string>> rows)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        var templateRow = body.Descendants<TableRow>()
            .FirstOrDefault(r => r.InnerText.Contains(SampleManagementBookRowMarker));
        if (templateRow == null)
        {
            _logger.LogWarning("Sample management book template has no {Marker} row", SampleManagementBookRowMarker);
            return;
        }

        // Lặp lại các dòng tiêu đề khi bảng sang trang
        foreach (var headerRow in templateRow.ElementsBefore().OfType<TableRow>())
        {
            var trPr = headerRow.TableRowProperties ??= new TableRowProperties();
            if (!trPr.Elements<TableHeader>().Any()) trPr.AppendChild(new TableHeader());
        }

        foreach (var para in templateRow.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Không có mẫu → vẫn giữ 1 dòng trống cho sổ
        if (rows.Count == 0)
        {
            rows.Add(new Dictionary<string, string>
            {
                { SampleManagementBookRowMarker, "" }, { "{{No}}", "" }, { "{{Customer}}", "" },
                { "{{NameOfSample}}", "" }, { "{{ContractNo}}", "" }, { "{{FormPrintedDate}}", "" },
                { "{{DateReceiving}}", "" }, { "{{Quantity}}", "" },
            });
        }

        var lastRow = templateRow;
        for (var i = 0; i < rows.Count; i++)
        {
            var row = (TableRow)templateRow.CloneNode(true);
            PrepareSampleManagementBookRow(row);
            ReplacePlaceholdersInElement(row, TrimPlaceholderValues(rows[i]));

            foreach (var para in row.Descendants<Paragraph>())
            {
                TrimParagraphWhitespace(para);
            }

            if (rows.Count > 1)
            {
                MergeCustomerCell(row, isFirst: i == 0);
            }

            lastRow.InsertAfterSelf(row);
            lastRow = row;
        }

        templateRow.Remove();
        document.MainDocumentPart!.Document.Save();
    }

    /// <summary>
    /// Template để noWrap ở các ô → tên mẫu/tên cơ sở dài sẽ tràn cột. Bỏ noWrap, không cho tách dòng qua trang.
    /// </summary>
    private static void PrepareSampleManagementBookRow(TableRow row)
    {
        var trPr = row.TableRowProperties ??= new TableRowProperties();
        if (!trPr.Elements<CantSplit>().Any()) trPr.AppendChild(new CantSplit());

        foreach (var cellProps in row.Elements<TableCell>().Select(c => c.TableCellProperties).Where(p => p != null))
        {
            cellProps!.NoWrap = null;
        }
    }

    private static void MergeCustomerCell(TableRow row, bool isFirst)
    {
        var cell = row.Elements<TableCell>().ElementAtOrDefault(SampleManagementBookCustomerColumn);
        if (cell == null) return;

        var cellProps = cell.TableCellProperties ??= new TableCellProperties();
        cellProps.VerticalMerge = new VerticalMerge
        {
            Val = isFirst ? MergedCellValues.Restart : MergedCellValues.Continue
        };

        if (!isFirst)
        {
            foreach (var text in cell.Descendants<Text>())
            {
                text.Text = "";
            }
        }
    }

    /// <summary>
    /// Template có khoảng trắng/NBSP bao quanh placeholder → bỏ để ô căn giữa/căn trái gọn.
    /// </summary>
    private static void TrimParagraphWhitespace(Paragraph para)
    {
        var texts = para.Descendants<Text>().ToList();

        foreach (var text in texts)
        {
            text.Text = text.Text.TrimStart(CellWhitespace);
            if (text.Text.Length > 0) break;
        }

        for (var i = texts.Count - 1; i >= 0; i--)
        {
            texts[i].Text = texts[i].Text.TrimEnd(CellWhitespace);
            if (texts[i].Text.Length > 0) break;
        }
    }
}
