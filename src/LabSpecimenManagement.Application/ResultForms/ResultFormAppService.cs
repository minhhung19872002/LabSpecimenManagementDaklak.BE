using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LabSpecimenManagement.ResultForms;

[Authorize]
public partial class ResultFormAppService : ApplicationService, IResultFormAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<QualityManagementTask, Guid> _qualityTaskRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _qualityIndicatorRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationCreator _notificationCreator;
    private readonly ICurrentUser _currentUser;
    private readonly IPdfConversionService _pdfConversionService;
    private readonly ILogger<ResultFormAppService> _logger;

    // Template constants are defined in TemplateConstants class
    // Use TemplateConstants.StandardBasedTemplates for single indicator templates

    public ResultFormAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<QualityManagementTask, Guid> qualityTaskRepository,
        IRepository<QualityManagementIndicator, Guid> qualityIndicatorRepository,
        IWebHostEnvironment environment,
        INotificationCreator notificationCreator,
        ICurrentUser currentUser,
        IPdfConversionService pdfConversionService,
        ILogger<ResultFormAppService> logger)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _qualityTaskRepository = qualityTaskRepository;
        _qualityIndicatorRepository = qualityIndicatorRepository;
        _environment = environment;
        _notificationCreator = notificationCreator;
        _currentUser = currentUser;
        _pdfConversionService = pdfConversionService;
        _logger = logger;
    }

    public async Task<GenerateResultFormOutput> GenerateResultFormAsync(GenerateResultFormInput input)
    {
        // 1. Load data
        var testRequest = await _testRequestRepository.GetAsync(input.TestRequestId);
        var sample = await _sampleRepository.GetAsync(input.SampleId);

        if (sample.TestRequestId != input.TestRequestId)
        {
            throw new ValidationException("sample", "Sample không thuộc TestRequest này");
        }

        // Get sample indicators
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            x => x.TestRequestSampleId == input.SampleId);

        // Get laboratory tasks for this test request
        var laboratoryTasks = await _laboratoryTaskRepository.GetListAsync(
            x => x.TestRequestId == input.TestRequestId);

        // Get all laboratory task indicators for these tasks
        var taskIds = laboratoryTasks.Select(t => t.Id).ToList();
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => taskIds.Contains(x.LaboratoryTaskId));

        // Filter task indicators that belong to this sample's indicators
        var sampleIndicatorIds = sampleIndicators.Select(si => si.Id).ToHashSet();
        var taskIndicators = allTaskIndicators
            .Where(ti => sampleIndicatorIds.Contains(ti.TestRequestSampleIndicatorId))
            .ToList();

        // Get indicator and method details
        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var indicators = await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id));
        var indicatorDict = indicators.ToDictionary(i => i.Id);

        var methodIds = taskIndicators.Where(ti => ti.MethodId.HasValue).Select(ti => ti.MethodId!.Value).Distinct().ToList();
        var methods = methodIds.Any()
            ? await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id))
            : new List<TestMethod>();
        var methodDict = methods.ToDictionary(m => m.Id);

        // Get test date from laboratory task (use the first one that has TestDate)
        var testDate = laboratoryTasks.Where(t => t.TestDate.HasValue).Select(t => t.TestDate).FirstOrDefault();

        // 2. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", sample.SampleCode ?? "" },
            { "{{CurrentDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },  // Typo in template
            { "{{Address}}", address },  // Correct spelling
            { "{{NameOfSample}}", sample.SampleName ?? "" },
            { "{{DateOfReceiving}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{TimeOfTesting}}", testDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{SampleDescription}}", sample.StatusText ?? "" },
            { "{{Quantity}}", sample.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" }
        };

        // 3. Build table data
        var tableData = new List<ResultFormTableRow>();
        int rowIndex = 1;

        foreach (var sampleIndicator in sampleIndicators)
        {
            var indicator = indicatorDict.GetValueOrDefault(sampleIndicator.IndicatorId);
            var taskIndicator = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == sampleIndicator.Id);

            TestMethod? method = null;
            if (taskIndicator?.MethodId != null)
            {
                method = methodDict.GetValueOrDefault(taskIndicator.MethodId.Value);
            }

            tableData.Add(new ResultFormTableRow
            {
                No = rowIndex++,
                Parameters = indicator?.Name ?? "",
                Unit = taskIndicator?.Unit ?? "",
                TestMethod = method?.Name ?? "",
                Result = ConvertToSuperscriptDisplay(taskIndicator?.Result),
                Standards = "",  // Standards removed from TestMethod entity
                Uncertainty = "",  // TODO: Add Uncertainty field to LaboratoryTaskIndicator if needed
                Proclaim = ""      // TODO: Add Proclaim field to LaboratoryTaskIndicator if needed
            });
        }

        // 4. Generate document
        var templatePath = Path.Combine(_environment.WebRootPath ?? "", "templates", TemplateConstants.TemplateBM03);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", TemplateConstants.TemplateBM03);
        }

        // Create temp directory if not exists
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        // Generate unique filename
        var fileName = $"{Guid.NewGuid():N}_BM03_{sample.SampleCode}.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        // Copy template to temp (use FileStream with FileShare.Read to avoid lock issues)
        using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            sourceStream.CopyTo(destStream);
        }

        // Process document
        ProcessDocument(outputPath, placeholders, tableData);

        // Update TestRequest status to PendingApproval after printing result form
        // Only update if current status is InProgress (replaced TestingCompleted in new flow)
        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await _testRequestRepository.UpdateAsync(testRequest);
        }

        return new GenerateResultFormOutput
        {
            FileName = fileName,
            SampleCode = sample.SampleCode ?? ""
        };
    }

    private string BuildAddress(TestRequest testRequest)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(testRequest.Address)) parts.Add(testRequest.Address);
        if (!string.IsNullOrWhiteSpace(testRequest.Ward)) parts.Add(testRequest.Ward);
        if (!string.IsNullOrWhiteSpace(testRequest.District)) parts.Add(testRequest.District);
        if (!string.IsNullOrWhiteSpace(testRequest.Province)) parts.Add(testRequest.Province);
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Calculate TimeOfTesting from a list of LaboratoryTaskIndicators
    /// Returns: "dd/MM/yyyy - dd/MM/yyyy" where:
    /// - Start date = Min(TestDate) of all indicators
    /// - End date = Max(SubmitDate) of all indicators
    /// </summary>
    private string CalculateTimeOfTesting(List<LaboratoryTaskIndicator> taskIndicators)
    {
        if (taskIndicators == null || !taskIndicators.Any())
        {
            return "";
        }

        // Get Min TestDate (ngày bắt đầu thử nghiệm sớm nhất)
        var minTestDate = taskIndicators
            .Where(ti => ti.TestDate.HasValue)
            .Select(ti => ti.TestDate!.Value)
            .DefaultIfEmpty()
            .Min();

        // Get Max SubmitDate (ngày gửi kết quả muộn nhất)
        var maxSubmitDate = taskIndicators
            .Where(ti => ti.SubmitDate.HasValue)
            .Select(ti => ti.SubmitDate!.Value)
            .DefaultIfEmpty()
            .Max();

        // Format result
        if (minTestDate == default && maxSubmitDate == default)
        {
            return "";
        }
        else if (minTestDate == default)
        {
            return maxSubmitDate.ToString("dd/MM/yyyy");
        }
        else if (maxSubmitDate == default)
        {
            return minTestDate.ToString("dd/MM/yyyy");
        }
        else if (minTestDate.Date == maxSubmitDate.Date)
        {
            // Same date, show only one
            return minTestDate.ToString("dd/MM/yyyy");
        }
        else
        {
            return $"{minTestDate:dd/MM/yyyy} - {maxSubmitDate:dd/MM/yyyy}";
        }
    }

    /// <summary>
    /// Calculate TimeOfTesting for a single LaboratoryTaskIndicator
    /// Returns: "dd/MM/yyyy - dd/MM/yyyy" where:
    /// - Start date = TestDate
    /// - End date = SubmitDate
    /// </summary>
    private string CalculateTimeOfTestingForSingleIndicator(LaboratoryTaskIndicator taskIndicator)
    {
        if (taskIndicator == null)
        {
            return "";
        }

        var testDate = taskIndicator.TestDate;
        var submitDate = taskIndicator.SubmitDate;

        // Format result
        if (!testDate.HasValue && !submitDate.HasValue)
        {
            return "";
        }
        else if (!testDate.HasValue)
        {
            return submitDate!.Value.ToString("dd/MM/yyyy");
        }
        else if (!submitDate.HasValue)
        {
            return testDate.Value.ToString("dd/MM/yyyy");
        }
        else if (testDate.Value.Date == submitDate.Value.Date)
        {
            // Same date, show only one
            return testDate.Value.ToString("dd/MM/yyyy");
        }
        else
        {
            return $"{testDate.Value:dd/MM/yyyy} - {submitDate.Value:dd/MM/yyyy}";
        }
    }

    private void ProcessDocument(string filePath, Dictionary<string, string> placeholders, List<ResultFormTableRow> tableData)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        // Convert floating tables to inline so they flow with content
        // This fixes the issue where "Ghi chú" and footnotes overlay the data table
        ConvertFloatingTablesToInline(document);

        // IMPORTANT: Process table FIRST before replacing other placeholders
        // This ensures the table structure is intact when we find and clone template rows
        ProcessTable(body, tableData);

        // Then replace simple placeholders in all text elements
        ReplacePlaceholdersInBody(body, placeholders);

        document.MainDocumentPart?.Document?.Save();
    }

    private void ReplacePlaceholdersInBody(Body body, Dictionary<string, string> placeholders)
    {
        // First, merge split placeholders in all paragraphs
        foreach (var para in body.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Then replace placeholders
        foreach (var text in body.Descendants<Text>().ToList())
        {
            if (string.IsNullOrEmpty(text.Text)) continue;

            foreach (var placeholder in placeholders)
            {
                if (text.Text.Contains(placeholder.Key))
                {
                    // Check if this is a checkbox placeholder
                    if (placeholder.Key.StartsWith("{{CB_"))
                    {
                        ReplaceCheckboxPlaceholder(text, placeholder.Key, placeholder.Value);
                    }
                    else
                    {
                        // For Note placeholder, remove height constraint on containing cell/row and set font size to 11
                        if (placeholder.Key == "{{Note}}" && !string.IsNullOrEmpty(placeholder.Value))
                        {
                            RemoveContainingCellHeightConstraint(text);
                            SetNoteFontSize(text, 11);
                        }
                        // For Address placeholder, remove height constraint to allow long addresses
                        if ((placeholder.Key == "{{Address}}" || placeholder.Key == "{{Adrress}}") && !string.IsNullOrEmpty(placeholder.Value))
                        {
                            RemoveContainingCellHeightConstraint(text);
                        }
                        text.Text = text.Text.Replace(placeholder.Key, placeholder.Value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Remove fixed height constraint from the table cell and row containing the text element
    /// This allows the cell to auto-expand when content is long
    /// </summary>
    private void RemoveContainingCellHeightConstraint(Text textElement)
    {
        // Find parent TableCell
        var cell = textElement.Ancestors<TableCell>().FirstOrDefault();
        if (cell == null) return;

        // Remove cell height constraint (from TableCellProperties)
        var cellProps = cell.GetFirstChild<TableCellProperties>();
        // Note: TableCellProperties doesn't have height, it's on TableCellVerticalAlignment
        // The height is usually set on TableRowHeight

        // Find parent TableRow and remove its height constraint
        var row = cell.Ancestors<TableRow>().FirstOrDefault();
        if (row != null)
        {
            RemoveRowHeightConstraint(row);
        }
    }

    /// <summary>
    /// Set font size for the Note text element to ensure consistent formatting
    /// Font size in OpenXML is in half-points (e.g., 11pt = 22 half-points)
    /// </summary>
    private void SetNoteFontSize(Text textElement, int fontSizeInPoints)
    {
        var parentRun = textElement.Parent as Run;
        if (parentRun == null) return;

        // Font size in OpenXML is specified in half-points
        var halfPoints = (fontSizeInPoints * 2).ToString();

        var runProps = parentRun.GetFirstChild<RunProperties>();
        if (runProps == null)
        {
            runProps = new RunProperties();
            parentRun.InsertAt(runProps, 0);
        }

        // Remove existing font size if present
        var existingFontSize = runProps.GetFirstChild<FontSize>();
        if (existingFontSize != null)
        {
            existingFontSize.Remove();
        }

        var existingFontSizeCs = runProps.GetFirstChild<FontSizeComplexScript>();
        if (existingFontSizeCs != null)
        {
            existingFontSizeCs.Remove();
        }

        // Set new font size (11pt = 22 half-points)
        runProps.Append(new FontSize { Val = halfPoints });
        runProps.Append(new FontSizeComplexScript { Val = halfPoints });
    }

    /// <summary>
    /// Replace checkbox placeholder with proper Unicode checkbox character
    /// Using Symbola font which renders ballot box characters correctly in PDF
    /// </summary>
    private void ReplaceCheckboxPlaceholder(Text textElement, string placeholder, string value)
    {
        // The value is already the checkbox character (☑ or ☐)
        // We need to replace the placeholder with this character using a font that supports it

        var parentRun = textElement.Parent as Run;
        var originalText = textElement.Text;

        if (parentRun == null || !originalText.Contains(placeholder))
        {
            // Simple fallback - just replace text directly
            textElement.Text = originalText.Replace(placeholder, value);
            return;
        }

        var placeholderIndex = originalText.IndexOf(placeholder);
        var beforeText = originalText.Substring(0, placeholderIndex);
        var afterText = originalText.Substring(placeholderIndex + placeholder.Length);

        // Update original text to before part only
        textElement.Text = beforeText;

        // Create new run with checkbox using Symbola font
        // Symbola has excellent Unicode symbol support including ballot boxes
        var checkboxRun = new Run();
        var runProps = new RunProperties();

        // Use Symbola font - installed in custom Gotenberg Docker image
        runProps.Append(new RunFonts
        {
            Ascii = "Symbola",
            HighAnsi = "Symbola",
            EastAsia = "Symbola",
            ComplexScript = "Symbola"
        });

        // Copy font size from parent run if available
        var parentRunProps = parentRun.GetFirstChild<RunProperties>();
        var parentFontSize = parentRunProps?.GetFirstChild<FontSize>();
        if (parentFontSize != null)
        {
            runProps.Append(parentFontSize.CloneNode(true));
        }

        checkboxRun.Append(runProps);
        checkboxRun.Append(new Text(value) { Space = SpaceProcessingModeValues.Preserve });

        // Insert checkbox run after parent run
        parentRun.InsertAfterSelf(checkboxRun);

        // Create and insert after text run if needed
        if (!string.IsNullOrEmpty(afterText))
        {
            var afterRun = (Run)parentRun.CloneNode(true);
            // Clear all text elements and add new one
            foreach (var t in afterRun.Elements<Text>().ToList())
            {
                t.Remove();
            }
            afterRun.Append(new Text(afterText) { Space = SpaceProcessingModeValues.Preserve });
            checkboxRun.InsertAfterSelf(afterRun);
        }
    }

    private void MergeSplitPlaceholders(Paragraph para)
    {
        var runs = para.Elements<Run>().ToList();
        if (runs.Count <= 1) return;

        // Get full text from all runs
        var fullText = string.Concat(runs.SelectMany(r => r.Elements<Text>()).Select(t => t.Text ?? ""));

        // If no placeholder pattern, skip
        if (!fullText.Contains("{{")) return;

        // Merge all text into first run's first text element
        var firstRun = runs.First();
        var firstText = firstRun.GetFirstChild<Text>();

        if (firstText == null)
        {
            firstText = new Text();
            firstRun.AppendChild(firstText);
        }

        firstText.Text = fullText;
        firstText.Space = SpaceProcessingModeValues.Preserve;

        // Remove text from other runs (but keep runs for formatting)
        for (int i = 1; i < runs.Count; i++)
        {
            foreach (var t in runs[i].Elements<Text>().ToList())
            {
                t.Remove();
            }
        }
    }

    private void ProcessTable(Body body, List<ResultFormTableRow> tableData)
    {
        // Helper to get text from row's direct cells only (not from nested tables)
        string GetDirectRowText(TableRow row)
        {
            var texts = new List<string>();
            foreach (var cell in row.Elements<TableCell>())
            {
                // Get text only from direct paragraphs, not from nested tables
                foreach (var para in cell.Elements<Paragraph>())
                {
                    texts.Add(string.Concat(para.Descendants<Text>().Select(t => t.Text ?? "")));
                }
            }
            return string.Join("", texts);
        }

        // Get ALL tables including nested ones using Descendants
        var allTables = body.Descendants<Table>().ToList();

        // Find the INNERMOST table that contains {{TestTable}} or {{No}} directly in its rows
        // We reverse to check innermost (nested) tables first
        Table? targetTable = null;
        TableRow? templateRow = null;

        foreach (var table in allTables.AsEnumerable().Reverse())
        {
            // Get only direct child rows
            var directRows = table.Elements<TableRow>().ToList();
            foreach (var row in directRows)
            {
                // Check if row has nested tables
                var hasNestedTable = row.Descendants<Table>().Any();

                // Get text from direct cells only
                var rowText = GetDirectRowText(row);

                // Only consider this row if it contains placeholders AND doesn't have nested tables
                if (!hasNestedTable && (rowText.Contains("{{TestTable}}") || rowText.Contains("{{No}}")))
                {
                    targetTable = table;
                    templateRow = row;
                    break;
                }
            }
            if (targetTable != null) break;
        }

        if (targetTable == null || templateRow == null) return;

        // IMPORTANT: Remove fixed height from parent rows that contain the data table
        // This allows the parent cell/row to expand when data table has many rows
        RemoveParentRowHeightConstraints(targetTable);

        // Get all direct rows of target table
        var allRows = targetTable.Elements<TableRow>().ToList();
        var templateRowIndex = allRows.IndexOf(templateRow);

        if (templateRowIndex < 0) return;

        // Clone template row for reuse
        var templateClone = (TableRow)templateRow.CloneNode(true);

        // Remove the template row from the table
        templateRow.Remove();

        // Re-get rows after removal
        allRows = targetTable.Elements<TableRow>().ToList();

        // Determine insert position (after header row)
        TableRow? insertAfterRow = null;
        if (templateRowIndex > 0 && allRows.Count > 0)
        {
            insertAfterRow = allRows[templateRowIndex - 1];
        }

        // Create and insert data rows
        foreach (var data in tableData)
        {
            var newRow = CloneAndFillRow(templateClone, data);

            if (insertAfterRow != null)
            {
                insertAfterRow.InsertAfterSelf(newRow);
                insertAfterRow = newRow;
            }
            else
            {
                // Insert at beginning of table
                var firstRow = targetTable.Elements<TableRow>().FirstOrDefault();
                if (firstRow != null)
                {
                    firstRow.InsertBeforeSelf(newRow);
                }
                else
                {
                    targetTable.AppendChild(newRow);
                }
                insertAfterRow = newRow;
            }
        }
    }

    private TableRow CloneAndFillRow(TableRow template, ResultFormTableRow data)
    {
        // Clone the template row
        var newRow = (TableRow)template.CloneNode(true);

        // Remove fixed row height to allow row to auto-expand with content
        RemoveRowHeightConstraint(newRow);

        // Allow page break inside row for long content
        AllowPageBreakInRow(newRow);

        // Enable text wrapping in all cells to prevent content overflow
        EnableTextWrappingInRow(newRow);

        // IMPORTANT: Merge split placeholders before replacing
        // Word often splits placeholders like {{TestMethod}} into multiple Text elements
        foreach (var para in newRow.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Replace placeholders in all Text elements
        foreach (var text in newRow.Descendants<Text>().ToList())
        {
            if (string.IsNullOrEmpty(text.Text)) continue;

            // Handle Result placeholder specially - it may contain newlines for Note
            if (text.Text.Contains("{{Result}}") && !string.IsNullOrEmpty(data.Result) && data.Result.Contains('\n'))
            {
                ReplaceTextWithLineBreaks(text, "{{Result}}", data.Result);
            }
            else
            {
                text.Text = text.Text
                    .Replace("{{TestTable}}", "")
                    .Replace("{{No}}", data.No.ToString())
                    .Replace("{{NameOfSample}}", data.NameOfSample ?? "") // BM02 - Tên mẫu
                    .Replace("{{CodeOfNumber}}", data.CodeOfNumber ?? "") // BM02 - Mã số mẫu
                    .Replace("{{Parameters}}", data.Parameters ?? "")
                    .Replace("{{Unit}}", data.Unit ?? "")
                    .Replace("{{TestMethod}}", data.TestMethod ?? "")
                    .Replace("{{Result}}", data.Result ?? "")
                    .Replace("{{Standards}}", data.Standards ?? "")
                    .Replace("{{CompareWithStandard}}", data.CompareWithStandard ?? "")
                    .Replace("{{Allowlimit}}", data.Allowlimit ?? "") // BM04 - Giới hạn cho phép
                    .Replace("{{Uncertainty}}", data.Uncertainty ?? "")
                    .Replace("{{Proclaim}}", data.Proclaim ?? "");
            }
        }

        return newRow;
    }

    /// <summary>
    /// Replace placeholder text with content that includes line breaks (for Word documents)
    /// This creates proper <w:br/> elements instead of \n characters
    /// </summary>
    private void ReplaceTextWithLineBreaks(Text textElement, string placeholder, string valueWithNewlines)
    {
        var parentRun = textElement.Parent as Run;
        if (parentRun == null)
        {
            // Fallback - just replace without line breaks
            textElement.Text = textElement.Text.Replace(placeholder, valueWithNewlines.Replace("\n", " "));
            return;
        }

        var originalText = textElement.Text;
        var placeholderIndex = originalText.IndexOf(placeholder);

        if (placeholderIndex < 0)
        {
            return;
        }

        var beforeText = originalText.Substring(0, placeholderIndex);
        var afterText = originalText.Substring(placeholderIndex + placeholder.Length);

        // Split the value by newlines
        var lines = valueWithNewlines.Split('\n');

        // Update original text to before part only
        textElement.Text = beforeText;

        // Get reference to insert elements after
        OpenXmlElement insertAfter = textElement;

        // Add each line with breaks between them
        for (int i = 0; i < lines.Length; i++)
        {
            // Add text
            var lineText = new Text(lines[i]);
            if (lines[i].StartsWith(" ") || lines[i].EndsWith(" "))
            {
                lineText.Space = SpaceProcessingModeValues.Preserve;
            }
            parentRun.InsertAfter(lineText, insertAfter);
            insertAfter = lineText;

            // Add line break (except after the last line)
            if (i < lines.Length - 1)
            {
                var lineBreak = new Break();
                parentRun.InsertAfter(lineBreak, insertAfter);
                insertAfter = lineBreak;
            }
        }

        // Add after text if any
        if (!string.IsNullOrEmpty(afterText))
        {
            var afterTextElement = new Text(afterText);
            if (afterText.StartsWith(" ") || afterText.EndsWith(" "))
            {
                afterTextElement.Space = SpaceProcessingModeValues.Preserve;
            }
            parentRun.InsertAfter(afterTextElement, insertAfter);
        }
    }

    /// <summary>
    /// Remove fixed height constraint from a table row to allow auto-expansion
    /// </summary>
    private void RemoveRowHeightConstraint(TableRow row)
    {
        var rowProps = row.GetFirstChild<TableRowProperties>();
        if (rowProps != null)
        {
            // Remove TableRowHeight element which constrains the row height
            var rowHeight = rowProps.GetFirstChild<TableRowHeight>();
            if (rowHeight != null)
            {
                rowHeight.Remove();
            }
        }
    }

    /// <summary>
    /// Allow page break inside a table row for proper pagination when content is long
    /// This prevents content from being cut off or overlapping with elements below
    /// </summary>
    private void AllowPageBreakInRow(TableRow row)
    {
        var rowProps = row.GetFirstChild<TableRowProperties>();
        if (rowProps == null)
        {
            rowProps = new TableRowProperties();
            row.InsertAt(rowProps, 0);
        }

        // Remove CantSplit if exists (which prevents row from splitting across pages)
        var cantSplit = rowProps.GetFirstChild<CantSplit>();
        if (cantSplit != null)
        {
            cantSplit.Remove();
        }
    }

    /// <summary>
    /// Enable text wrapping in all cells of a table row to prevent content overflow
    /// </summary>
    private void EnableTextWrappingInRow(TableRow row)
    {
        foreach (var cell in row.Elements<TableCell>())
        {
            var cellProps = cell.GetFirstChild<TableCellProperties>();
            if (cellProps == null)
            {
                cellProps = new TableCellProperties();
                cell.InsertAt(cellProps, 0);
            }

            // Remove NoWrap if exists - this enables text wrapping
            var noWrap = cellProps.GetFirstChild<NoWrap>();
            if (noWrap != null)
            {
                noWrap.Remove();
            }
        }
    }

    /// <summary>
    /// Remove fixed height constraints from parent rows that contain the data table.
    /// This is critical because nested tables in Word don't auto-expand their parent cells
    /// when w:trHeight has w:hRule="exact". We need to remove this constraint so the
    /// parent row can grow with the nested data table content.
    /// </summary>
    private void RemoveParentRowHeightConstraints(Table dataTable)
    {
        // Walk up the parent chain to find containing TableCell, then TableRow
        var parent = dataTable.Parent;
        while (parent != null)
        {
            if (parent is TableCell parentCell)
            {
                // Found the cell containing our data table, now find its parent row
                var parentRow = parentCell.Parent as TableRow;
                if (parentRow != null)
                {
                    var rowProps = parentRow.GetFirstChild<TableRowProperties>();
                    if (rowProps != null)
                    {
                        // Remove TableRowHeight with exact rule - this is what causes the fixed height
                        var rowHeight = rowProps.GetFirstChild<TableRowHeight>();
                        if (rowHeight != null)
                        {
                            // Check if it's exact height (hRule="exact")
                            var hRule = rowHeight.HeightType?.Value;
                            if (hRule == HeightRuleValues.Exact)
                            {
                                // Change to auto (atLeast) so it can grow, or remove entirely
                                rowHeight.Remove();
                            }
                        }
                    }
                }
            }
            parent = parent.Parent;
        }
    }

    /// <summary>
    /// Convert floating tables to inline tables so they move with content.
    /// This fixes the issue where tables with "Ghi chú" (notes) are positioned with fixed coordinates
    /// and overlay the main data table when it has many rows.
    /// Floating tables have TablePositionProperties (tblpPr) which we need to remove.
    /// </summary>
    private void ConvertFloatingTablesToInline(WordprocessingDocument doc)
    {
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        // Find all tables with TablePositionProperties (floating tables)
        var tables = body.Descendants<Table>().ToList();

        foreach (var table in tables)
        {
            var tableProps = table.GetFirstChild<TableProperties>();
            if (tableProps == null) continue;

            // Remove TablePositionProperties which makes the table floating
            var positionProps = tableProps.GetFirstChild<TablePositionProperties>();
            if (positionProps != null)
            {
                positionProps.Remove();
            }
        }
    }

    private class ResultFormTableRow
    {
        public int No { get; set; }
        public string NameOfSample { get; set; } = ""; // BM02 - Tên mẫu
        public string CodeOfNumber { get; set; } = ""; // BM02 - Mã số mẫu
        public string Parameters { get; set; } = "";
        public string Unit { get; set; } = "";
        public string TestMethod { get; set; } = "";
        public string Result { get; set; } = "";
        public string Standards { get; set; } = "";
        public string CompareWithStandard { get; set; } = ""; // For BM05-QLNN template
        public string Allowlimit { get; set; } = ""; // For BM04 template - Giới hạn cho phép
        public string Uncertainty { get; set; } = "";
        public string Proclaim { get; set; } = "";
    }

    /// <summary>
    /// Get samples for result form selection with testing status
    /// </summary>
    public async Task<List<SampleForSelectionDto>> GetSamplesForResultFormAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.GetAsync(testRequestId);
        // Filter out samples that are customer cancelled (KH hủy)
        var samples = await _sampleRepository.GetListAsync(x => x.TestRequestId == testRequestId && !x.IsCustomerCancelled);
        var laboratoryTasks = await _laboratoryTaskRepository.GetListAsync(x => x.TestRequestId == testRequestId);

        // Get sample indicator IDs for all samples
        var sampleIds = samples.Select(s => s.Id).ToList();
        var allSampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            x => sampleIds.Contains(x.TestRequestSampleId));

        // Get all task indicators
        var taskIds = laboratoryTasks.Select(t => t.Id).ToList();
        var allTaskIndicators = taskIds.Any()
            ? await _laboratoryTaskIndicatorRepository.GetListAsync(x => taskIds.Contains(x.LaboratoryTaskId))
            : new List<LaboratoryTaskIndicator>();

        var result = new List<SampleForSelectionDto>();

        foreach (var sample in samples.OrderBy(s => s.SortOrder))
        {
            // Get sample's indicators
            var sampleIndicatorIds = allSampleIndicators
                .Where(si => si.TestRequestSampleId == sample.Id)
                .Select(si => si.Id)
                .ToHashSet();

            // Find lab tasks that have indicators for this sample
            var sampleTaskIndicators = allTaskIndicators
                .Where(ti => sampleIndicatorIds.Contains(ti.TestRequestSampleIndicatorId))
                .ToList();

            var relatedTaskIds = sampleTaskIndicators.Select(ti => ti.LaboratoryTaskId).Distinct().ToList();
            var relatedTasks = laboratoryTasks.Where(t => relatedTaskIds.Contains(t.Id)).ToList();

            // Determine testing status based on lab task status
            var testingStatus = GetTestingStatusText(relatedTasks);

            // canGenerateResult should match testingStatus logic (use max status)
            var maxStatus = relatedTasks.Any() ? relatedTasks.Max(t => t.Status) : (LaboratoryTaskStatus?)null;
            var canGenerateResult = maxStatus == LaboratoryTaskStatus.SentToQuality;

            result.Add(new SampleForSelectionDto
            {
                Id = sample.Id,
                SampleCode = sample.SampleCode ?? "",
                SampleName = sample.SampleName ?? "",
                TestingStatus = testingStatus,
                CanGenerateResult = canGenerateResult,
                RequestedCompletionDate = testRequest.ResultDeliveryDate
            });
        }

        return result;
    }

    private string GetTestingStatusText(List<LaboratoryTask> tasks)
    {
        if (!tasks.Any())
            return "Chưa gán phòng";

        // Get the highest status (most progressed)
        var maxStatus = tasks.Max(t => t.Status);

        return maxStatus switch
        {
            LaboratoryTaskStatus.NotAssigned => "Chưa giao việc",
            LaboratoryTaskStatus.Assigned => "Đã giao việc",
            LaboratoryTaskStatus.Testing => "Đang thử nghiệm",
            LaboratoryTaskStatus.PendingApproval => "Chờ duyệt kết quả",
            LaboratoryTaskStatus.Approved => "Đã duyệt",
            LaboratoryTaskStatus.SentToQuality => "Đã gửi QLCL",
            _ => "Không xác định"
        };
    }

    /// <summary>
    /// Generate BM01 result form document for single sample
    /// </summary>
    public async Task<GenerateResultFormOutput> GenerateBM01ResultFormAsync(GenerateBM01ResultFormInput input)
    {
        // 1. Load and validate data
        var testRequest = await _testRequestRepository.GetAsync(input.TestRequestId);
        var sample = await _sampleRepository.GetAsync(input.SampleId);

        if (sample.TestRequestId != input.TestRequestId)
        {
            throw new ValidationException("sample", "Sample không thuộc TestRequest này");
        }

        // Get sample indicators
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            x => x.TestRequestSampleId == input.SampleId);

        // Get laboratory tasks for this test request
        var laboratoryTasks = await _laboratoryTaskRepository.GetListAsync(
            x => x.TestRequestId == input.TestRequestId);

        // Get all laboratory task indicators for these tasks
        var taskIds = laboratoryTasks.Select(t => t.Id).ToList();
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => taskIds.Contains(x.LaboratoryTaskId));

        // Filter task indicators that belong to this sample's indicators
        var sampleIndicatorIds = sampleIndicators.Select(si => si.Id).ToHashSet();
        var taskIndicators = allTaskIndicators
            .Where(ti => sampleIndicatorIds.Contains(ti.TestRequestSampleIndicatorId))
            .ToList();

        // Find related lab tasks for this sample
        var relatedTaskIds = taskIndicators.Select(ti => ti.LaboratoryTaskId).Distinct().ToList();
        var relatedTasks = laboratoryTasks.Where(t => relatedTaskIds.Contains(t.Id)).ToList();

        // Validate: Max status of related tasks must be ResultSent
        if (!relatedTasks.Any())
        {
            throw new Common.Exceptions.InvalidOperationException("Mẫu chưa được gán phòng xét nghiệm", "sample");
        }

        var maxStatus = relatedTasks.Max(t => t.Status);
        if (maxStatus != LaboratoryTaskStatus.SentToQuality)
        {
            throw new Common.Exceptions.InvalidOperationException("Mẫu chưa xét nghiệm xong", "sample");
        }

        // Get indicator and method details
        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var indicators = await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id));
        var indicatorDict = indicators.ToDictionary(i => i.Id);

        var methodIds = taskIndicators.Where(ti => ti.MethodId.HasValue).Select(ti => ti.MethodId!.Value).Distinct().ToList();
        var methods = methodIds.Any()
            ? await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id))
            : new List<TestMethod>();
        var methodDict = methods.ToDictionary(m => m.Id);

        // Calculate TimeOfTesting: Min(TestDate) - Max(SubmitDate) of indicators in this sample
        var timeOfTesting = CalculateTimeOfTesting(taskIndicators);

        // 2. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", sample.SampleCode ?? "" },
            { "{{CurrentDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{NameOfSample}}", sample.SampleName ?? "" },
            { "{{DateOfReceiving}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", sample.StatusText ?? "" },
            { "{{Quantity}}", sample.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", input.Note ?? "" },
            { "{{Conclude}}", "" } // BM05-QLNN - Kết luận (để trống, user tự điền)
        };

        // 3. Build table data (BM01 has fewer columns than BM03)
        var tableData = new List<ResultFormTableRow>();
        int rowIndex = 1;

        foreach (var sampleIndicator in sampleIndicators)
        {
            var indicator = indicatorDict.GetValueOrDefault(sampleIndicator.IndicatorId);
            var taskIndicator = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == sampleIndicator.Id);

            // Skip CustomerCancelled indicators (KH hủy)
            if (taskIndicator?.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
            {
                continue;
            }

            TestMethod? method = null;
            if (taskIndicator?.MethodId != null)
            {
                method = methodDict.GetValueOrDefault(taskIndicator.MethodId.Value);
            }

            tableData.Add(new ResultFormTableRow
            {
                No = rowIndex++,
                Parameters = indicator?.Name ?? "",
                Unit = string.IsNullOrWhiteSpace(taskIndicator?.Unit) ? "-" : taskIndicator.Unit,
                TestMethod = method?.Name ?? "",
                Result = ConvertToSuperscriptDisplay(taskIndicator?.Result),
                // Note: ReferenceLimit is not available in TestRequestSampleIndicator context
                // These fields will be empty for BM01 but templates BM04/BM05-QLNN might need them
                Standards = "",
                CompareWithStandard = "", // BM05-QLNN - So với TCVN/QCVN
                Allowlimit = "", // BM04 - Giới hạn cho phép
                Uncertainty = "",
                Proclaim = ""
            });
        }

        // 4. Generate document using selected template or default BM01
        var templatePath = GetTemplatePath(input.TemplateName, TemplateConstants.TemplateBM01);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", input.TemplateName ?? TemplateConstants.TemplateBM01);
        }

        // Create temp directory if not exists
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        // Generate unique filename - sanitize sample code to remove invalid path characters
        var safeSampleCode = (sample.SampleCode ?? "unknown").Replace("/", "-").Replace("\\", "-");
        var templateName = string.IsNullOrEmpty(input.TemplateName) ? TemplateConstants.DefaultTemplateName : input.TemplateName;
        var fileName = $"{Guid.NewGuid():N}_{templateName}_{safeSampleCode}.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        // Copy template to temp
        using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            sourceStream.CopyTo(destStream);
        }

        // Process document
        ProcessDocument(outputPath, placeholders, tableData);

        // Convert to PDF
        string pdfFileName;
        try
        {
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);
            pdfFileName = Path.GetFileName(pdfPath);

            // Delete temporary DOCX file
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary DOCX file: {Path}", outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DOCX to PDF, returning DOCX instead");
            pdfFileName = fileName;
        }

        // Update TestRequest status to PendingApproval after printing result form
        // Only update if current status is InProgress (replaced TestingCompleted in new flow)
        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await _testRequestRepository.UpdateAsync(testRequest);
        }

        return new GenerateResultFormOutput
        {
            FileName = pdfFileName,
            SampleCode = sample.SampleCode ?? ""
        };
    }

    /// <summary>
    /// Generate test request form document (BM05 - phiếu yêu cầu XN)
    /// This creates a copy of the template for user to edit and print
    /// When printing, status changes from New to Pending (Chờ xử lý)
    /// If already Pending, status remains unchanged (allow reprint)
    /// </summary>
    public async Task<GenerateTestRequestFormOutput> GenerateTestRequestFormAsync(GenerateTestRequestFormInput input)
    {
        // 1. Load test request data with samples
        var testRequest = await _testRequestRepository.GetAsync(input.TestRequestId);
        var samples = await _sampleRepository.GetListAsync(x => x.TestRequestId == input.TestRequestId);

        // 2. Update status to Pending if currently New
        // If already Pending or higher, don't change status (allow reprint)
        if (testRequest.Status == TestRequestStatus.New)
        {
            testRequest.Status = TestRequestStatus.Pending;
            testRequest.PendingDate = Clock.Now;
            testRequest.PendingByUserId = _currentUser.Id;
            testRequest.PendingByUserName = _currentUser.Name ?? _currentUser.UserName;
            await _testRequestRepository.UpdateAsync(testRequest);

            // Send notification to the test request creator
            if (testRequest.CreatorId.HasValue)
            {
                var sampleCount = samples.Count;
                await _notificationCreator.NotifyNewTestRequestPendingAsync(
                    testRequest.CreatorId.Value,
                    testRequest.ContractNumber ?? "",
                    testRequest.Id,
                    sampleCount);
            }
        }

        // 3. Check if template exists
        var templatePath = Path.Combine(_environment.WebRootPath ?? "", "templates", TemplateConstants.TemplateBM05);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", TemplateConstants.TemplateBM05);
        }

        // 4. Create temp directory if not exists
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        // 5. Generate unique filename - keep original extension
        var safeRequestNumber = testRequest.RequestNumber?.Replace("/", "-") ?? "unknown";
        var templateExtension = Path.GetExtension(TemplateConstants.TemplateBM05); // .doc or .docx
        var fileName = $"{Guid.NewGuid():N}_BM05_{safeRequestNumber}{templateExtension}";
        var outputPath = Path.Combine(tempDir, fileName);

        // 6. Copy template to temp
        using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destStream);
        }

        // 7. Process document - replace placeholders
        // Note: OpenXML only supports .docx format
        // For .doc files, we just copy the template and let user edit in OnlyOffice
        _logger.LogInformation("Template extension: {Extension}, processing DOCX: {IsDocx}",
            templateExtension, templateExtension.Equals(".docx", StringComparison.OrdinalIgnoreCase));
        if (templateExtension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            var placeholders = BuildTestRequestFormPlaceholders(testRequest, samples);
            var sampleTableData = await BuildSampleTableDataAsync(samples);
            _logger.LogInformation("Sample table data count: {Count}, first sample indicators: {IndicatorCount}",
                sampleTableData.Count, sampleTableData.FirstOrDefault()?.IndicatorDetails.Count ?? 0);
            ProcessTestRequestFormDocumentWithTable(outputPath, placeholders, sampleTableData);
        }

        // 8. Convert DOCX to PDF using LibreOffice
        string pdfFileName;
        try
        {
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);
            pdfFileName = Path.GetFileName(pdfPath);

            // 9. Delete the DOCX file after successful PDF conversion
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                    _logger.LogInformation("Deleted temporary DOCX file: {Path}", outputPath);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail if cleanup fails
                _logger.LogWarning(ex, "Failed to delete temporary DOCX file: {Path}", outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DOCX to PDF, returning DOCX instead");
            // If PDF conversion fails, return the DOCX file as fallback
            pdfFileName = fileName;
        }

        return new GenerateTestRequestFormOutput
        {
            FileName = pdfFileName,
            RequestNumber = testRequest.RequestNumber ?? "",
            ContractNumber = testRequest.ContractNumber ?? ""
        };
    }

    /// <summary>
    /// Preview test request form with form data (no DB save required)
    /// Used when creating new test request or editing unsaved changes
    /// </summary>
    public async Task<GenerateTestRequestFormOutput> PreviewTestRequestFormAsync(PreviewTestRequestFormInput input)
    {
        // 1. Check if template exists
        var templatePath = Path.Combine(_environment.WebRootPath ?? "", "templates", TemplateConstants.TemplateBM05);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", TemplateConstants.TemplateBM05);
        }

        // 2. Create temp directory if not exists
        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        // 3. Generate unique filename - keep original extension
        var safeContractNumber = input.ContractNumber?.Replace("/", "-") ?? "preview";
        var templateExtension = Path.GetExtension(TemplateConstants.TemplateBM05);
        var fileName = $"{Guid.NewGuid():N}_BM05_Preview_{safeContractNumber}{templateExtension}";
        var outputPath = Path.Combine(tempDir, fileName);

        // 4. Copy template to temp
        using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destStream);
        }

        // 5. Process document - replace placeholders
        if (templateExtension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            var placeholders = BuildPreviewFormPlaceholders(input);
            var sampleTableData = BuildPreviewSampleTableData(input.Samples);
            ProcessTestRequestFormDocumentWithTable(outputPath, placeholders, sampleTableData);
        }

        // 6. Convert DOCX to PDF using LibreOffice
        string pdfFileName;
        try
        {
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);
            pdfFileName = Path.GetFileName(pdfPath);

            // 7. Delete the DOCX file after successful PDF conversion
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                    _logger.LogInformation("Deleted temporary DOCX file: {Path}", outputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary DOCX file: {Path}", outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DOCX to PDF, returning DOCX instead");
            pdfFileName = fileName;
        }

        return new GenerateTestRequestFormOutput
        {
            FileName = pdfFileName,
            RequestNumber = "Preview",
            ContractNumber = input.ContractNumber ?? ""
        };
    }

    /// <summary>
    /// Build placeholders for preview form (from input DTO, not from DB entity)
    /// </summary>
    private Dictionary<string, string> BuildPreviewFormPlaceholders(PreviewTestRequestFormInput input)
    {
        var address = input.Address ?? "";
        var wardText = !string.IsNullOrWhiteSpace(input.Ward) ? input.Ward : "";
        var districtText = !string.IsNullOrWhiteSpace(input.District) ? input.District : "";
        var provinceText = !string.IsNullOrWhiteSpace(input.Province) ? input.Province : "";
        var resultCopies = input.ResultCopyCount ?? "";
        var customerTestMethod = input.MethodCustomerDetail ?? "";
        var compareStandard = input.ConformityDeclaration ?? "";
        var otherRequirements = input.OtherRequirements ?? "";
        var subContractInfo = input.SubContract ?? "";
        var currentDate = DateTime.Now;
        var dateReceiving = input.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "";
        var totalAmount = input.TotalCost.ToString("N0");
        var totalByText = input.TotalCostInWords ?? ConvertNumberToVietnameseWords(input.TotalCost);
        var receiptNumber = input.ReceiptNumber ?? "";
        var currentDateText = currentDate.ToString("dd/MM/yyyy");

        var placeholders = new Dictionary<string, string>
        {
            { "{{ContractNo}}", input.ContractNumber ?? "" },
            { "{{Customer}}", input.CustomerName },
            { "{{Adress}}", address },
            { "{{Address}}", address },
            { "{{Ward}}", wardText },
            { "{{District}}", districtText },
            { "{{Province}}", provinceText },
            { "{{TaxNo}}", input.TaxCode ?? "" },
            { "{{ContactPerson}}", input.ContactPerson ?? "" },
            { "{{PhoneNumber}}", input.Phone ?? "" },
            { "{{SampleTotal}}", input.TotalSampleCount.ToString() },
            { "{{ResultCopies}}", resultCopies },
            { "{{CertifiedCopies}}", resultCopies },
            { "{{CustomerTestMethod}}", customerTestMethod },
            { "{{CompareStandard}}", compareStandard },
            { "{{OtherRequirements}}", otherRequirements },
            { "{{SubContractInfo}}", subContractInfo },
            { "{{DateReceiving}}", dateReceiving },
            { "{{TotalAmount}}", totalAmount },
            { "{{TotalByText}}", totalByText },
            { "{{ReceiptNumberTTDV}}", receiptNumber },
            { "{{CurrentDate}}", currentDateText },

            // Checkbox - Test Method
            { "{{CB_TestMethod_Center}}", input.MethodWestHighlandInstitute ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_TestMethod_Customer}}", input.MethodCustomer ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Report Language
            { "{{CB_Report_VN}}", input.ResultLanguage == ResultLanguage.Vietnamese ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Report_EN}}", input.ResultLanguage == ResultLanguage.English ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Report_Both}}", input.ResultLanguage == ResultLanguage.VietnameseAndEnglish ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Separate Results
            { "{{CB_SeparateResult}}", input.SeparatePassedResults ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Sampling Requirement
            { "{{CB_Sampling_Center}}", input.SamplingRequirement == SamplingRequirement.LabSampling ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Sampling_Customer}}", input.SamplingRequirement == SamplingRequirement.CustomerSampling ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Delivery Method
            { "{{CB_Delivery_Direct}}", input.DeliveryDirectPickup ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Delivery_Post}}", input.DeliveryPostal ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Return Sample
            { "{{CB_ReturnSample}}", input.ReturnStoredSample ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Agree Terms
            { "{{CB_AgreeTerms}}", input.AgreeTerms ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Sample Type
            { "{{CB_Service}}", input.SampleTypeService ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Contract}}", input.SampleTypeContract ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_QLNN}}", input.SampleTypeTask ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_SubContract}}", input.SampleTypeSubContract ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Sample Storage
            { "{{CB_Save_No}}", input.SampleStorage == SampleStorageOption.No ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Save_Yes}}", input.SampleStorage == SampleStorageOption.Yes ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Payment Method (disabled)
            { "{{CB_Cash}}", TemplateConstants.UncheckedBox },
            { "{{CB_Credit}}", TemplateConstants.UncheckedBox }
        };

        return placeholders;
    }

    /// <summary>
    /// Build sample table data from preview DTO (no DB lookup)
    /// </summary>
    private List<TestRequestSampleTableRow> BuildPreviewSampleTableData(List<PreviewTestRequestSampleDto> samples)
    {
        var tableData = new List<TestRequestSampleTableRow>();
        int rowIndex = 1;

        foreach (var sample in samples.OrderBy(s => s.SortOrder))
        {
            var indicatorDetails = new List<IndicatorSubTableRow>();
            int subRowIndex = 1;

            foreach (var indicator in sample.Indicators)
            {
                indicatorDetails.Add(new IndicatorSubTableRow
                {
                    No = subRowIndex++,
                    IndicatorName = indicator.IndicatorName,
                    UnitPrice = indicator.UnitPrice.ToString("N0")
                });
            }

            var indicatorNames = sample.Indicators
                .Select(i => i.IndicatorName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
            var indicatorsText = indicatorNames.Count > 0
                ? string.Join("\n", indicatorNames.Select(name => $"- {name}"))
                : "";

            tableData.Add(new TestRequestSampleTableRow
            {
                No = rowIndex++,
                SampleName = sample.SampleName ?? "",
                Quantity = sample.SampleQuantity ?? "",
                SampleInfo = sample.StatusText ?? "",
                Indicators = indicatorsText,
                IndicatorDetails = indicatorDetails
            });
        }

        return tableData;
    }

    /// <summary>
    /// Build sample table data for test request form
    /// </summary>
    private async Task<List<TestRequestSampleTableRow>> BuildSampleTableDataAsync(List<TestRequestSample> samples)
    {
        var tableData = new List<TestRequestSampleTableRow>();
        int rowIndex = 1;

        // Get all sample indicator IDs
        var sampleIds = samples.Select(s => s.Id).ToList();
        var allSampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            x => sampleIds.Contains(x.TestRequestSampleId));

        // Get indicator details
        var indicatorIds = allSampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var indicators = indicatorIds.Any()
            ? await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id))
            : new List<Indicator>();
        var indicatorDict = indicators.ToDictionary(i => i.Id);

        foreach (var sample in samples.OrderBy(s => s.SortOrder))
        {
            // Get sample indicators for this sample with their prices
            var sampleIndicatorList = allSampleIndicators
                .Where(si => si.TestRequestSampleId == sample.Id)
                .ToList();

            // Build indicator details for sub-table
            var indicatorDetails = new List<IndicatorSubTableRow>();
            int subRowIndex = 1;
            foreach (var si in sampleIndicatorList)
            {
                var indicator = indicatorDict.GetValueOrDefault(si.IndicatorId);
                if (indicator != null)
                {
                    indicatorDetails.Add(new IndicatorSubTableRow
                    {
                        No = subRowIndex++,
                        IndicatorName = indicator.Name ?? "",
                        UnitPrice = si.UnitPrice.ToString("N0")
                    });
                }
            }

            // Format indicators as list with - prefix and newline (for fallback/old template)
            var indicatorNames = sampleIndicatorList
                .Select(si => indicatorDict.GetValueOrDefault(si.IndicatorId)?.Name ?? "")
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
            var indicatorsText = indicatorNames.Count > 0
                ? string.Join("\n", indicatorNames.Select(name => $"- {name}"))
                : "";

            tableData.Add(new TestRequestSampleTableRow
            {
                No = rowIndex++,
                SampleName = sample.SampleName ?? "",
                Quantity = sample.SampleQuantity ?? "",
                SampleInfo = sample.StatusText ?? "",
                Indicators = indicatorsText,
                IndicatorDetails = indicatorDetails
            });
        }

        return tableData;
    }

    /// <summary>
    /// Process test request form document with sample table
    /// </summary>
    private void ProcessTestRequestFormDocumentWithTable(string filePath, Dictionary<string, string> placeholders, List<TestRequestSampleTableRow> sampleTableData)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var mainPart = document.MainDocumentPart;
        if (mainPart?.Document?.Body == null) return;

        var body = mainPart.Document.Body;

        // Process sample table first
        ProcessSampleTable(body, sampleTableData);

        // Replace placeholders in entire document (including textboxes, headers, footers)
        ReplacePlaceholdersInDocument(mainPart, placeholders);

        mainPart.Document.Save();
    }

    /// <summary>
    /// Replace placeholders in entire document including textboxes
    /// </summary>
    private void ReplacePlaceholdersInDocument(MainDocumentPart mainPart, Dictionary<string, string> placeholders)
    {
        // Process main body
        if (mainPart.Document?.Body != null)
        {
            ReplacePlaceholdersInElement(mainPart.Document.Body, placeholders);
        }

        // Process headers
        foreach (var headerPart in mainPart.HeaderParts)
        {
            if (headerPart.Header != null)
            {
                ReplacePlaceholdersInElement(headerPart.Header, placeholders);
            }
        }

        // Process footers
        foreach (var footerPart in mainPart.FooterParts)
        {
            if (footerPart.Footer != null)
            {
                ReplacePlaceholdersInElement(footerPart.Footer, placeholders);
            }
        }
    }

    /// <summary>
    /// Replace placeholders in any OpenXmlElement (handles textboxes, nested elements)
    /// When placeholder has a value, also remove surrounding dots (......) to save space
    /// </summary>
    private void ReplacePlaceholdersInElement(OpenXmlElement element, Dictionary<string, string> placeholders)
    {
        // First, merge split placeholders in all paragraphs
        foreach (var para in element.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Process each paragraph to handle dots across multiple text runs
        foreach (var para in element.Descendants<Paragraph>())
        {
            ReplacePlaceholdersInParagraphWithDotRemoval(para, placeholders);
        }
    }

    // Characters to trim: regular dot (.), Unicode ellipsis (…), and full-width dot (．)
    private static readonly char[] DotCharacters = new[] { '.', '…', '．', '·', '•' };

    /// <summary>
    /// Replace placeholders in a paragraph, removing surrounding dots when placeholder has value
    /// This handles cases where dots and placeholders are in separate text runs
    /// </summary>
    private void ReplacePlaceholdersInParagraphWithDotRemoval(Paragraph para, Dictionary<string, string> placeholders)
    {
        var textElements = para.Descendants<Text>().ToList();
        if (!textElements.Any()) return;

        foreach (var placeholder in placeholders)
        {
            // Find the text element containing this placeholder
            var targetText = textElements.FirstOrDefault(t => t.Text?.Contains(placeholder.Key) == true);
            if (targetText == null) continue;

            // Replace the placeholder
            targetText.Text = targetText.Text.Replace(placeholder.Key, placeholder.Value);

            // If placeholder has value, remove surrounding dots from adjacent text elements
            if (!string.IsNullOrWhiteSpace(placeholder.Value))
            {
                var targetIndex = textElements.IndexOf(targetText);

                // Remove trailing dots from previous text element(s)
                for (int i = targetIndex - 1; i >= 0; i--)
                {
                    var prevText = textElements[i];
                    if (string.IsNullOrEmpty(prevText.Text)) continue;

                    var trimmed = TrimEndDots(prevText.Text);
                    if (trimmed.Length < prevText.Text.Length)
                    {
                        prevText.Text = trimmed;
                        // If we removed dots and there's still content, stop
                        if (!string.IsNullOrEmpty(trimmed)) break;
                    }
                    else
                    {
                        // No dots found, stop looking
                        break;
                    }
                }

                // Remove leading dots from next text element(s)
                for (int i = targetIndex + 1; i < textElements.Count; i++)
                {
                    var nextText = textElements[i];
                    if (string.IsNullOrEmpty(nextText.Text)) continue;

                    var trimmed = TrimStartDots(nextText.Text);
                    if (trimmed.Length < nextText.Text.Length)
                    {
                        nextText.Text = trimmed;
                        // If we removed dots and there's still content, stop
                        if (!string.IsNullOrEmpty(trimmed)) break;
                    }
                    else
                    {
                        // No dots found, stop looking
                        break;
                    }
                }

                // Also check for dots within the same text element (before and after the replaced value)
                var currentText = targetText.Text;
                if (!string.IsNullOrEmpty(currentText))
                {
                    // Find position of the replacement value
                    var valueIndex = currentText.IndexOf(placeholder.Value);
                    if (valueIndex >= 0)
                    {
                        var before = TrimEndDots(currentText.Substring(0, valueIndex));
                        var after = TrimStartDots(currentText.Substring(valueIndex + placeholder.Value.Length));
                        targetText.Text = before + placeholder.Value + after;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Trim dots from end of string (handles regular dots and Unicode dots)
    /// </summary>
    private static string TrimEndDots(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.TrimEnd(DotCharacters);
    }

    /// <summary>
    /// Trim dots from start of string (handles regular dots and Unicode dots)
    /// </summary>
    private static string TrimStartDots(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.TrimStart(DotCharacters);
    }

    /// <summary>
    /// Process sample table in test request form
    /// </summary>
    private void ProcessSampleTable(Body body, List<TestRequestSampleTableRow> tableData)
    {
        // Helper to get text from row's direct cells only
        string GetDirectRowText(TableRow row)
        {
            var texts = new List<string>();
            foreach (var cell in row.Elements<TableCell>())
            {
                foreach (var para in cell.Elements<Paragraph>())
                {
                    texts.Add(string.Concat(para.Descendants<Text>().Select(t => t.Text ?? "")));
                }
            }
            return string.Join("", texts);
        }

        // Find the table containing sample row placeholders
        var allTables = body.Descendants<Table>().ToList();
        Table? targetTable = null;
        TableRow? templateRow = null;

        foreach (var table in allTables.AsEnumerable().Reverse())
        {
            var directRows = table.Elements<TableRow>().ToList();
            foreach (var row in directRows)
            {
                var rowText = GetDirectRowText(row);

                // Look for sample table markers - allow rows with or without nested tables
                if (rowText.Contains("{{TestRequestTable}}") || rowText.Contains("{{NameOfSample}}"))
                {
                    targetTable = table;
                    templateRow = row;
                    _logger.LogInformation("Found template row with text: {RowText}, has nested table: {HasNested}",
                        rowText.Substring(0, Math.Min(100, rowText.Length)),
                        row.Descendants<Table>().Any());
                    break;
                }
            }
            if (targetTable != null) break;
        }

        if (targetTable == null || templateRow == null) return;

        var allRows = targetTable.Elements<TableRow>().ToList();
        var templateRowIndex = allRows.IndexOf(templateRow);
        if (templateRowIndex < 0) return;

        // Clone template row
        var templateClone = (TableRow)templateRow.CloneNode(true);
        templateRow.Remove();

        // Re-get rows after removal
        allRows = targetTable.Elements<TableRow>().ToList();

        // Determine insert position
        TableRow? insertAfterRow = null;
        if (templateRowIndex > 0 && allRows.Count > 0)
        {
            insertAfterRow = allRows[templateRowIndex - 1];
        }

        // Create and insert data rows
        foreach (var data in tableData)
        {
            var newRow = CloneAndFillSampleRow(templateClone, data);

            if (insertAfterRow != null)
            {
                insertAfterRow.InsertAfterSelf(newRow);
                insertAfterRow = newRow;
            }
            else
            {
                var firstRow = targetTable.Elements<TableRow>().FirstOrDefault();
                if (firstRow != null)
                {
                    firstRow.InsertBeforeSelf(newRow);
                }
                else
                {
                    targetTable.AppendChild(newRow);
                }
                insertAfterRow = newRow;
            }
        }
    }

    /// <summary>
    /// Clone and fill sample row with data
    /// </summary>
    private TableRow CloneAndFillSampleRow(TableRow template, TestRequestSampleTableRow data)
    {
        var newRow = (TableRow)template.CloneNode(true);

        // First, merge split placeholders in all paragraphs within the row
        foreach (var para in newRow.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Check if there's a nested sub-table for indicators
        var nestedTable = newRow.Descendants<Table>().FirstOrDefault();
        _logger.LogInformation("Looking for nested table in row. Found: {Found}, IndicatorDetails count: {Count}",
            nestedTable != null, data.IndicatorDetails.Count);
        if (nestedTable != null && data.IndicatorDetails.Count > 0)
        {
            _logger.LogInformation("Processing nested sub-table with {Count} indicators", data.IndicatorDetails.Count);
            ProcessIndicatorSubTable(nestedTable, data.IndicatorDetails);
        }

        // Then replace placeholders in the main row (excluding nested table which is already processed)
        foreach (var text in newRow.Descendants<Text>().ToList())
        {
            if (string.IsNullOrEmpty(text.Text)) continue;

            // Skip if this text is inside a nested table (already processed)
            if (text.Ancestors<Table>().Count() > 0 && nestedTable != null)
            {
                // Check if this text is inside the nested table
                var parentTable = text.Ancestors<Table>().FirstOrDefault();
                if (parentTable == nestedTable) continue;
            }

            // Handle {{SampleIndicators}} separately - need to insert line breaks (fallback for old template)
            if (text.Text.Contains("{{SampleIndicators}}"))
            {
                ReplaceTextWithLineBreaks(text, "{{SampleIndicators}}", data.Indicators);
            }
            else
            {
                text.Text = text.Text
                    .Replace("{{TestRequestTable}}", "")
                    .Replace("{{No}}", data.No.ToString())
                    .Replace("{{NameOfSample}}", data.SampleName)
                    .Replace("{{Quantity}}", data.Quantity)
                    .Replace("{{SampleInfo}}", data.SampleInfo);
            }
        }

        return newRow;
    }

    /// <summary>
    /// Process indicator sub-table inside sample row
    /// Handles {{SubTable}}{{SubNo}}, {{SubIndicator}}, {{SubPrice}} placeholders
    /// </summary>
    private void ProcessIndicatorSubTable(Table subTable, List<IndicatorSubTableRow> indicatorDetails)
    {
        // First, merge split placeholders in all paragraphs within the sub-table
        foreach (var para in subTable.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Find the template row containing {{SubTable}} or {{SubNo}}
        TableRow? templateRow = null;
        foreach (var row in subTable.Elements<TableRow>())
        {
            var rowText = string.Concat(row.Descendants<Text>().Select(t => t.Text ?? ""));
            _logger.LogInformation("Sub-table row text: {RowText}", rowText);
            if (rowText.Contains("{{SubTable}}") || rowText.Contains("{{SubNo}}") || rowText.Contains("{{SubIndicator}}"))
            {
                templateRow = row;
                _logger.LogInformation("Found template row in sub-table");
                break;
            }
        }

        if (templateRow == null)
        {
            _logger.LogWarning("No template row found in sub-table. Available rows: {RowCount}", subTable.Elements<TableRow>().Count());
            return;
        }

        // Clone template row and remove original
        var templateClone = (TableRow)templateRow.CloneNode(true);
        var templateRowParent = templateRow.Parent;
        var templateRowIndex = templateRowParent?.Elements<TableRow>().ToList().IndexOf(templateRow) ?? 0;
        templateRow.Remove();

        // Get all rows after removal to determine insert position
        var allRows = subTable.Elements<TableRow>().ToList();
        TableRow? insertAfterRow = templateRowIndex > 0 && allRows.Count > 0
            ? allRows[Math.Min(templateRowIndex - 1, allRows.Count - 1)]
            : null;

        // Create and insert data rows for each indicator
        foreach (var indicator in indicatorDetails)
        {
            var newRow = (TableRow)templateClone.CloneNode(true);

            // Merge split placeholders
            foreach (var para in newRow.Descendants<Paragraph>())
            {
                MergeSplitPlaceholders(para);
            }

            // Replace placeholders in the new row
            foreach (var text in newRow.Descendants<Text>().ToList())
            {
                if (string.IsNullOrEmpty(text.Text)) continue;

                text.Text = text.Text
                    .Replace("{{SubTable}}", "")
                    .Replace("{{SubNo}}", indicator.No.ToString())
                    .Replace("{{SubIndicator}}", indicator.IndicatorName)
                    .Replace("{{SubPrice}}", indicator.UnitPrice);
            }

            // Insert the new row
            if (insertAfterRow != null)
            {
                insertAfterRow.InsertAfterSelf(newRow);
                insertAfterRow = newRow;
            }
            else
            {
                var firstRow = subTable.Elements<TableRow>().FirstOrDefault();
                if (firstRow != null)
                {
                    firstRow.InsertBeforeSelf(newRow);
                }
                else
                {
                    subTable.AppendChild(newRow);
                }
                insertAfterRow = newRow;
            }
        }
    }

    /// <summary>
    /// Replace placeholder with text containing line breaks (using Word's <w:br/> element)
    /// </summary>
    private void ReplaceTextWithLineBreaks(Text textElement, string placeholder, string value, bool alignLeft = true)
    {
        if (string.IsNullOrEmpty(value))
        {
            textElement.Text = textElement.Text.Replace(placeholder, "");
            return;
        }

        var originalText = textElement.Text;
        var placeholderIndex = originalText.IndexOf(placeholder);
        if (placeholderIndex < 0) return;

        // Text before placeholder
        var beforeText = originalText.Substring(0, placeholderIndex);
        // Text after placeholder
        var afterText = originalText.Substring(placeholderIndex + placeholder.Length);

        // Split value by newline
        var lines = value.Split('\n');

        // Get the parent Run
        var parentRun = textElement.Parent as Run;
        if (parentRun == null)
        {
            // Fallback: just replace with plain text
            textElement.Text = originalText.Replace(placeholder, value.Replace("\n", " "));
            return;
        }

        // Set paragraph alignment to left if requested
        if (alignLeft)
        {
            var paragraph = parentRun.Ancestors<Paragraph>().FirstOrDefault();
            if (paragraph != null)
            {
                var pPr = paragraph.GetFirstChild<ParagraphProperties>();
                if (pPr == null)
                {
                    pPr = new ParagraphProperties();
                    paragraph.InsertAt(pPr, 0);
                }
                // Set justification to left
                var justification = pPr.GetFirstChild<Justification>();
                if (justification != null)
                {
                    justification.Val = JustificationValues.Left;
                }
                else
                {
                    pPr.AppendChild(new Justification() { Val = JustificationValues.Left });
                }
            }
        }

        // Set text before placeholder
        textElement.Text = beforeText + lines[0];

        // Insert remaining lines with line breaks after the current text element
        OpenXmlElement insertAfter = textElement;
        for (int i = 1; i < lines.Length; i++)
        {
            // Insert a line break
            var lineBreak = new Break();
            insertAfter.InsertAfterSelf(lineBreak);
            insertAfter = lineBreak;

            // Insert the text
            var newText = new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve };
            insertAfter.InsertAfterSelf(newText);
            insertAfter = newText;
        }

        // Add text after placeholder (if any)
        if (!string.IsNullOrEmpty(afterText))
        {
            var afterTextElement = new Text(afterText) { Space = SpaceProcessingModeValues.Preserve };
            insertAfter.InsertAfterSelf(afterTextElement);
        }
    }

    /// <summary>
    /// Sample table row data for test request form
    /// </summary>
    private class TestRequestSampleTableRow
    {
        public int No { get; set; }
        public string SampleName { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string SampleInfo { get; set; } = "";
        public string Indicators { get; set; } = "";
        public List<IndicatorSubTableRow> IndicatorDetails { get; set; } = new();
    }

    /// <summary>
    /// Indicator sub-table row data for nested table in test request form
    /// </summary>
    private class IndicatorSubTableRow
    {
        public int No { get; set; }
        public string IndicatorName { get; set; } = "";
        public string UnitPrice { get; set; } = "";
    }

    /// <summary>
    /// Build placeholder dictionary for test request form (BM05)
    /// </summary>
    private Dictionary<string, string> BuildTestRequestFormPlaceholders(TestRequest testRequest, List<TestRequestSample> samples)
    {
        // Address = chỉ lấy địa chỉ người dùng nhập, không nối tỉnh/huyện/xã
        var address = testRequest.Address ?? "";

        // Build address parts - thêm khoảng trắng phía sau để tránh bị sát với text khác
        var wardText = !string.IsNullOrWhiteSpace(testRequest.Ward) ? testRequest.Ward : "";
        var districtText = !string.IsNullOrWhiteSpace(testRequest.District) ? testRequest.District : "";
        var provinceText = !string.IsNullOrWhiteSpace(testRequest.Province) ? testRequest.Province : "";

        // Calculate certified copies (KQTN yêu cầu - giấy chứng nhận)
        var resultCopies = testRequest.ResultCopyCount ?? "";

        // Get customer test method detail
        var customerTestMethod = testRequest.MethodCustomerDetail ?? "";

        // Compare standard / conformity declaration
        var compareStandard = testRequest.ConformityDeclaration ?? "";

        // Other requirements
        var otherRequirements = testRequest.OtherRequirements ?? "";

        // Subcontract info
        var subContractInfo = testRequest.SubContract ?? "";

        // Format dates
        var currentDate = DateTime.Now;
        // DateReceiving = Ngày trả kết quả (ResultDeliveryDate), không phải RequestDate
        var dateReceiving = testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "";

        // Total amount formatting
        var totalAmount = testRequest.TotalCost.ToString("N0");
        var totalByText = testRequest.TotalCostInWords ?? ConvertNumberToVietnameseWords(testRequest.TotalCost);

        // Receipt number
        var receiptNumber = testRequest.ReceiptNumber ?? "";

        // Current date formatted - DD/MM/YYYY format
        var currentDateText = currentDate.ToString("dd/MM/yyyy");

        var placeholders = new Dictionary<string, string>
        {
            // Text fields
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName },
            { "{{Adress}}", address },  // Note: template may have typo
            { "{{Address}}", address },
            { "{{Ward}}", wardText },
            { "{{District}}", districtText },
            { "{{Province}}", provinceText },
            { "{{TaxNo}}", testRequest.TaxCode ?? "" },
            { "{{ContactPerson}}", testRequest.ContactPerson ?? "" },
            { "{{PhoneNumber}}", testRequest.Phone ?? "" },
            { "{{SampleTotal}}", testRequest.TotalSampleCount.ToString() },
            { "{{ResultCopies}}", resultCopies },
            { "{{CertifiedCopies}}", resultCopies }, // Same as ResultCopies
            { "{{CustomerTestMethod}}", customerTestMethod },
            { "{{CompareStandard}}", compareStandard },
            { "{{OtherRequirements}}", otherRequirements },
            { "{{SubContractInfo}}", subContractInfo },
            { "{{DateReceiving}}", dateReceiving },
            { "{{TotalAmount}}", totalAmount },
            { "{{TotalByText}}", totalByText },
            { "{{ReceiptNumberTTDV}}", receiptNumber },
            { "{{CurrentDate}}", currentDateText },

            // Checkbox - Test Method
            { "{{CB_TestMethod_Center}}", testRequest.MethodWestHighlandInstitute ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_TestMethod_Customer}}", testRequest.MethodCustomer ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Report Language
            { "{{CB_Report_VN}}", testRequest.ResultLanguage == ResultLanguage.Vietnamese ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Report_EN}}", testRequest.ResultLanguage == ResultLanguage.English ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Report_Both}}", testRequest.ResultLanguage == ResultLanguage.VietnameseAndEnglish ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Separate Results
            { "{{CB_SeparateResult}}", testRequest.SeparatePassedResults ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Sampling Requirement
            { "{{CB_Sampling_Center}}", testRequest.SamplingRequirement == SamplingRequirement.LabSampling ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Sampling_Customer}}", testRequest.SamplingRequirement == SamplingRequirement.CustomerSampling ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Delivery Method
            { "{{CB_Delivery_Direct}}", testRequest.DeliveryDirectPickup ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Delivery_Post}}", testRequest.DeliveryPostal ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Return Sample
            { "{{CB_ReturnSample}}", testRequest.ReturnStoredSample ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Agree Terms
            { "{{CB_AgreeTerms}}", testRequest.AgreeTerms ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Sample Type
            { "{{CB_Service}}", testRequest.SampleTypeService ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Contract}}", testRequest.SampleTypeContract ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_QLNN}}", testRequest.SampleTypeTask ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_SubContract}}", testRequest.SampleTypeSubContract ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Sample Storage
            { "{{CB_Save_No}}", testRequest.SampleStorage == SampleStorageOption.No ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },
            { "{{CB_Save_Yes}}", testRequest.SampleStorage == SampleStorageOption.Yes ? TemplateConstants.CheckedBox : TemplateConstants.UncheckedBox },

            // Checkbox - Payment Method (disabled - paymentReceived field is not used in form)
            { "{{CB_Cash}}", TemplateConstants.UncheckedBox },
            { "{{CB_Credit}}", TemplateConstants.UncheckedBox }
        };

        return placeholders;
    }

    /// <summary>
    /// Process test request form document and replace placeholders
    /// </summary>
    private void ProcessTestRequestFormDocument(string filePath, Dictionary<string, string> placeholders)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null) return;

        // Replace placeholders in body
        ReplacePlaceholdersInBody(body, placeholders);

        document.MainDocumentPart?.Document?.Save();
    }

    /// <summary>
    /// Convert number to Vietnamese words (simplified version)
    /// </summary>
    private string ConvertNumberToVietnameseWords(decimal number)
    {
        if (number == 0) return "Không đồng";

        var intPart = (long)number;
        if (intPart == 0) return "Không đồng";

        var result = ConvertLongToVietnameseWords(intPart);
        return $"{result} đồng";
    }

    private string ConvertLongToVietnameseWords(long number)
    {
        if (number == 0) return "không";

        var units = new[] { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        var result = "";

        // Billions (tỷ)
        if (number >= 1_000_000_000)
        {
            result += ConvertLongToVietnameseWords(number / 1_000_000_000) + " tỷ ";
            number %= 1_000_000_000;
        }

        // Millions (triệu)
        if (number >= 1_000_000)
        {
            result += ConvertLongToVietnameseWords(number / 1_000_000) + " triệu ";
            number %= 1_000_000;
        }

        // Thousands (nghìn)
        if (number >= 1000)
        {
            result += ConvertLongToVietnameseWords(number / 1000) + " nghìn ";
            number %= 1000;
        }

        // Hundreds (trăm)
        if (number >= 100)
        {
            result += units[number / 100] + " trăm ";
            number %= 100;
        }

        // Tens and units
        if (number >= 10)
        {
            if (number >= 20)
            {
                result += units[number / 10] + " mươi ";
                number %= 10;
                if (number == 1) result += "mốt";
                else if (number == 5) result += "lăm";
                else if (number > 0) result += units[number];
            }
            else if (number >= 10)
            {
                result += "mười ";
                number %= 10;
                if (number == 5) result += "lăm";
                else if (number > 0) result += units[number];
            }
        }
        else if (number > 0)
        {
            result += units[number];
        }

        return result.Trim();
    }

    /// <summary>
    /// Generate single indicator result form (In KQ Đơn) for Quality Management
    /// Uses BM01 template for single indicator results
    /// </summary>
    public async Task<GenerateResultFormOutput> GenerateSingleIndicatorResultFormAsync(GenerateSingleIndicatorInput input)
    {
        // 1. Load quality task and indicator
        var qualityTask = await _qualityTaskRepository.GetAsync(input.TaskId);
        var qualityIndicator = await _qualityIndicatorRepository.GetAsync(input.IndicatorId);

        if (qualityIndicator.QualityManagementTaskId != input.TaskId)
        {
            throw new ValidationException("indicator", "Chỉ tiêu không thuộc công việc này");
        }

        if (qualityIndicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt", "indicator");
        }

        // 2. Get laboratory task indicator and related data
        var labTaskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(qualityIndicator.LaboratoryTaskIndicatorId);
        var labTask = await _laboratoryTaskRepository.GetAsync(labTaskIndicator.LaboratoryTaskId);
        var testRequest = await _testRequestRepository.GetAsync(qualityTask.TestRequestId);

        // Get sample info from sample indicator
        var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labTaskIndicator.TestRequestSampleIndicatorId);
        var sample = await _sampleRepository.GetAsync(sampleIndicator.TestRequestSampleId);

        // Get indicator and method details
        var indicator = await _indicatorRepository.GetAsync(sampleIndicator.IndicatorId);
        TestMethod? method = null;
        if (labTaskIndicator.MethodId.HasValue)
        {
            method = await _testMethodRepository.GetAsync(labTaskIndicator.MethodId.Value);
        }

        // Calculate TimeOfTesting: TestDate - SubmitDate for single indicator
        var timeOfTesting = CalculateTimeOfTestingForSingleIndicator(labTaskIndicator);

        // 3. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var placeholders = new Dictionary<string, string>
        {
            // Common placeholders (BM01, BM03)
            { "{{SampleCode}}", sample.SampleCode ?? "" },
            { "{{CurrentDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{NameOfSample}}", sample.SampleName ?? "" },
            { "{{DateOfReceiving}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", sample.StatusText ?? "" },
            { "{{Quantity}}", sample.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", input.Note ?? "" },

            // Additional placeholders for BM05-QLNN template
            { "{{NumberOfSample}}", sample.SampleCode ?? "" },
            { "{{DescriptionOfSample}}", sample.StatusText ?? "" },
            { "{{TimeToSaveSample}}", "" }, // TODO: Add field to Sample if needed
            { "{{DateOfSending}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{PlaceToSendSample}}", testRequest.CustomerName ?? "" },
            { "{{AttachFile}}", "" }, // TODO: Add attachment info if needed
            { "{{Result}}", ConvertToSuperscriptDisplay(labTaskIndicator.Result) },
            { "{{Conclude}}", "" } // BM05-QLNN - Kết luận (để trống, user tự điền)
        };

        // 4. Build table data - single indicator
        var tableData = new List<ResultFormTableRow>
        {
            new ResultFormTableRow
            {
                No = 1,
                Parameters = indicator.Name ?? "",
                Unit = string.IsNullOrWhiteSpace(labTaskIndicator.Unit) ? "-" : labTaskIndicator.Unit,
                TestMethod = method?.Name ?? "",
                Result = ConvertToSuperscriptDisplay(labTaskIndicator.Result),
                Standards = qualityIndicator.ReferenceLimit ?? "",
                CompareWithStandard = qualityIndicator.ReferenceLimit ?? "",
                Allowlimit = qualityIndicator.ReferenceLimit ?? "", // BM04 - Giới hạn cho phép
                Uncertainty = "",
                Proclaim = ""
            }
        };

        // 5. Generate document using selected template or default BM01
        var templatePath = GetTemplatePath(input.TemplateName, TemplateConstants.TemplateBM01);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", TemplateConstants.TemplateBM01);
        }

        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        var safeSampleCode = sample.SampleCode?.Replace("/", "-") ?? "unknown";
        var fileName = $"{Guid.NewGuid():N}_KQ_DON_{safeSampleCode}.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            sourceStream.CopyTo(destStream);
        }

        ProcessDocument(outputPath, placeholders, tableData);

        // 6. Convert to PDF
        string pdfFileName;
        try
        {
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);
            pdfFileName = Path.GetFileName(pdfPath);

            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary DOCX file: {Path}", outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DOCX to PDF");
            pdfFileName = fileName;
        }

        return new GenerateResultFormOutput
        {
            FileName = pdfFileName,
            SampleCode = sample.SampleCode ?? ""
        };
    }

    /// <summary>
    /// Generate mixed indicator result form (In KQ Hỗn hợp) for Quality Management
    /// Uses BM01 template for multiple indicator results
    /// </summary>
    public async Task<GenerateResultFormOutput> GenerateMixedIndicatorResultFormAsync(GenerateMixedIndicatorInput input)
    {
        if (input.IndicatorIds == null || input.IndicatorIds.Count == 0)
        {
            throw new ValidationException("indicatorIds", "Vui lòng chọn ít nhất một chỉ tiêu");
        }

        // 1. Load quality task and indicators
        var qualityTask = await _qualityTaskRepository.GetAsync(input.TaskId);
        var qualityIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => input.IndicatorIds.Contains(x.Id));

        // Validate all indicators belong to this task and are approved
        foreach (var qi in qualityIndicators)
        {
            if (qi.QualityManagementTaskId != input.TaskId)
            {
                throw new ValidationException("indicator", "Có chỉ tiêu không thuộc công việc này");
            }
            if (qi.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt", "indicator");
            }
        }

        // 2. Get test request
        var testRequest = await _testRequestRepository.GetAsync(qualityTask.TestRequestId);

        // 3. Get all related data
        var labTaskIndicatorIds = qualityIndicators.Select(qi => qi.LaboratoryTaskIndicatorId).ToList();
        var labTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => labTaskIndicatorIds.Contains(x.Id));

        var labTaskIds = labTaskIndicators.Select(lti => lti.LaboratoryTaskId).Distinct().ToList();
        var labTasks = await _laboratoryTaskRepository.GetListAsync(x => labTaskIds.Contains(x.Id));

        var sampleIndicatorIds = labTaskIndicators.Select(lti => lti.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(x => sampleIndicatorIds.Contains(x.Id));

        var sampleIds = sampleIndicators.Select(si => si.TestRequestSampleId).Distinct().ToList();
        var samples = await _sampleRepository.GetListAsync(x => sampleIds.Contains(x.Id));
        var sampleDict = samples.ToDictionary(s => s.Id);
        var firstSample = samples.FirstOrDefault();

        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var indicators = await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id));
        var indicatorDict = indicators.ToDictionary(i => i.Id);

        var methodIds = labTaskIndicators.Where(lti => lti.MethodId.HasValue).Select(lti => lti.MethodId!.Value).Distinct().ToList();
        var methods = methodIds.Any()
            ? await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id))
            : new List<TestMethod>();
        var methodDict = methods.ToDictionary(m => m.Id);

        // Calculate TimeOfTesting: Min(TestDate) - Max(SubmitDate) of all selected indicators
        var timeOfTesting = CalculateTimeOfTesting(labTaskIndicators);

        // 4. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var now = DateTime.Now;
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", firstSample?.SampleCode ?? "" },
            { "{{CurrentDate}}", now.ToString("dd/MM/yyyy") },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{NameOfSample}}", firstSample?.SampleName ?? "" },
            { "{{DateOfReceiving}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", firstSample?.StatusText ?? "" },
            { "{{Quantity}}", firstSample?.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", input.Note ?? "" },
            // BM02 specific placeholders
            { "{{ContractNumber}}", testRequest.ContractNumber ?? "" },
            { "{{Date}}", now.Day.ToString() },
            { "{{Month}}", now.Month.ToString() },
            { "{{Year}}", now.Year.ToString() },
            { "{{CustomerName}}", testRequest.CustomerName ?? "" },
            { "{{FullAddress}}", address }
        };

        // 5. Build table data - multiple indicators
        var tableData = new List<ResultFormTableRow>();
        int rowIndex = 1;

        foreach (var qualityIndicator in qualityIndicators)
        {
            var labTaskIndicator = labTaskIndicators.FirstOrDefault(lti => lti.Id == qualityIndicator.LaboratoryTaskIndicatorId);
            if (labTaskIndicator == null) continue;

            var sampleIndicator = sampleIndicators.FirstOrDefault(si => si.Id == labTaskIndicator.TestRequestSampleIndicatorId);
            if (sampleIndicator == null) continue;

            var indicator = indicatorDict.GetValueOrDefault(sampleIndicator.IndicatorId);
            var sample = sampleDict.GetValueOrDefault(sampleIndicator.TestRequestSampleId);
            TestMethod? method = null;
            if (labTaskIndicator.MethodId.HasValue)
            {
                method = methodDict.GetValueOrDefault(labTaskIndicator.MethodId.Value);
            }

            // Build Result with Note (format: Result + newline + Note)
            // Apply superscript conversion for formulas like 2^2 → 2²
            var resultText = ConvertToSuperscriptDisplay(labTaskIndicator.Result);
            if (!string.IsNullOrWhiteSpace(labTaskIndicator.Note))
            {
                resultText = $"{resultText}\n{labTaskIndicator.Note}";
            }

            tableData.Add(new ResultFormTableRow
            {
                No = rowIndex++,
                NameOfSample = sample?.SampleName ?? "", // BM02 - Tên mẫu
                CodeOfNumber = sample?.SampleCode ?? "", // BM02 - Mã số mẫu
                Parameters = indicator?.Name ?? "",
                Unit = string.IsNullOrWhiteSpace(labTaskIndicator.Unit) ? "-" : labTaskIndicator.Unit,
                TestMethod = method?.Name ?? "",
                Result = resultText,
                Standards = qualityIndicator.ReferenceLimit ?? "",
                CompareWithStandard = qualityIndicator.ReferenceLimit ?? "",
                Allowlimit = qualityIndicator.ReferenceLimit ?? "", // BM04 - Giới hạn cho phép
                Uncertainty = "",
                Proclaim = ""
            });
        }

        // 6. Generate document using fixed BM02 template for mixed indicator result
        // In KQ Hỗn hợp always uses BM02 template (no dropdown selection)
        var templatePath = Path.Combine(_environment.WebRootPath ?? "", "templates", TemplateConstants.TemplateBM02);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", TemplateConstants.TemplateBM02);
        }

        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        var safeSampleCode = (firstSample?.SampleCode ?? "unknown").Replace("/", "-").Replace("\\", "-");
        var fileName = $"{Guid.NewGuid():N}_KQ_HONHOP_{safeSampleCode}.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            sourceStream.CopyTo(destStream);
        }

        ProcessDocument(outputPath, placeholders, tableData);

        // 7. Convert to PDF
        string pdfFileName;
        try
        {
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);
            pdfFileName = Path.GetFileName(pdfPath);

            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary DOCX file: {Path}", outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DOCX to PDF");
            pdfFileName = fileName;
        }

        return new GenerateResultFormOutput
        {
            FileName = pdfFileName,
            SampleCode = firstSample?.SampleCode ?? ""
        };
    }

    /// <summary>
    /// Generate mixed samples result form (In KQ Hỗn hợp cho nhiều mẫu)
    /// Used from TestRequestDetail page - combines indicators from multiple samples into one PDF
    /// </summary>
    public async Task<GenerateResultFormOutput> GenerateMixedSamplesResultFormAsync(GenerateMixedSamplesResultFormInput input)
    {
        if (input.SampleIds == null || input.SampleIds.Count == 0)
        {
            throw new ValidationException("sampleIds", "Vui lòng chọn ít nhất một mẫu");
        }

        // 1. Load and validate data
        var testRequest = await _testRequestRepository.GetAsync(input.TestRequestId);
        var samples = await _sampleRepository.GetListAsync(x => input.SampleIds.Contains(x.Id));

        // Validate all samples belong to this test request
        foreach (var sample in samples)
        {
            if (sample.TestRequestId != input.TestRequestId)
            {
                throw new ValidationException("sample", $"Mẫu {sample.SampleCode} không thuộc phiếu yêu cầu này");
            }
        }

        // Get all sample indicators for selected samples
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            x => input.SampleIds.Contains(x.TestRequestSampleId));

        // Get laboratory tasks for this test request
        var laboratoryTasks = await _laboratoryTaskRepository.GetListAsync(
            x => x.TestRequestId == input.TestRequestId);

        // Get all laboratory task indicators
        var taskIds = laboratoryTasks.Select(t => t.Id).ToList();
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => taskIds.Contains(x.LaboratoryTaskId));

        // Filter task indicators that belong to selected samples' indicators
        var sampleIndicatorIds = sampleIndicators.Select(si => si.Id).ToHashSet();
        var taskIndicators = allTaskIndicators
            .Where(ti => sampleIndicatorIds.Contains(ti.TestRequestSampleIndicatorId))
            .ToList();

        // Find related lab tasks for selected samples
        var relatedTaskIds = taskIndicators.Select(ti => ti.LaboratoryTaskId).Distinct().ToList();
        var relatedTasks = laboratoryTasks.Where(t => relatedTaskIds.Contains(t.Id)).ToList();

        // Validate: All related tasks must have status SentToQuality
        if (!relatedTasks.Any())
        {
            throw new Common.Exceptions.InvalidOperationException("Các mẫu chưa được gán phòng xét nghiệm", "sample");
        }

        var notCompletedTasks = relatedTasks.Where(t => t.Status != LaboratoryTaskStatus.SentToQuality).ToList();
        if (notCompletedTasks.Any())
        {
            throw new Common.Exceptions.InvalidOperationException("Có mẫu chưa xét nghiệm xong", "sample");
        }

        // Get indicator and method details
        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var indicators = await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id));
        var indicatorDict = indicators.ToDictionary(i => i.Id);

        var methodIds = taskIndicators.Where(ti => ti.MethodId.HasValue).Select(ti => ti.MethodId!.Value).Distinct().ToList();
        var methods = methodIds.Any()
            ? await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id))
            : new List<TestMethod>();
        var methodDict = methods.ToDictionary(m => m.Id);

        // Calculate TimeOfTesting: Min(TestDate) - Max(SubmitDate) of all indicators from selected samples
        var timeOfTesting = CalculateTimeOfTesting(taskIndicators);

        // Use first sample for header info (or combine if needed)
        var firstSample = samples.FirstOrDefault();
        var sampleCodes = string.Join(", ", samples.Select(s => s.SampleCode));
        var sampleNames = string.Join(", ", samples.Select(s => s.SampleName).Distinct());

        // 2. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", sampleCodes },
            { "{{CurrentDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{NameOfSample}}", sampleNames },
            { "{{DateOfReceiving}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", firstSample?.StatusText ?? "" },
            { "{{Quantity}}", string.Join(", ", samples.Select(s => s.SampleQuantity).Where(q => !string.IsNullOrEmpty(q))) },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", input.Note ?? "" }
        };

        // 3. Build table data - combine all indicators from all samples
        var tableData = new List<ResultFormTableRow>();
        int rowIndex = 1;

        // Group by sample to show sample info
        var sampleDict = samples.ToDictionary(s => s.Id);
        foreach (var sampleIndicator in sampleIndicators.OrderBy(si => sampleDict.GetValueOrDefault(si.TestRequestSampleId)?.SampleCode))
        {
            var sample = sampleDict.GetValueOrDefault(sampleIndicator.TestRequestSampleId);
            var indicator = indicatorDict.GetValueOrDefault(sampleIndicator.IndicatorId);
            var taskIndicator = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == sampleIndicator.Id);

            // Skip CustomerCancelled indicators (KH hủy)
            if (taskIndicator?.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
            {
                continue;
            }

            TestMethod? method = null;
            if (taskIndicator?.MethodId != null)
            {
                method = methodDict.GetValueOrDefault(taskIndicator.MethodId.Value);
            }

            tableData.Add(new ResultFormTableRow
            {
                No = rowIndex++,
                Parameters = indicator?.Name ?? "",
                Unit = string.IsNullOrWhiteSpace(taskIndicator?.Unit) ? "-" : taskIndicator.Unit,
                TestMethod = method?.Name ?? "",
                Result = ConvertToSuperscriptDisplay(taskIndicator?.Result),
                Standards = "",
                Uncertainty = "",
                Proclaim = ""
            });
        }

        // 4. Generate document
        var templatePath = Path.Combine(_environment.WebRootPath ?? "", "templates", TemplateConstants.TemplateBM01);
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", TemplateConstants.TemplateBM01);
        }

        var tempDir = Path.Combine(_environment.WebRootPath ?? "", "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        var safeSampleCode = (firstSample?.SampleCode ?? "mixed").Replace("/", "-").Replace("\\", "-");
        var fileName = $"{Guid.NewGuid():N}_KQ_HONHOP_{safeSampleCode}.docx";
        var outputPath = Path.Combine(tempDir, fileName);

        using (var sourceStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var destStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            sourceStream.CopyTo(destStream);
        }

        ProcessDocument(outputPath, placeholders, tableData);

        // 5. Convert to PDF
        string pdfFileName;
        try
        {
            var pdfPath = await _pdfConversionService.ConvertDocxToPdfAsync(outputPath);
            pdfFileName = Path.GetFileName(pdfPath);

            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary DOCX file: {Path}", outputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert DOCX to PDF, returning DOCX instead");
            pdfFileName = fileName;
        }

        // 6. Update TestRequest status to PendingApproval after printing
        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await _testRequestRepository.UpdateAsync(testRequest);
        }

        return new GenerateResultFormOutput
        {
            FileName = pdfFileName,
            SampleCode = sampleCodes
        };
    }

    /// <summary>
    /// Get list of available templates for In KQ Đơn (single indicator result)
    /// Only returns specific templates for this purpose
    /// </summary>
    public Task<List<TemplateDto>> GetTemplatesAsync()
    {
        var templatesDir = Path.Combine(_environment.WebRootPath ?? "", "templates");
        var templates = new List<TemplateDto>();

        // Only return templates for In KQ Đơn
        foreach (var templateFileName in TemplateConstants.StandardBasedTemplates)
        {
            var filePath = Path.Combine(templatesDir, templateFileName);
            if (File.Exists(filePath))
            {
                // Remove .template.docx extension to get display name
                var displayName = TemplateConstants.GetDisplayName(templateFileName);

                templates.Add(new TemplateDto
                {
                    Name = displayName,
                    DisplayName = displayName,
                    FileName = templateFileName
                });
            }
        }

        return Task.FromResult(templates.OrderBy(t => t.DisplayName).ToList());
    }

    /// <summary>
    /// Get template path by name, falls back to default template if not found
    /// </summary>
    private string GetTemplatePath(string? templateName, string defaultTemplate)
    {
        var templatesDir = Path.Combine(_environment.WebRootPath ?? "", "templates");

        if (!string.IsNullOrWhiteSpace(templateName))
        {
            var customTemplatePath = Path.Combine(templatesDir, TemplateConstants.GetTemplateFileName(templateName));
            if (File.Exists(customTemplatePath))
            {
                return customTemplatePath;
            }
            _logger.LogWarning("Template not found: {TemplateName}, using default: {DefaultTemplate}", templateName, defaultTemplate);
        }

        return Path.Combine(templatesDir, defaultTemplate);
    }

    #region Superscript Helpers

    /// <summary>
    /// Unicode Superscript character mapping
    /// </summary>
    private static readonly Dictionary<char, char> SuperscriptMap = new()
    {
        {'0', '⁰'}, {'1', '¹'}, {'2', '²'}, {'3', '³'}, {'4', '⁴'},
        {'5', '⁵'}, {'6', '⁶'}, {'7', '⁷'}, {'8', '⁸'}, {'9', '⁹'},
        {'-', '⁻'}, {'+', '⁺'}, {'.', '·'}
    };

    /// <summary>
    /// Convert caret notation to Unicode superscript for display in reports
    /// Examples:
    /// - 2^2 → 2²
    /// - 10^-3 → 10⁻³
    /// - 1.5x10^-6 → 1.5×10⁻⁶
    /// - 2X10^2 → 2×10²
    /// Already-unicode superscript text is passed through unchanged
    /// </summary>
    private static string ConvertToSuperscriptDisplay(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? "";

        var result = text;

        // 1. Convert 'x' or 'X' between numbers to '×' (multiplication sign)
        // Pattern: number followed by x/X followed by number
        result = Regex.Replace(result, @"(\d)\s*[xX]\s*(\d)", "$1×$2");

        // 2. Convert caret notation to superscript
        // Pattern: number^exponent (e.g., 2^2, 10^-3, 5.5^2)
        var caretPattern = @"(\d+(?:\.\d+)?)\^(-?\d+(?:\.\d+)?)";
        result = Regex.Replace(result, caretPattern, match =>
        {
            var baseNum = match.Groups[1].Value;
            var exponent = match.Groups[2].Value;
            var superscriptExponent = new string(exponent.Select(c =>
                SuperscriptMap.GetValueOrDefault(c, c)).ToArray());
            return baseNum + superscriptExponent;
        });

        return result;
    }

    #endregion
}
