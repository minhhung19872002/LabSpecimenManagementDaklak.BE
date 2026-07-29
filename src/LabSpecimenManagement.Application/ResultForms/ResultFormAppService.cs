using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
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
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IRepository<QualityManagementTask, Guid> _qualityTaskRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _qualityIndicatorRepository;
    private readonly IRepository<SampleIndicatorNote, Guid> _sampleIndicatorNoteRepository;
    private readonly IRepository<MasterData.Note, Guid> _noteRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationCreator _notificationCreator;
    private readonly ICurrentUser _currentUser;
    private readonly IPdfConversionService _pdfConversionService;
    private readonly ILogger<ResultFormAppService> _logger;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;

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
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<QualityManagementTask, Guid> qualityTaskRepository,
        IRepository<QualityManagementIndicator, Guid> qualityIndicatorRepository,
        IRepository<SampleIndicatorNote, Guid> sampleIndicatorNoteRepository,
        IRepository<MasterData.Note, Guid> noteRepository,
        IWebHostEnvironment environment,
        INotificationCreator notificationCreator,
        ICurrentUser currentUser,
        IPdfConversionService pdfConversionService,
        ILogger<ResultFormAppService> logger,
        IIdentityUserRepository userRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _qualityTaskRepository = qualityTaskRepository;
        _qualityIndicatorRepository = qualityIndicatorRepository;
        _sampleIndicatorNoteRepository = sampleIndicatorNoteRepository;
        _noteRepository = noteRepository;
        _environment = environment;
        _notificationCreator = notificationCreator;
        _currentUser = currentUser;
        _pdfConversionService = pdfConversionService;
        _logger = logger;
        _userRepository = userRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
    }

    /// <summary>
    /// Lookup ngày in đầu tiên từ QualityManagementIndicator cho toàn bộ test request.
    /// Trả về PrintedDate sớm nhất nếu có, null nếu chưa từng in.
    /// </summary>
    private async Task<DateTime?> GetFirstPrintDateForTestRequestAsync(Guid testRequestId)
    {
        var qualityTasks = await _qualityTaskRepository.GetListAsync(
            x => x.TestRequestId == testRequestId);
        var qualityTaskIds = qualityTasks.Select(t => t.Id).ToList();

        if (!qualityTaskIds.Any()) return null;

        var qualityIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => qualityTaskIds.Contains(x.QualityManagementTaskId) && x.PrintedDate != null);

        return qualityIndicators
            .Select(qi => qi.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault();
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

        // Load TestMethodIndicator for per-indicator formatted unit
        var methodIndicators = methodIds.Any() && indicatorIds.Any()
            ? await _testMethodIndicatorRepository.GetListAsync(x =>
                methodIds.Contains(x.TestMethodId) && indicatorIds.Contains(x.IndicatorId))
            : new List<TestMethodIndicator>();

        // Calculate TimeOfTesting: Min(TestDate) - Max(SubmitDate) of selected sample's indicators only
        // - Ngày chỉ tiêu đầu tiên thử nghiệm: Ngày KTV check-in chỉ tiêu đầu tiên của mẫu được chọn
        // - Ngày gửi kết quả cuối cùng: Ngày gửi kết quả (SubmitDate) muộn nhất của mẫu được chọn
        var timeOfTesting = CalculateTimeOfTesting(taskIndicators);

        // 2. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var printDate = input.PrintDate
            ?? await GetFirstPrintDateForTestRequestAsync(input.TestRequestId)
            ?? DateTime.Now;
        var printDateText = printDate.ToString("dd/MM/yyyy");
        var contractDateText = testRequest.ContractDate?.ToString("dd/MM/yyyy") ?? "";
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", sample.SampleCode ?? "" },
            { "{{CurrentDate}}", printDateText },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },  // Typo in template
            { "{{Address}}", address },  // Correct spelling
            { "{{SamplingAddress}}", sample.SamplingAddress ?? "" },
            { "{{NameOfSample}}", sample.SampleName ?? "" },
            { "{{DateOfReceiving}}", contractDateText },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", sample.StatusText ?? "" },
            { "{{Quantity}}", sample.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            // BM05-QLNN specific placeholders
            { "{{NumberOfSample}}", sample.SampleCode ?? "" },
            { "{{SampleQuantity}}", sample.SampleQuantity ?? "" },
            { "{{DescriptionOfSample}}", sample.StatusText ?? "" },
            { "{{TimeToSaveSample}}", "" },
            // BA: DateOfSending = ngày lấy mẫu, dùng cùng giá trị với DateOfReceiving (ContractDate)
            { "{{DateOfSending}}", contractDateText },
            { "{{PlaceToSendSample}}", testRequest.CustomerName ?? "" },
            { "{{AttachFile}}", "" },
            { "{{Conclude}}", "" }
        };

        // Add Officer In Charge placeholder (QLKT who approved the indicators)
        // Only shows name if exactly 1 approver, otherwise empty
        var officerInChargeName = await GetOfficerInChargeNameAsync(taskIndicators);
        placeholders.Add("{{OfficerInCharge}}", officerInChargeName);

        // 3. Build table data - Handle parent-child indicator grouping
        var tableData = await BuildTableDataWithParentChildGroupingAsync(
            sampleIndicators,
            indicators,
            indicatorDict,
            taskIndicators,
            methodDict,
            methodIndicators);

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
        if (!string.IsNullOrWhiteSpace(testRequest.Ward)) parts.Add(LowercaseAdministrativePrefix(testRequest.Ward, "Phường", "Xã"));
        if (!string.IsNullOrWhiteSpace(testRequest.District)) parts.Add(LowercaseAdministrativePrefix(testRequest.District, "Quận"));
        if (!string.IsNullOrWhiteSpace(testRequest.Province)) parts.Add(LowercaseAdministrativePrefix(testRequest.Province, "Tỉnh", "Thành phố"));
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Get Vice President full name by user ID
    /// Returns empty string if not provided or user not found
    /// </summary>
    private async Task<string> GetVicePresidentFullNameAsync(Guid? vicePresidentId)
    {
        if (!vicePresidentId.HasValue)
        {
            return "";
        }

        var user = await _userRepository.FindAsync(vicePresidentId.Value);
        if (user == null)
        {
            return "";
        }

        var fullName = $"{user.Surname} {user.Name}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName : fullName;
    }

    /// <summary>
    /// Get Officer In Charge name from LaboratoryTaskIndicators
    /// Logic:
    /// - Get all unique ApprovedById from selected indicators
    /// - If exactly 1 approver: return full name (Surname + Name)
    /// - If 0 or multiple approvers: return empty string
    /// </summary>
    private async Task<string> GetOfficerInChargeNameAsync(IEnumerable<LaboratoryTaskIndicator> taskIndicators)
    {
        if (taskIndicators == null || !taskIndicators.Any())
        {
            return "";
        }

        // Get TechnicalManagerId from LaboratoryTask (QLKT của phòng thử nghiệm)
        var labTaskIds = taskIndicators
            .Select(i => i.LaboratoryTaskId)
            .Distinct()
            .ToList();

        var labTasks = await _laboratoryTaskRepository.GetListAsync(lt => labTaskIds.Contains(lt.Id));
        var managerIds = labTasks
            .Where(lt => lt.TechnicalManagerId.HasValue)
            .Select(lt => lt.TechnicalManagerId!.Value)
            .Distinct()
            .ToList();

        if (!managerIds.Any())
        {
            return "";
        }

        var managers = (await _userRepository.GetListAsync())
            .Where(u => managerIds.Contains(u.Id))
            .ToList();
        
        var managerNames = managers.Select(m =>
        {
            var fullName = $"{m.Surname} {m.Name}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? m.UserName : fullName;
        });

        return string.Join(", ", managerNames);
    }

    /// <summary>
    /// Build table data with parent-child indicator grouping
    /// Parent indicators are displayed with their children grouped below:
    /// - Parameters: "Parent Name\n- Child1 Name\n- Child2 Name"
    /// - Result: "- Result1\n- Result2"
    /// </summary>
    /// <param name="skipCustomerCancelled">If true, skip indicators with CustomerCancelled status</param>
    /// <param name="useHyphenForEmptyUnit">If true, use "-" for empty unit (BM01 style)</param>
    private async Task<List<ResultFormTableRow>> BuildTableDataWithParentChildGroupingAsync(
        List<TestRequestSampleIndicator> sampleIndicators,
        List<Indicator> indicators,
        Dictionary<Guid, Indicator> indicatorDict,
        List<LaboratoryTaskIndicator> taskIndicators,
        Dictionary<Guid, TestMethod> methodDict,
        List<TestMethodIndicator> methodIndicators,
        bool skipCustomerCancelled = false,
        bool useHyphenForEmptyUnit = false)
    {
        var tableData = new List<ResultFormTableRow>();
        int rowIndex = 1;

        var sampleIndicatorToIndicatorId = sampleIndicators.ToDictionary(si => si.Id, si => si.IndicatorId);

        string GetUnit(LaboratoryTaskIndicator? ti)
        {
            var unit = ti?.Unit;

            if (!string.IsNullOrWhiteSpace(unit) && HtmlToOpenXmlConverter.ContainsHtmlTags(unit))
            {
                return unit;
            }

            if (ti?.MethodId != null && sampleIndicatorToIndicatorId.TryGetValue(ti.TestRequestSampleIndicatorId, out var indicatorId))
            {
                var tmi = methodIndicators.FirstOrDefault(x =>
                    x.TestMethodId == ti.MethodId && x.IndicatorId == indicatorId);
                if (!string.IsNullOrWhiteSpace(tmi?.FormattedUnit))
                {
                    return tmi.FormattedUnit;
                }
            }

            return useHyphenForEmptyUnit
                ? (string.IsNullOrWhiteSpace(unit) ? "-" : unit!)
                : (unit ?? "");
        }

        // For subcontract flow, ưu tiên tên phương pháp phụ nhập tại popup.
        (string Name, string? FormattedName) GetDisplayMethod(LaboratoryTaskIndicator? ti, TestMethod? method)
        {
            if (!string.IsNullOrWhiteSpace(ti?.SubContractMethodName))
            {
                return (ti.SubContractMethodName!, null);
            }

            return (method?.Name ?? string.Empty, method?.FormattedName);
        }

        // Get parent indicator IDs that are referenced but might not be in our list
        var parentIdsToFetch = indicators
            .Where(i => i.ParentIndicatorId.HasValue && !indicatorDict.ContainsKey(i.ParentIndicatorId.Value))
            .Select(i => i.ParentIndicatorId!.Value)
            .Distinct()
            .ToList();

        // Fetch parent indicators if needed
        if (parentIdsToFetch.Any())
        {
            var parentIndicators = await _indicatorRepository.GetListAsync(x => parentIdsToFetch.Contains(x.Id));
            foreach (var parent in parentIndicators)
            {
                indicatorDict[parent.Id] = parent;
            }
        }

        // Filter out CustomerCancelled indicators if needed
        var filteredSampleIndicators = sampleIndicators;
        if (skipCustomerCancelled)
        {
            var cancelledSampleIndicatorIds = taskIndicators
                .Where(ti => ti.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
                .Select(ti => ti.TestRequestSampleIndicatorId)
                .ToHashSet();
            filteredSampleIndicators = sampleIndicators
                .Where(si => !cancelledSampleIndicatorIds.Contains(si.Id))
                .ToList();
        }

        // Separate indicators into: standalone (no parent), children (has parent)
        var childIndicators = filteredSampleIndicators
            .Where(si => indicatorDict.TryGetValue(si.IndicatorId, out var ind) && ind.ParentIndicatorId.HasValue)
            .ToList();
        var standaloneIndicators = filteredSampleIndicators
            .Where(si => indicatorDict.TryGetValue(si.IndicatorId, out var ind) && !ind.ParentIndicatorId.HasValue)
            .ToList();

        // Group children by parent
        var childrenByParent = childIndicators
            .GroupBy(si => indicatorDict[si.IndicatorId].ParentIndicatorId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Process standalone indicators (that are parents with children in this sample, or truly standalone)
        var processedParentIds = new HashSet<Guid>();

        foreach (var sampleIndicator in standaloneIndicators.OrderBy(si => indicatorDict[si.IndicatorId].SortOrder))
        {
            var indicator = indicatorDict[sampleIndicator.IndicatorId];
            var taskIndicator = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == sampleIndicator.Id);

            // Check if this standalone indicator has children in this sample
            if (childrenByParent.TryGetValue(indicator.Id, out var children) && children.Any())
            {
                // This is a parent with children - combine them
                processedParentIds.Add(indicator.Id);

                var parametersPlainBuilder = new StringBuilder();  // Plain text (Name)
                var parametersFormattedBuilder = new StringBuilder();  // HTML formatted (FormattedName)
                var resultBuilder = new StringBuilder();
                var hasAnyFormattedName = !string.IsNullOrEmpty(indicator.FormattedName);

                // Add parent name first
                parametersPlainBuilder.Append(indicator.Name);
                parametersFormattedBuilder.Append(indicator.FormattedName ?? indicator.Name);

                // Add children
                foreach (var childSi in children.OrderBy(c => indicatorDict[c.IndicatorId].SortOrder))
                {
                    var childInd = indicatorDict[childSi.IndicatorId];
                    var childTi = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == childSi.Id);

                    if (!string.IsNullOrEmpty(childInd.FormattedName))
                        hasAnyFormattedName = true;

                    parametersPlainBuilder.Append("\n- ");
                    parametersPlainBuilder.Append(childInd.Name);

                    parametersFormattedBuilder.Append("\n- ");
                    parametersFormattedBuilder.Append(childInd.FormattedName ?? childInd.Name);

                    if (resultBuilder.Length > 0) resultBuilder.Append('\n');
                    resultBuilder.Append(ConvertToSuperscriptDisplay(childTi?.Result));
                }

                // Get first child's method and unit
                var firstChildSi = children.First();
                var firstChildTi = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == firstChildSi.Id);
                TestMethod? method = null;
                if (firstChildTi?.MethodId != null)
                {
                    method = methodDict.GetValueOrDefault(firstChildTi.MethodId.Value);
                }
                var displayMethod = GetDisplayMethod(firstChildTi, method);

                tableData.Add(new ResultFormTableRow
                {
                    No = rowIndex++,
                    Parameters = parametersPlainBuilder.ToString(),
                    ParametersFormattedName = hasAnyFormattedName ? parametersFormattedBuilder.ToString() : null,
                    Unit = GetUnit(firstChildTi),
                    TestMethod = displayMethod.Name,
                    TestMethodFormattedName = displayMethod.FormattedName,
                    Result = resultBuilder.ToString(),
                    Standards = "",
                    Uncertainty = "",
                    Proclaim = ""
                });
            }
            else
            {
                // Truly standalone indicator (no children)
                TestMethod? method = null;
                if (taskIndicator?.MethodId != null)
                {
                    method = methodDict.GetValueOrDefault(taskIndicator.MethodId.Value);
                }
                var displayMethod = GetDisplayMethod(taskIndicator, method);

                tableData.Add(new ResultFormTableRow
                {
                    No = rowIndex++,
                    Parameters = indicator.Name ?? "",
                    ParametersFormattedName = indicator.FormattedName,
                    Unit = GetUnit(taskIndicator),
                    TestMethod = displayMethod.Name,
                    TestMethodFormattedName = displayMethod.FormattedName,
                    Result = ConvertToSuperscriptDisplay(taskIndicator?.Result),
                    Standards = "",
                    Uncertainty = "",
                    Proclaim = ""
                });
            }
        }

        // Process orphan children (parent not in this sample)
        var orphanParentIds = childrenByParent.Keys.Except(processedParentIds)
            .OrderBy(pid => indicatorDict.GetValueOrDefault(pid)?.SortOrder ?? 0)
            .ToList();
        foreach (var parentId in orphanParentIds)
        {
            var children = childrenByParent[parentId];
            var parentIndicator = indicatorDict.GetValueOrDefault(parentId);

            var parametersPlainBuilder = new StringBuilder();
            var parametersFormattedBuilder = new StringBuilder();
            var resultBuilder = new StringBuilder();
            var hasAnyFormattedName = parentIndicator != null && !string.IsNullOrEmpty(parentIndicator.FormattedName);

            // Add parent name first (even if parent is not in sample)
            if (parentIndicator != null)
            {
                parametersPlainBuilder.Append(parentIndicator.Name);
                parametersFormattedBuilder.Append(parentIndicator.FormattedName ?? parentIndicator.Name);
            }

            // Add children
            foreach (var childSi in children.OrderBy(c => indicatorDict[c.IndicatorId].SortOrder))
            {
                var childInd = indicatorDict[childSi.IndicatorId];
                var childTi = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == childSi.Id);

                if (!string.IsNullOrEmpty(childInd.FormattedName))
                    hasAnyFormattedName = true;

                if (parametersPlainBuilder.Length > 0) parametersPlainBuilder.Append('\n');
                parametersPlainBuilder.Append("- ");
                parametersPlainBuilder.Append(childInd.Name);

                if (parametersFormattedBuilder.Length > 0) parametersFormattedBuilder.Append('\n');
                parametersFormattedBuilder.Append("- ");
                parametersFormattedBuilder.Append(childInd.FormattedName ?? childInd.Name);

                if (resultBuilder.Length > 0) resultBuilder.Append('\n');
                resultBuilder.Append(ConvertToSuperscriptDisplay(childTi?.Result));
            }

            // Get first child's method and unit
            var firstChildSi = children.First();
            var firstChildTi = taskIndicators.FirstOrDefault(ti => ti.TestRequestSampleIndicatorId == firstChildSi.Id);
            TestMethod? method = null;
            if (firstChildTi?.MethodId != null)
            {
                method = methodDict.GetValueOrDefault(firstChildTi.MethodId.Value);
            }
            var displayMethod = GetDisplayMethod(firstChildTi, method);

            tableData.Add(new ResultFormTableRow
            {
                No = rowIndex++,
                Parameters = parametersPlainBuilder.ToString(),
                ParametersFormattedName = hasAnyFormattedName ? parametersFormattedBuilder.ToString() : null,
                Unit = GetUnit(firstChildTi),
                TestMethod = displayMethod.Name,
                TestMethodFormattedName = displayMethod.FormattedName,
                Result = resultBuilder.ToString(),
                Standards = "",
                Uncertainty = "",
                Proclaim = ""
            });
        }

        // Sort by TestMethod name so indicators with the same method are grouped together
        tableData = tableData
            .OrderBy(r => r.TestMethod, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Re-number after sorting
        for (int i = 0; i < tableData.Count; i++)
        {
            tableData[i].No = i + 1;
        }

        return tableData;
    }

    /// <summary>
    /// Calculate TimeOfTesting from a list of LaboratoryTaskIndicators
    /// Returns: "dd/MM/yyyy - dd/MM/yyyy" where:
    /// - Start date = Min(TestDate) of all indicators (ngày thử nghiệm sớm nhất)
    /// - End date = Max(SubmitDate) of all indicators (ngày gửi kết quả muộn nhất)
    /// Always displays both dates even if they are the same (e.g., "07/01/2025 - 07/01/2025")
    /// </summary>
    private string CalculateTimeOfTesting(List<LaboratoryTaskIndicator> taskIndicators)
    {
        if (taskIndicators == null || !taskIndicators.Any())
        {
            return "";
        }

        // Get Min TestDate (ngày thử nghiệm sớm nhất - chỉ tiêu gần nhất)
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

        // Format result - always show both dates
        if (minTestDate == default && maxSubmitDate == default)
        {
            return "";
        }
        else if (minTestDate == default)
        {
            // Only have submit date, show it twice
            return $"{maxSubmitDate:dd/MM/yyyy} - {maxSubmitDate:dd/MM/yyyy}";
        }
        else if (maxSubmitDate == default)
        {
            // Only have test date, show it twice
            return $"{minTestDate:dd/MM/yyyy} - {minTestDate:dd/MM/yyyy}";
        }
        else
        {
            var startDate = minTestDate.Date;
            var endDate = maxSubmitDate.Date;

            return $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
        }
    }

    /// <summary>
    /// Calculate TimeOfTesting for a single LaboratoryTaskIndicator
    /// Returns: "dd/MM/yyyy - dd/MM/yyyy" where:
    /// - Start date = TestDate
    /// - End date = SubmitDate
    /// If one side is missing, repeats the available date
    /// </summary>
    private string CalculateTimeOfTestingForSingleIndicator(LaboratoryTaskIndicator taskIndicator)
    {
        if (taskIndicator == null)
        {
            return "";
        }

        var testDate = taskIndicator.TestDate;
        var submitDate = taskIndicator.SubmitDate;

        if (!testDate.HasValue && !submitDate.HasValue)
        {
            return "";
        }
        else if (!testDate.HasValue)
        {
            return $"{submitDate!.Value:dd/MM/yyyy} - {submitDate.Value:dd/MM/yyyy}";
        }
        else if (!submitDate.HasValue)
        {
            return $"{testDate.Value:dd/MM/yyyy} - {testDate.Value:dd/MM/yyyy}";
        }
        else
        {
            var startDate = testDate.Value.Date;
            var endDate = submitDate.Value.Date;

            return $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
        }
    }

    private void ProcessDocument(string filePath, Dictionary<string, string> placeholders, List<ResultFormTableRow> tableData)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var mainPart = document.MainDocumentPart;
        var body = mainPart?.Document?.Body;
        if (body == null || mainPart == null) return;

        // Convert floating tables to inline so they flow with content
        // This fixes the issue where "Ghi chú" and footnotes overlay the data table
        ConvertFloatingTablesToInline(document);

        // IMPORTANT: Process table FIRST before replacing other placeholders
        // This ensures the table structure is intact when we find and clone template rows
        ProcessTable(body, tableData);

        // Then replace placeholders in entire document (including headers, footers, textboxes)
        ReplacePlaceholdersInDocument(mainPart, placeholders);

        // Fix: Ensure all table cells have explicit vertical alignment
        // This fixes the issue where watermark in document header causes LibreOffice/Collabora
        // to incorrectly render vertical alignment (text pushed to top instead of center)
        EnsureTableCellVerticalAlignment(body);

        mainPart.Document?.Save();
    }

    private void ReplacePlaceholdersInBody(Body body, Dictionary<string, string> placeholders)
    {
        placeholders = TrimPlaceholderValues(placeholders);

        // First, merge split placeholders in all paragraphs
        foreach (var para in body.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Merge dots that follow placeholders into the same text element
        foreach (var para in body.Descendants<Paragraph>())
        {
            MergeDotsWithPlaceholders(para);
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
                        // For Note placeholder, remove height constraint on containing cell/row and preserve font size from template
                        if (placeholder.Key == "{{Note}}" && !string.IsNullOrEmpty(placeholder.Value))
                        {
                            RemoveContainingCellHeightConstraint(text);
                            // Get font size from placeholder in template (default 11pt if not set)
                            var fontSize = GetFontSizeFromText(text);
                            SetNoteFontSize(text, fontSize);
                            NormalizeParagraphIndentation(text); // Convert first line indent to left indent for all lines

                            // Handle HTML content in Note (from rich text editor)
                            if (HtmlToOpenXmlConverter.ContainsHtmlTags(placeholder.Value))
                            {
                                ReplaceNoteWithHtmlRuns(text, placeholder.Key, placeholder.Value);
                                continue;
                            }

                            // Handle newlines in Note placeholder (plain text fallback)
                            if (placeholder.Value.Contains("\n"))
                            {
                                ReplaceTextWithLineBreaks(text, placeholder.Key, placeholder.Value);
                                continue;
                            }
                        }
                        // For Address placeholder, remove height constraint to allow long addresses
                        if ((placeholder.Key == "{{Address}}" || placeholder.Key == "{{Adrress}}") && !string.IsNullOrEmpty(placeholder.Value))
                        {
                            RemoveContainingCellHeightConstraint(text);
                        }
                        // For Customer placeholder, remove height constraint to allow long customer names
                        if (placeholder.Key == "{{Customer}}" && !string.IsNullOrEmpty(placeholder.Value))
                        {
                            RemoveContainingCellHeightConstraint(text);
                        }

                        // If placeholder has value, replace placeholder + surrounding dots with value + padding spaces
                        // Keep total length same as original (placeholder + dots) to maintain layout without line break
                        // Template: "{{CustomerName}}......" (22 chars) -> "Công ty ABC           " (22 chars with spaces)
                        // But if value is longer than original, just use value without extra spaces (no truncation)
                        if (!string.IsNullOrEmpty(placeholder.Value))
                        {
                            // Extended dot/leader pattern - includes all possible dot-like characters
                            var dotPattern = @"[\.…·•․‥．⋯᠁︙\u2024\u2025\u22EF\u22EE_\-─━]+";

                            // Pattern: placeholder followed by dots (most common case)
                            var patternTrailingDots = System.Text.RegularExpressions.Regex.Escape(placeholder.Key) + dotPattern;
                            text.Text = System.Text.RegularExpressions.Regex.Replace(
                                text.Text,
                                patternTrailingDots,
                                match =>
                                {
                                    var originalLength = match.Value.Length;
                                    var paddingNeeded = Math.Max(0, originalLength - placeholder.Value.Length);
                                    return placeholder.Value + new string(' ', paddingNeeded);
                                });

                            // Pattern: dots before placeholder + placeholder + optional dots after
                            var patternLeadingDots = dotPattern + @"\s*" + System.Text.RegularExpressions.Regex.Escape(placeholder.Key) + @"(?:" + dotPattern + @")?";
                            text.Text = System.Text.RegularExpressions.Regex.Replace(
                                text.Text,
                                patternLeadingDots,
                                match =>
                                {
                                    var originalLength = match.Value.Length;
                                    var paddingNeeded = Math.Max(0, originalLength - placeholder.Value.Length);
                                    return placeholder.Value + new string(' ', paddingNeeded);
                                });
                        }

                        // Standard replacement (for cases without dots or when value is empty)
                        text.Text = text.Text.Replace(placeholder.Key, placeholder.Value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Remove fixed height constraint from the table cell and row containing the text element
    /// This allows the cell to auto-expand when content is long
    /// Also handles nested tables by removing height constraints from all parent rows
    /// </summary>
    private void RemoveContainingCellHeightConstraint(Text textElement)
    {
        // Find parent TableCell
        var cell = textElement.Ancestors<TableCell>().FirstOrDefault();
        if (cell == null) return;

        // Enable text wrapping in the cell (remove NoWrap if exists)
        var cellProps = cell.GetFirstChild<TableCellProperties>();
        if (cellProps != null)
        {
            var noWrap = cellProps.GetFirstChild<NoWrap>();
            if (noWrap != null)
            {
                noWrap.Remove();
            }
        }

        // Remove height constraints from ALL parent rows (including nested table parents)
        // This is critical for cells inside nested tables where parent rows also have fixed height
        foreach (var row in textElement.Ancestors<TableRow>())
        {
            RemoveRowHeightConstraint(row);
            AllowPageBreakInRow(row);
        }

        // Also remove height constraints from all parent cells (for deeply nested structures)
        foreach (var parentCell in textElement.Ancestors<TableCell>())
        {
            var parentCellProps = parentCell.GetFirstChild<TableCellProperties>();
            if (parentCellProps != null)
            {
                // Remove NoWrap from parent cells too
                var parentNoWrap = parentCellProps.GetFirstChild<NoWrap>();
                if (parentNoWrap != null)
                {
                    parentNoWrap.Remove();
                }
            }
        }
    }

    /// <summary>
    /// Set font size for the Note text element to ensure consistent formatting
    /// Font size in OpenXML is in half-points (e.g., 11pt = 22 half-points)
    /// </summary>
    /// <summary>
    /// Get font size from text element in template (in points)
    /// </summary>
    /// <param name="textElement">Text element containing placeholder</param>
    /// <returns>Font size in points (default 11 if not found)</returns>
    private int GetFontSizeFromText(Text textElement)
    {
        var parentRun = textElement.Parent as Run;
        if (parentRun == null) return 11;

        var runProps = parentRun.GetFirstChild<RunProperties>();
        if (runProps == null) return 11;

        // Try to get FontSize first
        var fontSize = runProps.GetFirstChild<FontSize>();
        if (fontSize?.Val?.Value != null)
        {
            // Font size in OpenXML is in half-points, convert to points
            if (int.TryParse(fontSize.Val.Value, out var halfPoints))
            {
                return halfPoints / 2;
            }
        }

        // Try FontSizeComplexScript as fallback
        var fontSizeCs = runProps.GetFirstChild<FontSizeComplexScript>();
        if (fontSizeCs?.Val?.Value != null)
        {
            if (int.TryParse(fontSizeCs.Val.Value, out var halfPoints))
            {
                return halfPoints / 2;
            }
        }

        // Default to 11pt if not found
        return 11;
    }

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

        // Set new font size
        runProps.Append(new FontSize { Val = halfPoints });
        runProps.Append(new FontSizeComplexScript { Val = halfPoints });
    }

    /// <summary>
    /// Normalize paragraph indentation - convert first line indent to left indent
    /// This ensures all lines in Note are indented uniformly (not just the first line)
    /// </summary>
    private void NormalizeParagraphIndentation(Text textElement)
    {
        var parentRun = textElement.Parent as Run;
        if (parentRun == null) return;

        var paragraph = parentRun.Ancestors<Paragraph>().FirstOrDefault();
        if (paragraph == null) return;

        var pPr = paragraph.GetFirstChild<ParagraphProperties>();
        if (pPr == null)
        {
            pPr = new ParagraphProperties();
            paragraph.InsertAt(pPr, 0);
        }

        var indentation = pPr.GetFirstChild<Indentation>();
        if (indentation != null)
        {
            // Get first line indent value and convert to left indent for all lines
            var firstLineValue = indentation.FirstLine?.Value;
            if (!string.IsNullOrEmpty(firstLineValue))
            {
                // Remove first line indent and set left indent instead
                indentation.FirstLine = null;
                indentation.Left = new StringValue(firstLineValue);
            }
        }
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

    /// <summary>
    /// Merge dots that follow placeholders (in separate runs) into the placeholder's text element
    /// This ensures "{{CustomerName}}" + "..." becomes "{{CustomerName}}..." in one text element
    /// </summary>
    private void MergeDotsWithPlaceholders(Paragraph para)
    {
        var runs = para.Elements<Run>().ToList();
        if (runs.Count < 2) return;

        for (int i = 0; i < runs.Count - 1; i++)
        {
            var currentRun = runs[i];
            var currentText = currentRun.GetFirstChild<Text>();
            if (currentText == null || string.IsNullOrEmpty(currentText.Text)) continue;

            // Check if current run ends with a placeholder pattern }}
            if (!currentText.Text.EndsWith("}}")) continue;

            // Look at next run(s) for dots
            var dotsToMerge = new System.Text.StringBuilder();
            var runsToRemove = new List<Run>();

            for (int j = i + 1; j < runs.Count; j++)
            {
                var nextRun = runs[j];
                var nextText = nextRun.GetFirstChild<Text>();
                if (nextText == null || string.IsNullOrEmpty(nextText.Text)) continue;

                var text = nextText.Text;
                // Check if this run is only dots
                var dotsOnly = true;
                var dotsCount = 0;
                foreach (var c in text)
                {
                    if (DotCharacters.Contains(c))
                    {
                        dotsCount++;
                    }
                    else
                    {
                        dotsOnly = false;
                        break;
                    }
                }

                if (dotsOnly && dotsCount > 0)
                {
                    // This run is all dots, merge it
                    dotsToMerge.Append(text);
                    runsToRemove.Add(nextRun);
                }
                else if (dotsCount > 0)
                {
                    // Run starts with dots but has other content
                    // Take just the leading dots
                    var leadingDots = "";
                    foreach (var c in text)
                    {
                        if (DotCharacters.Contains(c))
                            leadingDots += c;
                        else
                            break;
                    }
                    if (leadingDots.Length > 0)
                    {
                        dotsToMerge.Append(leadingDots);
                        nextText.Text = text.Substring(leadingDots.Length);
                    }
                    break; // Stop looking for more dots
                }
                else
                {
                    // No dots found, stop
                    break;
                }
            }

            // Merge dots into current run
            if (dotsToMerge.Length > 0)
            {
                currentText.Text += dotsToMerge.ToString();
                foreach (var runToRemove in runsToRemove)
                {
                    runToRemove.Remove();
                }
                // Update runs list after removal
                runs = para.Elements<Run>().ToList();
            }
        }
    }

    private void MergeSplitPlaceholders(Paragraph para)
    {
        var runs = para.Elements<Run>().ToList();
        if (runs.Count <= 1) return;

        // Check if paragraph contains Field elements (PAGE, NUMPAGES, etc.)
        var hasFieldElements = para.Descendants<FieldChar>().Any() ||
                               para.Descendants<FieldCode>().Any() ||
                               para.Descendants<SimpleField>().Any();

        if (hasFieldElements)
        {
            // For paragraphs with fields, merge only NON-field runs that are consecutive
            // This preserves field codes while still merging split placeholders
            MergeSplitPlaceholdersWithFields(para, runs);
            return;
        }

        // Get full text from all runs
        var fullText = string.Concat(runs.SelectMany(r => r.Elements<Text>()).Select(t => t.Text ?? ""));

        // If no placeholder pattern, skip
        if (!fullText.Contains("{{")) return;

        // Check if runs have different formatting (Bold, Italic, etc.)
        // If so, use smart merge that preserves formatting
        if (HasDifferentFormatting(runs))
        {
            MergeSplitPlaceholdersPreserveFormatting(para, runs);
            return;
        }

        // All runs have same formatting - safe to merge all text into first run
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

    /// <summary>
    /// Check if runs have different formatting (Bold, Italic, Underline, etc.)
    /// </summary>
    private bool HasDifferentFormatting(List<Run> runs)
    {
        if (runs.Count <= 1) return false;

        bool? firstBold = null;
        bool? firstItalic = null;

        foreach (var run in runs)
        {
            var props = run.RunProperties;
            var isBold = props?.Bold != null && (props.Bold.Val == null || props.Bold.Val.Value);
            var isItalic = props?.Italic != null && (props.Italic.Val == null || props.Italic.Val.Value);

            if (firstBold == null)
            {
                firstBold = isBold;
                firstItalic = isItalic;
            }
            else if (isBold != firstBold || isItalic != firstItalic)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Merge split placeholders while preserving formatting of each run.
    /// Only merges consecutive runs with the same formatting.
    /// </summary>
    private void MergeSplitPlaceholdersPreserveFormatting(Paragraph para, List<Run> runs)
    {
        // Group consecutive runs with same formatting
        var groups = new List<(int startIndex, int endIndex, string formatting)>();
        int groupStart = 0;
        string currentFormatting = GetRunFormattingKey(runs[0]);

        for (int i = 1; i <= runs.Count; i++)
        {
            string newFormatting = i < runs.Count ? GetRunFormattingKey(runs[i]) : "";
            if (i == runs.Count || newFormatting != currentFormatting)
            {
                groups.Add((groupStart, i - 1, currentFormatting));
                groupStart = i;
                currentFormatting = newFormatting;
            }
        }

        // Merge each group
        foreach (var group in groups)
        {
            if (group.endIndex > group.startIndex)
            {
                // Merge runs in this group
                var groupText = new StringBuilder();
                for (int i = group.startIndex; i <= group.endIndex; i++)
                {
                    foreach (var text in runs[i].Elements<Text>())
                    {
                        groupText.Append(text.Text ?? "");
                    }
                }

                // Put merged text in first run of group
                var firstText = runs[group.startIndex].GetFirstChild<Text>();
                if (firstText == null)
                {
                    firstText = new Text();
                    runs[group.startIndex].AppendChild(firstText);
                }
                firstText.Text = groupText.ToString();
                firstText.Space = SpaceProcessingModeValues.Preserve;

                // Clear text from other runs in group
                for (int i = group.startIndex + 1; i <= group.endIndex; i++)
                {
                    foreach (var t in runs[i].Elements<Text>().ToList())
                    {
                        t.Remove();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get a key representing the formatting of a run (Bold, Italic, etc.)
    /// </summary>
    private string GetRunFormattingKey(Run run)
    {
        var props = run.RunProperties;
        if (props == null) return "normal";

        var parts = new List<string>();
        if (props.Bold != null && (props.Bold.Val == null || props.Bold.Val.Value)) parts.Add("B");
        if (props.Italic != null && (props.Italic.Val == null || props.Italic.Val.Value)) parts.Add("I");
        if (props.Underline != null) parts.Add("U");

        return parts.Count > 0 ? string.Join("", parts) : "normal";
    }

    /// <summary>
    /// Merge split placeholders in paragraphs that contain field codes.
    /// Only merges consecutive non-field runs to preserve PAGE/NUMPAGES fields.
    /// </summary>
    private void MergeSplitPlaceholdersWithFields(Paragraph para, List<Run> runs)
    {
        // Identify runs that contain field elements
        var fieldRunIndices = new HashSet<int>();
        for (int i = 0; i < runs.Count; i++)
        {
            if (runs[i].Descendants<FieldChar>().Any() ||
                runs[i].Descendants<FieldCode>().Any() ||
                runs[i].Descendants<SimpleField>().Any())
            {
                fieldRunIndices.Add(i);
            }
        }

        // Find consecutive groups of non-field runs and merge them
        int groupStart = -1;
        for (int i = 0; i <= runs.Count; i++)
        {
            bool isFieldRun = i >= runs.Count || fieldRunIndices.Contains(i);

            if (!isFieldRun && groupStart == -1)
            {
                // Start a new group
                groupStart = i;
            }
            else if (isFieldRun && groupStart != -1)
            {
                // End current group and merge if it has multiple runs
                int groupEnd = i - 1;
                if (groupEnd > groupStart)
                {
                    MergeRunGroup(runs, groupStart, groupEnd);
                }
                groupStart = -1;
            }
        }
    }

    /// <summary>
    /// Merge a group of consecutive runs into the first run
    /// </summary>
    private void MergeRunGroup(List<Run> runs, int startIndex, int endIndex)
    {
        // Get full text from runs in this group
        var groupText = new StringBuilder();
        for (int i = startIndex; i <= endIndex; i++)
        {
            foreach (var text in runs[i].Elements<Text>())
            {
                groupText.Append(text.Text ?? "");
            }
        }

        // If no placeholder pattern in this group, skip
        if (!groupText.ToString().Contains("{{")) return;

        // Merge all text into first run's first text element
        var firstRun = runs[startIndex];
        var firstText = firstRun.GetFirstChild<Text>();

        if (firstText == null)
        {
            firstText = new Text();
            firstRun.AppendChild(firstText);
        }

        firstText.Text = groupText.ToString();
        firstText.Space = SpaceProcessingModeValues.Preserve;

        // Remove text from other runs in group (but keep runs for formatting)
        for (int i = startIndex + 1; i <= endIndex; i++)
        {
            foreach (var t in runs[i].Elements<Text>().ToList())
            {
                t.Remove();
            }
        }
    }

    /// <summary>
    /// Replace placeholders in header paragraphs while preserving PAGE/NUMPAGES field codes.
    /// This handles the case where header has format: P{PAGE}/{NUMPAGES} - {{NumberOfSample}}
    /// The field codes must be preserved for proper page numbering in the output document.
    /// </summary>
    private void ReplaceHeaderPlaceholdersPreservingFields(Paragraph para, Dictionary<string, string> placeholders)
    {
        var runs = para.Elements<Run>().ToList();
        if (runs.Count == 0) return;

        // Build a map of which runs contain field elements (skip these)
        var fieldRunIndices = new HashSet<int>();
        for (int i = 0; i < runs.Count; i++)
        {
            if (runs[i].Descendants<FieldChar>().Any() ||
                runs[i].Descendants<FieldCode>().Any())
            {
                fieldRunIndices.Add(i);
            }
        }

        // Collect text elements from non-field runs (keep reference to actual Text objects)
        var textElements = new List<Text>();
        for (int i = 0; i < runs.Count; i++)
        {
            if (fieldRunIndices.Contains(i)) continue;
            textElements.AddRange(runs[i].Elements<Text>());
        }

        if (textElements.Count == 0) return;

        // Get combined text from all non-field text elements
        var combinedText = string.Concat(textElements.Select(t => t.Text ?? ""));

        // Check each placeholder
        foreach (var placeholder in placeholders)
        {
            if (!combinedText.Contains(placeholder.Key)) continue;

            // Find position of placeholder in combined text
            var placeholderPos = combinedText.IndexOf(placeholder.Key);
            if (placeholderPos < 0) continue;

            // Find which text elements contain the placeholder
            int currentPos = 0;
            int startElementIdx = -1;
            int startPosInElement = -1;
            int endElementIdx = -1;
            int endPosInElement = -1;

            for (int i = 0; i < textElements.Count; i++)
            {
                var elementText = textElements[i].Text ?? "";
                var elementStart = currentPos;
                var elementEnd = currentPos + elementText.Length;

                // Check if placeholder starts in this element
                if (startElementIdx < 0 && placeholderPos >= elementStart && placeholderPos < elementEnd)
                {
                    startElementIdx = i;
                    startPosInElement = placeholderPos - elementStart;
                }

                // Check if placeholder ends in this element
                var placeholderEndPos = placeholderPos + placeholder.Key.Length;
                if (placeholderEndPos > elementStart && placeholderEndPos <= elementEnd)
                {
                    endElementIdx = i;
                    endPosInElement = placeholderEndPos - elementStart;
                    break;
                }

                currentPos = elementEnd;
            }

            if (startElementIdx < 0 || endElementIdx < 0) continue;

            // Replace the placeholder
            if (startElementIdx == endElementIdx)
            {
                // Placeholder is within a single text element
                var text = textElements[startElementIdx].Text ?? "";
                textElements[startElementIdx].Text =
                    text.Substring(0, startPosInElement) +
                    placeholder.Value +
                    text.Substring(endPosInElement);
            }
            else
            {
                // Placeholder spans multiple text elements
                // Put replacement value in first element, clear others
                var firstText = textElements[startElementIdx].Text ?? "";
                textElements[startElementIdx].Text =
                    firstText.Substring(0, startPosInElement) + placeholder.Value;

                // Clear middle elements completely
                for (int i = startElementIdx + 1; i < endElementIdx; i++)
                {
                    textElements[i].Text = "";
                }

                // Keep only the part after the placeholder in last element
                var lastText = textElements[endElementIdx].Text ?? "";
                textElements[endElementIdx].Text = lastText.Substring(endPosInElement);
            }

            // Update combined text for next placeholder
            combinedText = string.Concat(textElements.Select(t => t.Text ?? ""));
        }
    }

    /// <summary>
    /// Replaces a placeholder in a Text element with formatted runs from HTML content.
    /// This allows rich text formatting (bold, italic, subscript, superscript) in Word documents.
    /// </summary>
    private void ReplaceTextWithFormattedRuns(Text textElement, string placeholder, string htmlContent)
    {
        if (textElement.Parent is not Run parentRun)
            return;

        var text = textElement.Text ?? "";
        var placeholderIndex = text.IndexOf(placeholder);
        if (placeholderIndex == -1)
            return;

        var beforeText = text.Substring(0, placeholderIndex);
        var afterText = text.Substring(placeholderIndex + placeholder.Length);

        // Get base run properties from parent run
        var baseProps = parentRun.GetFirstChild<RunProperties>()?.CloneNode(true) as RunProperties;

        // Convert HTML to runs
        var formattedRuns = HtmlToOpenXmlConverter.ConvertHtmlToRuns(htmlContent, baseProps);

        // Clear original text
        textElement.Text = beforeText;

        // If there's after text, create a new run for it
        Run? afterRun = null;
        if (!string.IsNullOrEmpty(afterText))
        {
            afterRun = new Run();
            if (baseProps != null)
            {
                afterRun.Append(baseProps.CloneNode(true));
            }
            afterRun.Append(new Text(afterText) { Space = SpaceProcessingModeValues.Preserve });
        }

        // Insert formatted runs after parent run
        var insertAfter = parentRun;
        foreach (var run in formattedRuns)
        {
            insertAfter.InsertAfterSelf(run);
            insertAfter = run;
        }

        // Insert after text run at the end
        if (afterRun != null)
        {
            insertAfter.InsertAfterSelf(afterRun);
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
        data.Parameters = data.Parameters?.Trim() ?? "";
        data.ParametersFormattedName = data.ParametersFormattedName?.Trim();
        data.Unit = data.Unit?.Trim() ?? "";
        data.TestMethod = data.TestMethod?.Trim() ?? "";
        data.TestMethodFormattedName = data.TestMethodFormattedName?.Trim();
        data.Result = data.Result?.Trim() ?? "";
        data.NameOfSample = data.NameOfSample?.Trim() ?? "";
        data.CodeOfNumber = data.CodeOfNumber?.Trim() ?? "";
        data.Standards = data.Standards?.Trim() ?? "";
        data.CompareWithStandard = data.CompareWithStandard?.Trim() ?? "";
        data.Allowlimit = data.Allowlimit?.Trim() ?? "";
        data.Uncertainty = data.Uncertainty?.Trim() ?? "";
        data.Proclaim = data.Proclaim?.Trim() ?? "";
        data.FootnoteNumber = data.FootnoteNumber?.Trim() ?? "";

        // Clone the template row
        var newRow = (TableRow)template.CloneNode(true);

        // Remove fixed row height to allow row to auto-expand with content
        RemoveRowHeightConstraint(newRow);

        // Allow page break inside row for long content
        AllowPageBreakInRow(newRow);

        // Enable text wrapping in all cells to prevent content overflow
        EnableTextWrappingInRow(newRow);

        // Copy cell borders from template to preserve border styles (dashed, dotted, etc.)
        CopyCellBordersFromTemplate(newRow, template);

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
            // Handle Parameters, TestMethod, CompareWithStandard, Allowlimit, Standards placeholders with HTML formatting or newlines
            else if (text.Text.Contains("{{Parameters}}") || text.Text.Contains("{{TestMethod}}") ||
                     text.Text.Contains("{{CompareWithStandard}}") || text.Text.Contains("{{Allowlimit}}") ||
                     text.Text.Contains("{{Standards}}"))
            {
                // Use formatted names if available, otherwise plain names
                var parametersDisplayName = !string.IsNullOrEmpty(data.ParametersFormattedName)
                    ? data.ParametersFormattedName
                    : data.Parameters;
                var testMethodDisplayName = !string.IsNullOrEmpty(data.TestMethodFormattedName)
                    ? data.TestMethodFormattedName
                    : data.TestMethod;

                var hasParametersHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(parametersDisplayName);
                var hasTestMethodHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(testMethodDisplayName);
                var hasCompareWithStandardHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(data.CompareWithStandard);
                var hasAllowlimitHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(data.Allowlimit);
                var hasStandardsHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(data.Standards);
                var hasUnitHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(data.Unit);
                var hasParametersNewlines = parametersDisplayName?.Contains('\n') ?? false;
                var hasTestMethodNewlines = testMethodDisplayName?.Contains('\n') ?? false;
                var hasCompareWithStandardNewlines = data.CompareWithStandard?.Contains('\n') ?? false;
                var hasAllowlimitNewlines = data.Allowlimit?.Contains('\n') ?? false;
                var hasStandardsNewlines = data.Standards?.Contains('\n') ?? false;

                // If any field contains HTML or newlines, we need special handling
                if (hasParametersHtml || hasTestMethodHtml || hasCompareWithStandardHtml || hasAllowlimitHtml || hasStandardsHtml || hasUnitHtml ||
                    hasParametersNewlines || hasTestMethodNewlines || hasCompareWithStandardNewlines || hasAllowlimitNewlines || hasStandardsNewlines)
                {
                    // Replace non-HTML placeholders first
                    text.Text = text.Text
                        .Replace("{{TestTable}}", "")
                        .Replace("{{No}}", data.No.ToString())
                        .Replace("{{NameOfSample}}", data.NameOfSample ?? "")
                        .Replace("{{CodeOfNumber}}", data.CodeOfNumber ?? "")
                        .Replace("{{Result}}", data.Result ?? "")
                        .Replace("{{n}}", data.FootnoteNumber ?? "")
                        .Replace("{{Uncertainty}}", data.Uncertainty ?? "")
                        .Replace("{{Proclaim}}", data.Proclaim ?? "");

                    // Handle Unit with HTML if applicable
                    if (hasUnitHtml && text.Text.Contains("{{Unit}}"))
                    {
                        ReplaceTextWithFormattedRuns(text, "{{Unit}}", data.Unit ?? "");
                    }
                    else
                    {
                        text.Text = text.Text.Replace("{{Unit}}", data.Unit ?? "");
                    }

                    // Handle Parameters with HTML/newlines if applicable
                    if ((hasParametersHtml || hasParametersNewlines) && text.Text.Contains("{{Parameters}}"))
                    {
                        // Use ReplaceTextWithFormattedRuns which now handles both HTML and \n newlines
                        ReplaceTextWithFormattedRuns(text, "{{Parameters}}", parametersDisplayName);
                    }
                    else
                    {
                        text.Text = text.Text.Replace("{{Parameters}}", parametersDisplayName ?? "");
                    }

                    // Handle TestMethod with HTML/newlines if applicable
                    if ((hasTestMethodHtml || hasTestMethodNewlines) && text.Text.Contains("{{TestMethod}}"))
                    {
                        ReplaceTextWithFormattedRuns(text, "{{TestMethod}}", testMethodDisplayName);
                    }
                    else
                    {
                        text.Text = text.Text.Replace("{{TestMethod}}", testMethodDisplayName ?? "");
                    }

                    // Handle CompareWithStandard with HTML or newlines (superscript/subscript in limits, or multiple children)
                    if ((hasCompareWithStandardHtml || hasCompareWithStandardNewlines) && text.Text.Contains("{{CompareWithStandard}}"))
                    {
                        ReplaceTextWithFormattedRuns(text, "{{CompareWithStandard}}", data.CompareWithStandard ?? "");
                    }
                    else
                    {
                        text.Text = text.Text.Replace("{{CompareWithStandard}}", data.CompareWithStandard ?? "");
                    }

                    // Handle Allowlimit with HTML or newlines (superscript/subscript in limits, or multiple children)
                    if ((hasAllowlimitHtml || hasAllowlimitNewlines) && text.Text.Contains("{{Allowlimit}}"))
                    {
                        ReplaceTextWithFormattedRuns(text, "{{Allowlimit}}", data.Allowlimit ?? "");
                    }
                    else
                    {
                        text.Text = text.Text.Replace("{{Allowlimit}}", data.Allowlimit ?? "");
                    }

                    // Handle Standards with HTML or newlines (superscript/subscript in limits, or multiple children)
                    if ((hasStandardsHtml || hasStandardsNewlines) && text.Text.Contains("{{Standards}}"))
                    {
                        ReplaceTextWithFormattedRuns(text, "{{Standards}}", data.Standards ?? "");
                    }
                    else
                    {
                        text.Text = text.Text.Replace("{{Standards}}", data.Standards ?? "");
                    }
                }
                else
                {
                    // No HTML and no newlines - simple replacement
                    text.Text = text.Text
                        .Replace("{{TestTable}}", "")
                        .Replace("{{No}}", data.No.ToString())
                        .Replace("{{NameOfSample}}", data.NameOfSample ?? "")
                        .Replace("{{CodeOfNumber}}", data.CodeOfNumber ?? "")
                        .Replace("{{Parameters}}", parametersDisplayName ?? "")
                        .Replace("{{Unit}}", data.Unit ?? "")
                        .Replace("{{TestMethod}}", testMethodDisplayName ?? "")
                        .Replace("{{Result}}", data.Result ?? "")
                        .Replace("{{Standards}}", data.Standards ?? "")
                        .Replace("{{CompareWithStandard}}", data.CompareWithStandard ?? "")
                        .Replace("{{n}}", data.FootnoteNumber ?? "")
                        .Replace("{{Allowlimit}}", data.Allowlimit ?? "")
                        .Replace("{{Uncertainty}}", data.Uncertainty ?? "")
                        .Replace("{{Proclaim}}", data.Proclaim ?? "");
                }
            }
            else
            {
                text.Text = text.Text
                    .Replace("{{TestTable}}", "")
                    .Replace("{{No}}", data.No.ToString())
                    .Replace("{{NameOfSample}}", data.NameOfSample ?? "") // BM02 - Tên mẫu
                    .Replace("{{CodeOfNumber}}", data.CodeOfNumber ?? "") // BM02 - Mã số mẫu
                    .Replace("{{Parameters}}", data.Parameters ?? "")
                    .Replace("{{TestMethod}}", data.TestMethod ?? "")
                    .Replace("{{Result}}", data.Result ?? "")
                    .Replace("{{Standards}}", data.Standards ?? "")
                    .Replace("{{CompareWithStandard}}", data.CompareWithStandard ?? "")
                    .Replace("{{n}}", data.FootnoteNumber ?? "") // BM05-QLNN - Superscript footnote number
                    .Replace("{{Allowlimit}}", data.Allowlimit ?? "") // BM04 - Giới hạn cho phép
                    .Replace("{{Uncertainty}}", data.Uncertainty ?? "")
                    .Replace("{{Proclaim}}", data.Proclaim ?? "");

                // Handle Unit with HTML if applicable
                if (HtmlToOpenXmlConverter.ContainsHtmlTags(data.Unit) && text.Text.Contains("{{Unit}}"))
                {
                    ReplaceTextWithFormattedRuns(text, "{{Unit}}", data.Unit ?? "");
                }
                else
                {
                    text.Text = text.Text.Replace("{{Unit}}", data.Unit ?? "");
                }
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
    /// Replace a Note placeholder with HTML-formatted OpenXML runs.
    /// Supports bold, italic, underline, strikethrough, superscript, subscript, lists.
    /// </summary>
    private void ReplaceNoteWithHtmlRuns(Text textElement, string placeholder, string htmlValue)
    {
        var parentRun = textElement.Parent as Run;
        if (parentRun == null)
        {
            // Fallback - strip HTML and replace as plain text
            textElement.Text = textElement.Text.Replace(placeholder, HtmlToOpenXmlConverter.ConvertHtmlToPlainText(htmlValue));
            return;
        }

        var parentParagraph = parentRun.Parent as Paragraph;
        if (parentParagraph == null)
        {
            textElement.Text = textElement.Text.Replace(placeholder, HtmlToOpenXmlConverter.ConvertHtmlToPlainText(htmlValue));
            return;
        }

        // Get base RunProperties from the parent run (includes font size set by SetNoteFontSize)
        var existingRunProps = parentRun.RunProperties?.CloneNode(true) as RunProperties;

        // Convert HTML to formatted runs
        var htmlRuns = HtmlToOpenXmlConverter.ConvertHtmlToRuns(htmlValue, existingRunProps);

        // Remove trailing break from last run (avoid extra blank line at end)
        if (htmlRuns.Count > 0)
        {
            var lastRun = htmlRuns[htmlRuns.Count - 1];
            var lastChild = lastRun.LastChild;
            if (lastChild is Break)
            {
                lastChild.Remove();
            }
        }

        // Handle text before/after placeholder in the same Text element
        var originalText = textElement.Text;
        var placeholderIndex = originalText.IndexOf(placeholder);
        if (placeholderIndex < 0) return;

        var beforeText = originalText.Substring(0, placeholderIndex);
        var afterText = originalText.Substring(placeholderIndex + placeholder.Length);

        // Clear the placeholder from original text
        textElement.Text = beforeText;

        // Insert HTML runs after the parent run
        var insertAfter = parentRun as OpenXmlElement;
        foreach (var htmlRun in htmlRuns)
        {
            parentParagraph.InsertAfter(htmlRun, insertAfter);
            insertAfter = htmlRun;
        }

        // Add after text if any (in a new run with base properties)
        if (!string.IsNullOrEmpty(afterText))
        {
            var afterRun = new Run();
            if (existingRunProps != null)
            {
                afterRun.Append(existingRunProps.CloneNode(true));
            }
            afterRun.Append(new Text(afterText) { Space = SpaceProcessingModeValues.Preserve });
            parentParagraph.InsertAfter(afterRun, insertAfter);
        }
    }

    /// <summary>
    /// Remove fixed height constraint from a table row to allow auto-expansion
    /// Changes height rule from "exact" to "atLeast" so row can grow with content
    /// </summary>
    private void RemoveRowHeightConstraint(TableRow row)
    {
        var rowProps = row.GetFirstChild<TableRowProperties>();
        if (rowProps != null)
        {
            var rowHeight = rowProps.GetFirstChild<TableRowHeight>();
            if (rowHeight != null)
            {
                // Change from "exact" to "atLeast" - this allows row to expand
                // "exact" = fixed height (content gets clipped)
                // "atLeast" = minimum height, can grow if content needs more space
                // "auto" = automatic height based on content
                if (rowHeight.HeightType?.Value == HeightRuleValues.Exact)
                {
                    rowHeight.HeightType = HeightRuleValues.AtLeast;
                }
                else if (rowHeight.HeightType == null)
                {
                    // If no rule specified, set to atLeast to ensure expansion
                    rowHeight.HeightType = HeightRuleValues.AtLeast;
                }
            }
        }
        else
        {
            // If no row properties, create with auto height
            rowProps = new TableRowProperties();
            row.InsertAt(rowProps, 0);
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
    /// Apply consistent dashed gray border style to all cells in new row.
    /// Style: dashSmallGap, size 4, color 7F7F7F (gray)
    /// This ensures all data rows have the same professional appearance.
    /// </summary>
    private void CopyCellBordersFromTemplate(TableRow newRow, TableRow templateRow)
    {
        var newCells = newRow.Elements<TableCell>().ToList();

        foreach (var cell in newCells)
        {
            var cellProps = cell.GetFirstChild<TableCellProperties>();
            if (cellProps == null)
            {
                cellProps = new TableCellProperties();
                cell.InsertAt(cellProps, 0);
            }

            // Remove existing borders
            cellProps.GetFirstChild<TableCellBorders>()?.Remove();

            // Create consistent dashed gray borders for left, bottom, right only
            // Note: No TopBorder to preserve header's bottom border style
            var borders = new TableCellBorders(
                new LeftBorder { Val = BorderValues.DashSmallGap, Size = 4, Space = 0, Color = "7F7F7F" },
                new BottomBorder { Val = BorderValues.DashSmallGap, Size = 4, Space = 0, Color = "7F7F7F" },
                new RightBorder { Val = BorderValues.DashSmallGap, Size = 4, Space = 0, Color = "7F7F7F" }
            );

            cellProps.Append(borders);
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
        /// <summary>
        /// Tên chỉ tiêu có định dạng HTML (nếu có)
        /// </summary>
        public string? ParametersFormattedName { get; set; }
        public string Unit { get; set; } = "";
        public string TestMethod { get; set; } = "";
        /// <summary>
        /// Tên phương pháp có định dạng HTML (nếu có)
        /// </summary>
        public string? TestMethodFormattedName { get; set; }
        public string Result { get; set; } = "";
        public string Standards { get; set; } = "";
        public string CompareWithStandard { get; set; } = ""; // For BM05-QLNN template
        public string FootnoteNumber { get; set; } = ""; // For BM05-QLNN template - {{n}} placeholder (superscript)
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
            LaboratoryTaskStatus.UnderEvaluation => "Chưa giao việc",
            LaboratoryTaskStatus.Assigned => "Đã giao việc",
            LaboratoryTaskStatus.InProgress => "Đang thử nghiệm",
            LaboratoryTaskStatus.WaitingApproval => "Chờ duyệt kết quả",
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

        // Load TestMethodIndicator for per-indicator formatted unit
        var methodIndicators = methodIds.Any() && indicatorIds.Any()
            ? await _testMethodIndicatorRepository.GetListAsync(x =>
                methodIds.Contains(x.TestMethodId) && indicatorIds.Contains(x.IndicatorId))
            : new List<TestMethodIndicator>();

        // Calculate TimeOfTesting: Min(TestDate) - Max(SubmitDate) of selected sample's indicators only
        // - Ngày chỉ tiêu đầu tiên thử nghiệm: Ngày KTV check-in chỉ tiêu đầu tiên của mẫu được chọn
        // - Ngày gửi kết quả cuối cùng: Ngày gửi kết quả (SubmitDate) muộn nhất của mẫu được chọn
        var timeOfTesting = CalculateTimeOfTesting(taskIndicators);

        // 2. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var printDate = input.PrintDate
            ?? await GetFirstPrintDateForTestRequestAsync(input.TestRequestId)
            ?? DateTime.Now;
        var printDateText = printDate.ToString("dd/MM/yyyy");
        var contractDateText = testRequest.ContractDate?.ToString("dd/MM/yyyy") ?? "";
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", sample.SampleCode ?? "" },
            { "{{CurrentDate}}", printDateText },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{SamplingAddress}}", sample.SamplingAddress ?? "" },
            { "{{NameOfSample}}", sample.SampleName ?? "" },
            { "{{DateOfReceiving}}", contractDateText },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", sample.StatusText ?? "" },
            { "{{Quantity}}", sample.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", input.Note ?? "" },
            // BM05-QLNN specific placeholders
            { "{{NumberOfSample}}", sample.SampleCode ?? "" },
            { "{{SampleQuantity}}", sample.SampleQuantity ?? "" },
            { "{{DescriptionOfSample}}", sample.StatusText ?? "" },
            { "{{TimeToSaveSample}}", "" },
            { "{{DateOfSending}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{PlaceToSendSample}}", testRequest.CustomerName ?? "" },
            { "{{AttachFile}}", "" },
            { "{{Conclude}}", "" } // BM05-QLNN - Kết luận (để trống, user tự điền)
        };

        // Add Officer In Charge placeholder (QLKT who approved the indicators)
        // Only shows name if exactly 1 approver, otherwise empty
        var officerInChargeName = await GetOfficerInChargeNameAsync(taskIndicators);
        placeholders.Add("{{OfficerInCharge}}", officerInChargeName);

        // 3. Build table data (BM01 has fewer columns than BM03)
        // Handle parent-child indicator grouping, skip CustomerCancelled, use "-" for empty unit
        var tableData = await BuildTableDataWithParentChildGroupingAsync(
            sampleIndicators,
            indicators,
            indicatorDict,
            taskIndicators,
            methodDict,
            methodIndicators,
            skipCustomerCancelled: true,
            useHyphenForEmptyUnit: true);

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
    /// In new flow: printing is available when status = Paid
    /// </summary>
    public async Task<GenerateTestRequestFormOutput> GenerateTestRequestFormAsync(GenerateTestRequestFormInput input)
    {
        // 1. Load test request data with samples
        var testRequest = await _testRequestRepository.GetAsync(input.TestRequestId);
        var samples = await _sampleRepository.GetListAsync(x => x.TestRequestId == input.TestRequestId);

        // 2. Check if template exists
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

        // 7. Auto-set FormPrintedDate based on indicator changes
        var sampleIds = samples.Select(s => s.Id).ToList();
        var allIndicatorIds = (await _sampleIndicatorRepository.GetListAsync(
                x => sampleIds.Contains(x.TestRequestSampleId)))
            .Select(si => si.Id)
            .OrderBy(id => id)
            .ToList();
        var currentHash = ComputeIndicatorHash(allIndicatorIds);

        if (!testRequest.FormPrintedDate.HasValue || testRequest.LastPrintedIndicatorHash != currentHash)
        {
            testRequest.FormPrintedDate = Clock.Now;
            testRequest.LastPrintedIndicatorHash = currentHash;
            await _testRequestRepository.UpdateAsync(testRequest);
        }

        // 8. Process document - replace placeholders
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

        // 8. Return DOCX file directly (no PDF conversion) for OnlyOffice editing
        var docxFileName = Path.GetFileName(outputPath);
        _logger.LogInformation("Test request form generated: {Path}", outputPath);

        // 9. Update TestRequest status: New -> PendingPayment when printing
        if (testRequest.Status == TestRequestStatus.New)
        {
            testRequest.Status = TestRequestStatus.PendingPayment;
            await _testRequestRepository.UpdateAsync(testRequest);
            _logger.LogInformation("TestRequest {Id} status changed from New to PendingPayment after printing", input.TestRequestId);
        }

        return new GenerateTestRequestFormOutput
        {
            FileName = docxFileName,
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
            var sampleTableData = await BuildPreviewSampleTableDataAsync(input.Samples);
            ProcessTestRequestFormDocumentWithTable(outputPath, placeholders, sampleTableData);
        }

        // 6. Return DOCX file directly (no PDF conversion) for OnlyOffice editing
        var docxFileName = Path.GetFileName(outputPath);
        _logger.LogInformation("Preview test request form generated: {Path}", outputPath);

        return new GenerateTestRequestFormOutput
        {
            FileName = docxFileName,
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
        var wardText = LowercaseAdministrativePrefix(input.Ward, "Phường", "Xã");
        var districtText = LowercaseAdministrativePrefix(input.District, "Quận");
        var provinceText = LowercaseAdministrativePrefix(input.Province, "Tỉnh", "Thành phố");
        var resultCopies = input.ResultCopyCount ?? "";
        // Chỉ hiển thị CustomerTestMethod khi MethodCustomer = true
        var customerTestMethod = input.MethodCustomer ? (input.MethodCustomerDetail ?? "") : "";
        var compareStandard = input.ConformityDeclaration ?? "";
        var otherRequirements = input.OtherRequirements ?? "";
        var subContractInfo = input.SubContract ?? "";
        var currentDate = DateTime.Now;
        var contractDate = input.ContractDate?.Date.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "";
        // Mẫu nhiệm vụ hiển thị 0
        var actualTotalCost = input.SampleTypeTask ? 0 : input.TotalCost;
        var totalAmount = actualTotalCost.ToString("N0");
        var totalByText = input.SampleTypeTask
            ? "Không đồng"
            : (input.TotalCostInWords ?? ConvertNumberToVietnameseWords(input.TotalCost));
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
            { "{{DateReceiving}}", contractDate },
            { "{{DateOfReceiving}}", contractDate },
            { "{{TotalAmount}}", totalAmount },
            { "{{TotalByText}}", totalByText },
            { "{{ReceiptNumberTTDV}}", receiptNumber },
            { "{{FormPrintedDate}}", input.FormPrintedDate?.ToString("dd/MM/yyyy") ?? currentDateText },
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
            { "{{CB_Credit}}", TemplateConstants.UncheckedBox },

            // Người gửi mẫu và người nhận mẫu
            { "{{SampleSender}}", input.ContactPerson ?? "" },
            { "{{SampleReceiver}}", string.IsNullOrWhiteSpace($"{_currentUser.SurName} {_currentUser.Name}".Trim()) ? _currentUser.UserName ?? "" : $"{_currentUser.SurName} {_currentUser.Name}".Trim() }
        };

        return placeholders;
    }

    /// <summary>
    /// Build sample table data from preview DTO with parent-child grouping
    /// Uses DB lookup for parent-child relationships
    /// </summary>
    private async Task<List<TestRequestSampleTableRow>> BuildPreviewSampleTableDataAsync(List<PreviewTestRequestSampleDto> samples)
    {
        var tableData = new List<TestRequestSampleTableRow>();
        int rowIndex = 1;

        // Get all indicator IDs from all samples
        var allIndicatorIds = samples
            .SelectMany(s => s.Indicators.Select(i => i.IndicatorId))
            .Distinct()
            .ToList();

        // Fetch indicators from DB to get parent-child relationships
        var indicators = allIndicatorIds.Any()
            ? await _indicatorRepository.GetListAsync(x => allIndicatorIds.Contains(x.Id))
            : new List<Indicator>();
        var indicatorDict = indicators.ToDictionary(i => i.Id);

        foreach (var sample in samples.OrderBy(s => s.SampleCode))
        {
            // Build indicator details with parent-child grouping
            var indicatorDetails = BuildPreviewIndicatorSubTableWithGrouping(sample.Indicators, indicatorDict);

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
    /// Build indicator sub-table rows with parent-child grouping for preview mode
    /// Groups by indicatorId + testMethodId + specimenTypeId to support same indicator with different methods
    /// </summary>
    private List<IndicatorSubTableRow> BuildPreviewIndicatorSubTableWithGrouping(
        List<PreviewSampleIndicatorDto> previewIndicators,
        Dictionary<Guid, Indicator> indicatorDict)
    {
        var indicatorDetails = new List<IndicatorSubTableRow>();
        int subRowIndex = 1;

        // Helper to create composite key: indicatorId_testMethodId_specimenTypeId
        string MakeKey(Guid indicatorId, Guid? testMethodId, Guid? specimenTypeId) =>
            $"{indicatorId}_{testMethodId?.ToString() ?? "null"}_{specimenTypeId?.ToString() ?? "null"}";

        // Track which indicator+method+specimen combinations have been processed
        var processedKeys = new HashSet<string>();

        // Create a map from composite key to preview indicator
        var indicatorMap = previewIndicators
            .Where(pi => indicatorDict.ContainsKey(pi.IndicatorId))
            .ToDictionary(pi => MakeKey(pi.IndicatorId, pi.TestMethodId, pi.SpecimenTypeId));

        // Sort by indicator sort order from DB
        var sortedIndicators = previewIndicators
            .Where(pi => indicatorDict.ContainsKey(pi.IndicatorId))
            .OrderBy(pi => indicatorDict[pi.IndicatorId].SortOrder)
            .ThenBy(pi => pi.TestMethodId)
            .ThenBy(pi => pi.SpecimenTypeId)
            .ToList();

        foreach (var previewInd in sortedIndicators)
        {
            var currentKey = MakeKey(previewInd.IndicatorId, previewInd.TestMethodId, previewInd.SpecimenTypeId);
            if (processedKeys.Contains(currentKey))
                continue;

            var indicator = indicatorDict[previewInd.IndicatorId];

            // Check if this indicator is a child (has parent)
            if (indicator.ParentIndicatorId.HasValue)
            {
                // Check if parent with same testMethodId and specimenTypeId is in the sample
                var parentKey = MakeKey(indicator.ParentIndicatorId.Value, previewInd.TestMethodId, previewInd.SpecimenTypeId);
                if (indicatorMap.ContainsKey(parentKey))
                    continue; // Will be processed with parent
            }

            // Check if this indicator has children in the sample with same testMethodId and specimenTypeId
            var childrenInSample = sortedIndicators
                .Where(pi =>
                {
                    if (!indicatorDict.ContainsKey(pi.IndicatorId))
                        return false;
                    var childInd = indicatorDict[pi.IndicatorId];
                    // Child must have same parent, testMethodId and specimenTypeId
                    return childInd.ParentIndicatorId == indicator.Id
                        && pi.TestMethodId == previewInd.TestMethodId
                        && pi.SpecimenTypeId == previewInd.SpecimenTypeId;
                })
                .OrderBy(pi => indicatorDict[pi.IndicatorId].SortOrder)
                .ToList();

            if (childrenInSample.Any())
            {
                // This is a parent with children - create grouped row
                var namePlainBuilder = new StringBuilder();
                var nameFormattedBuilder = new StringBuilder();
                decimal totalPrice = previewInd.UnitPrice;
                bool hasAnyFormattedName = !string.IsNullOrEmpty(indicator.FormattedName);

                namePlainBuilder.Append(indicator.Name);
                nameFormattedBuilder.Append(indicator.FormattedName ?? indicator.Name);

                foreach (var childPreview in childrenInSample)
                {
                    var childInd = indicatorDict[childPreview.IndicatorId];
                    if (!string.IsNullOrEmpty(childInd.FormattedName))
                        hasAnyFormattedName = true;

                    namePlainBuilder.Append("\n- ");
                    namePlainBuilder.Append(childInd.Name);

                    nameFormattedBuilder.Append("\n- ");
                    nameFormattedBuilder.Append(childInd.FormattedName ?? childInd.Name);

                    totalPrice += childPreview.UnitPrice;
                    var childKey = MakeKey(childPreview.IndicatorId, childPreview.TestMethodId, childPreview.SpecimenTypeId);
                    processedKeys.Add(childKey);
                }

                indicatorDetails.Add(new IndicatorSubTableRow
                {
                    No = subRowIndex++,
                    IndicatorName = namePlainBuilder.ToString(),
                    IndicatorFormattedName = hasAnyFormattedName ? nameFormattedBuilder.ToString() : null,
                    UnitPrice = totalPrice.ToString("N0")
                });
            }
            else
            {
                // Single indicator (no children or orphan child)
                // Use DB values for name/formattedName to ensure consistency
                indicatorDetails.Add(new IndicatorSubTableRow
                {
                    No = subRowIndex++,
                    IndicatorName = indicator.Name ?? previewInd.IndicatorName,
                    IndicatorFormattedName = indicator.FormattedName ?? previewInd.IndicatorFormattedName,
                    UnitPrice = previewInd.UnitPrice.ToString("N0")
                });
            }

            processedKeys.Add(currentKey);
        }

        // Handle any indicators not in DB (shouldn't happen but just in case)
        foreach (var previewInd in previewIndicators)
        {
            var key = MakeKey(previewInd.IndicatorId, previewInd.TestMethodId, previewInd.SpecimenTypeId);
            if (!processedKeys.Contains(key) && !indicatorDict.ContainsKey(previewInd.IndicatorId))
            {
                indicatorDetails.Add(new IndicatorSubTableRow
                {
                    No = subRowIndex++,
                    IndicatorName = previewInd.IndicatorName,
                    IndicatorFormattedName = previewInd.IndicatorFormattedName,
                    UnitPrice = previewInd.UnitPrice.ToString("N0")
                });
            }
        }

        return indicatorDetails;
    }

    /// <summary>
    /// Build sample table data for test request form
    /// Supports parent-child indicator grouping: Parent displays with its children
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

        foreach (var sample in samples.OrderBy(s => s.SampleCode))
        {
            // Get sample indicators for this sample with their prices
            var sampleIndicatorList = allSampleIndicators
                .Where(si => si.TestRequestSampleId == sample.Id)
                .ToList();

            // Build indicator details for sub-table with parent-child grouping
            var indicatorDetails = BuildIndicatorSubTableWithGrouping(sampleIndicatorList, indicatorDict);

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
    /// Build indicator sub-table rows with parent-child grouping
    /// Parent indicator will include its children: "Parent\n- Child1\n- Child2"
    /// Groups by indicatorId + testMethodId + specimenTypeId to support same indicator with different methods
    /// </summary>
    private List<IndicatorSubTableRow> BuildIndicatorSubTableWithGrouping(
        List<TestRequestSampleIndicator> sampleIndicators,
        Dictionary<Guid, Indicator> indicatorDict)
    {
        var indicatorDetails = new List<IndicatorSubTableRow>();
        int subRowIndex = 1;

        // Helper to create composite key: indicatorId_testMethodId_specimenTypeId
        string MakeKey(Guid indicatorId, Guid? testMethodId, Guid? specimenTypeId) =>
            $"{indicatorId}_{testMethodId?.ToString() ?? "null"}_{specimenTypeId?.ToString() ?? "null"}";

        // Track which indicator+method+specimen combinations have been processed
        var processedKeys = new HashSet<string>();

        // Create a map from composite key to sample indicator
        var indicatorMap = sampleIndicators
            .Where(si => indicatorDict.ContainsKey(si.IndicatorId))
            .ToDictionary(si => MakeKey(si.IndicatorId, si.TestMethodId, si.SpecimenTypeId));

        // Sort sample indicators by indicator sort order
        var sortedSampleIndicators = sampleIndicators
            .Where(si => indicatorDict.ContainsKey(si.IndicatorId))
            .OrderBy(si => indicatorDict[si.IndicatorId].SortOrder)
            .ThenBy(si => si.TestMethodId)
            .ThenBy(si => si.SpecimenTypeId)
            .ToList();

        foreach (var si in sortedSampleIndicators)
        {
            var currentKey = MakeKey(si.IndicatorId, si.TestMethodId, si.SpecimenTypeId);
            if (processedKeys.Contains(currentKey))
                continue;

            var indicator = indicatorDict[si.IndicatorId];

            // Check if this indicator is a child (has parent)
            if (indicator.ParentIndicatorId.HasValue)
            {
                // Check if parent with same testMethodId and specimenTypeId is in the sample
                var parentKey = MakeKey(indicator.ParentIndicatorId.Value, si.TestMethodId, si.SpecimenTypeId);
                if (indicatorMap.ContainsKey(parentKey))
                    continue; // Will be processed with parent
            }

            // Check if this indicator has children in the sample with same testMethodId and specimenTypeId
            var childrenInSample = sortedSampleIndicators
                .Where(csi =>
                {
                    if (!indicatorDict.ContainsKey(csi.IndicatorId))
                        return false;
                    var childInd = indicatorDict[csi.IndicatorId];
                    // Child must have same parent, testMethodId and specimenTypeId
                    return childInd.ParentIndicatorId == indicator.Id
                        && csi.TestMethodId == si.TestMethodId
                        && csi.SpecimenTypeId == si.SpecimenTypeId;
                })
                .OrderBy(csi => indicatorDict[csi.IndicatorId].SortOrder)
                .ToList();

            if (childrenInSample.Any())
            {
                // This is a parent with children - create grouped row
                var namePlainBuilder = new StringBuilder();
                var nameFormattedBuilder = new StringBuilder();
                decimal totalPrice = si.UnitPrice;
                bool hasAnyFormattedName = !string.IsNullOrEmpty(indicator.FormattedName);

                namePlainBuilder.Append(indicator.Name);
                nameFormattedBuilder.Append(indicator.FormattedName ?? indicator.Name);

                foreach (var childSi in childrenInSample)
                {
                    var childInd = indicatorDict[childSi.IndicatorId];
                    if (!string.IsNullOrEmpty(childInd.FormattedName))
                        hasAnyFormattedName = true;

                    namePlainBuilder.Append("\n- ");
                    namePlainBuilder.Append(childInd.Name);

                    nameFormattedBuilder.Append("\n- ");
                    nameFormattedBuilder.Append(childInd.FormattedName ?? childInd.Name);

                    totalPrice += childSi.UnitPrice;
                    var childKey = MakeKey(childSi.IndicatorId, childSi.TestMethodId, childSi.SpecimenTypeId);
                    processedKeys.Add(childKey);
                }

                indicatorDetails.Add(new IndicatorSubTableRow
                {
                    No = subRowIndex++,
                    IndicatorName = namePlainBuilder.ToString(),
                    IndicatorFormattedName = hasAnyFormattedName ? nameFormattedBuilder.ToString() : null,
                    UnitPrice = totalPrice.ToString("N0")
                });
            }
            else
            {
                // Single indicator (no children or orphan child)
                indicatorDetails.Add(new IndicatorSubTableRow
                {
                    No = subRowIndex++,
                    IndicatorName = indicator.Name ?? "",
                    IndicatorFormattedName = indicator.FormattedName,
                    UnitPrice = si.UnitPrice.ToString("N0")
                });
            }

            processedKeys.Add(currentKey);
        }

        return indicatorDetails;
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
        placeholders = TrimPlaceholderValues(placeholders);

        // Process main body
        if (mainPart.Document?.Body != null)
        {
            ReplacePlaceholdersInElement(mainPart.Document.Body, placeholders);
        }

        // Process headers - special handling for headers with PAGE/NUMPAGES fields
        foreach (var headerPart in mainPart.HeaderParts)
        {
            if (headerPart.Header != null)
            {
                _logger.LogDebug("Processing header part, paragraphs count: {Count}",
                    headerPart.Header.Descendants<Paragraph>().Count());

                // Special handling for headers with field codes (PAGE, NUMPAGES)
                foreach (var para in headerPart.Header.Descendants<Paragraph>())
                {
                    ReplaceHeaderPlaceholdersPreservingFields(para, placeholders);
                }
            }
        }

        // Process footers
        foreach (var footerPart in mainPart.FooterParts)
        {
            if (footerPart.Footer != null)
            {
                ReplacePlaceholdersInElement(footerPart.Footer, placeholders);

                // Also process any text directly in footer (not in paragraphs)
                foreach (var text in footerPart.Footer.Descendants<Text>())
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

        // Merge dots that follow placeholders into the same text element
        foreach (var para in element.Descendants<Paragraph>())
        {
            MergeDotsWithPlaceholders(para);
        }

        // Handle HTML/newline content in Note placeholder BEFORE standard processing
        if (placeholders.TryGetValue("{{Note}}", out var noteValue) && !string.IsNullOrEmpty(noteValue))
        {
            foreach (var text in element.Descendants<Text>().ToList())
            {
                if (!string.IsNullOrEmpty(text.Text) && text.Text.Contains("{{Note}}"))
                {
                    RemoveContainingCellHeightConstraint(text);
                    // Get font size from placeholder in template and preserve it
                    var fontSize = GetFontSizeFromText(text);
                    SetNoteFontSize(text, fontSize);
                    NormalizeParagraphIndentation(text);

                    if (HtmlToOpenXmlConverter.ContainsHtmlTags(noteValue))
                    {
                        ReplaceNoteWithHtmlRuns(text, "{{Note}}", noteValue);
                    }
                    else if (noteValue.Contains("\n"))
                    {
                        ReplaceTextWithLineBreaks(text, "{{Note}}", noteValue);
                    }
                    // else: plain text without newlines, will be handled by standard replacement below
                }
            }
        }

        // Remove entire paragraph when SamplingAddress is empty (avoid blank line)
        if (placeholders.TryGetValue("{{SamplingAddress}}", out var samplingAddr) && string.IsNullOrEmpty(samplingAddr))
        {
            foreach (var para in element.Descendants<Paragraph>().ToList())
            {
                var paraText = string.Concat(para.Descendants<Text>().Select(t => t.Text ?? ""));
                if (paraText.Trim() == "{{SamplingAddress}}")
                {
                    para.Remove();
                }
            }
        }

        // Process each paragraph to handle dots across multiple text runs
        foreach (var para in element.Descendants<Paragraph>())
        {
            ReplacePlaceholdersInParagraphWithDotRemoval(para, placeholders);
        }

        // Fallback: Handle split placeholders across multiple Text elements
        // This handles cases where Word splits {{Placeholder}} into multiple Text nodes
        ReplaceSplitPlaceholdersInElement(element, placeholders);

        // Final fallback: Direct replacement for any remaining complete placeholders
        foreach (var placeholder in placeholders)
        {
            foreach (var text in element.Descendants<Text>())
            {
                if (!string.IsNullOrEmpty(text.Text) && text.Text.Contains(placeholder.Key))
                {
                    text.Text = text.Text.Replace(placeholder.Key, placeholder.Value);
                }
            }
        }
    }

    /// <summary>
    /// Handle placeholders that are split across multiple Text elements.
    /// For example: Text1="{{NumberOf", Text2="Sample}}"
    /// This reconstructs the full text, replaces placeholders, and updates the elements.
    /// </summary>
    private void ReplaceSplitPlaceholdersInElement(OpenXmlElement element, Dictionary<string, string> placeholders)
    {
        foreach (var para in element.Descendants<Paragraph>())
        {
            var runs = para.Elements<Run>().ToList();
            if (runs.Count <= 1) continue;

            // Build full text from all runs
            var fullText = string.Concat(runs.SelectMany(r => r.Elements<Text>()).Select(t => t.Text ?? ""));

            // Check if any placeholder exists in the combined text but not in individual runs
            // This means the placeholder is split across runs
            foreach (var placeholder in placeholders)
            {
                if (!fullText.Contains(placeholder.Key)) continue;

                // Check if placeholder is already in a single run (already handled)
                var alreadyInSingleRun = runs.Any(r =>
                    r.Elements<Text>().Any(t => t.Text?.Contains(placeholder.Key) == true));
                if (alreadyInSingleRun) continue;

                // Placeholder is split - find which runs contain parts of it
                // and merge only those runs' text, keeping the formatting of the run containing "{{"
                var placeholderStartIndex = fullText.IndexOf(placeholder.Key);
                var placeholderEndIndex = placeholderStartIndex + placeholder.Key.Length;

                // Find the run containing the start of placeholder "{{"
                int charIndex = 0;
                Run? targetRun = null;
                foreach (var run in runs)
                {
                    var runText = string.Concat(run.Elements<Text>().Select(t => t.Text ?? ""));
                    if (charIndex <= placeholderStartIndex && charIndex + runText.Length > placeholderStartIndex)
                    {
                        targetRun = run;
                        break;
                    }
                    charIndex += runText.Length;
                }

                if (targetRun == null) continue;

                // Replace in the full text
                var replacedFullText = fullText.Replace(placeholder.Key, placeholder.Value);

                // Put replaced text in target run, clear others
                var targetText = targetRun.GetFirstChild<Text>();
                if (targetText == null)
                {
                    targetText = new Text();
                    targetRun.AppendChild(targetText);
                }
                targetText.Text = replacedFullText;
                targetText.Space = SpaceProcessingModeValues.Preserve;

                // Clear text from other runs
                foreach (var run in runs)
                {
                    if (run == targetRun) continue;
                    foreach (var t in run.Elements<Text>().ToList())
                    {
                        t.Text = "";
                    }
                }

                // Update fullText for next placeholder
                fullText = replacedFullText;
            }
        }
    }

    // Characters to trim: all dot-like and leader characters
    // . (period), … (ellipsis), · (middle dot), • (bullet), ․ (one dot leader), ‥ (two dot leader)
    // ． (fullwidth period), ⋯ (midline ellipsis), ᠁ (mongolian ellipsis), ︙ (vertical ellipsis)
    // _ (underscore), - (dash), ─ (box drawing), ━ (heavy box drawing)
    private static readonly char[] DotCharacters = new[] { '.', '…', '．', '·', '•', '․', '‥', '⋯', '᠁', '︙', '_', '-', '─', '━' };

    private static Dictionary<string, string> TrimPlaceholderValues(Dictionary<string, string> placeholders)
    {
        return placeholders.ToDictionary(p => p.Key, p => p.Value?.Trim() ?? "");
    }

    /// <summary>
    /// Normalize placeholders by removing extra spaces inside {{ }}
    /// e.g., "{{ CustomerTestMethod }}" -> "{{CustomerTestMethod}}"
    /// </summary>
    private static string NormalizePlaceholders(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{{")) return text;

        // Use regex to find all {{ ... }} patterns and normalize them
        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\{\{\s*(\w+)\s*\}\}",
            "{{$1}}");
    }

    // Placeholders that should have dot leader tabs removed when replaced
    // BM02 (Form trả KQ tổng hợp) and BM01 (Phiếu yêu cầu) placeholders
    private static readonly HashSet<string> PlaceholdersWithDotLeaderRemoval = new HashSet<string>
    {
        // BM02 - Form trả KQ tổng hợp
        "{{CustomerName}}",
        "{{FullAddress}}",
        "{{DateOfReceiving}}",
        "{{TimeOfTesting}}",
        "{{SampleDescription}}",
        "{{Quantity}}",
        "{{DateOfIssue}}",
        // BM01 - Phiếu yêu cầu
        "{{Customer}}",
        "{{Adress}}",
        "{{Ward}}",
        "{{Province}}",
        "{{TaxNo}}",
        "{{ContactPerson}}",
        "{{PhoneNumber}}"
    };

    /// <summary>
    /// Replace placeholders in a paragraph, removing surrounding dots when placeholder has value
    /// This handles cases where dots and placeholders are in separate text runs
    /// NEW APPROACH: Combine all text in paragraph, replace, then redistribute
    /// ALSO: Remove Tab elements with dot leaders for specific placeholders only
    /// </summary>
    private void ReplacePlaceholdersInParagraphWithDotRemoval(Paragraph para, Dictionary<string, string> placeholders)
    {
        var runs = para.Elements<Run>().ToList();
        if (!runs.Any()) return;

        // Build full text from all runs
        var fullText = string.Concat(runs.SelectMany(r => r.Elements<Text>()).Select(t => t.Text ?? ""));
        if (string.IsNullOrEmpty(fullText)) return;

        // Normalize placeholders
        fullText = NormalizePlaceholders(fullText);

        // Check if any placeholder exists in the full text
        var hasPlaceholder = placeholders.Any(p => fullText.Contains(p.Key));
        if (!hasPlaceholder) return;

        // Track if we need to remove dot leader tabs (only for specific placeholders)
        var shouldRemoveDotLeaderTabs = false;

        // Process each placeholder with dot handling
        foreach (var placeholder in placeholders)
        {
            if (!fullText.Contains(placeholder.Key)) continue;
            if (string.IsNullOrEmpty(placeholder.Value)) continue;

            // Check if this placeholder should have dot leader tabs removed
            if (PlaceholdersWithDotLeaderRemoval.Contains(placeholder.Key))
            {
                shouldRemoveDotLeaderTabs = true;
            }

            // Pattern: placeholder followed by ANY repeated non-alphanumeric chars (dots, underscores, dashes, etc.)
            // This catches cases where Word uses special characters for leader lines
            var patternTrailingAny = System.Text.RegularExpressions.Regex.Escape(placeholder.Key) + @"[\.…·•․‥．⋯᠁︙\u2024\u2025\u22EF\u22EE_\-─━]+";
            fullText = System.Text.RegularExpressions.Regex.Replace(
                fullText,
                patternTrailingAny,
                match =>
                {
                    var originalLength = match.Value.Length;
                    var paddingNeeded = Math.Max(0, originalLength - placeholder.Value.Length);
                    return placeholder.Value + new string(' ', paddingNeeded);
                });

            // Pattern: dots/leaders before placeholder + placeholder + optional dots after
            var patternWithDots = @"[\.…·•․‥．⋯᠁︙\u2024\u2025\u22EF\u22EE_\-─━]+\s*" + System.Text.RegularExpressions.Regex.Escape(placeholder.Key) + @"(?:[\.…·•․‥．⋯᠁︙\u2024\u2025\u22EF\u22EE_\-─━]+)?";
            fullText = System.Text.RegularExpressions.Regex.Replace(
                fullText,
                patternWithDots,
                match =>
                {
                    var originalLength = match.Value.Length;
                    var paddingNeeded = Math.Max(0, originalLength - placeholder.Value.Length);
                    return placeholder.Value + new string(' ', paddingNeeded);
                });

            // Standard replacement for placeholders without dots
            fullText = fullText.Replace(placeholder.Key, placeholder.Value);
        }

        // Put the processed text in the first run, clear others
        var firstRun = runs.First();
        var firstText = firstRun.GetFirstChild<Text>();
        if (firstText == null)
        {
            firstText = new Text();
            firstRun.AppendChild(firstText);
        }
        firstText.Text = fullText;
        firstText.Space = SpaceProcessingModeValues.Preserve;

        // Clear text from other runs
        for (int i = 1; i < runs.Count; i++)
        {
            foreach (var t in runs[i].Elements<Text>().ToList())
            {
                t.Text = "";
            }

            // Only remove Tab elements for specific placeholders
            if (shouldRemoveDotLeaderTabs)
            {
                foreach (var tab in runs[i].Elements<TabChar>().ToList())
                {
                    tab.Remove();
                }
            }
        }

        // Only remove tabs and dot leader tab stops for specific placeholders
        if (shouldRemoveDotLeaderTabs)
        {
            // Also remove tabs from the first run if they come after the text
            foreach (var tab in firstRun.Elements<TabChar>().ToList())
            {
                tab.Remove();
            }

            // Remove dot leader tab stops from paragraph properties
            RemoveDotLeaderTabStops(para);
        }
    }

    /// <summary>
    /// Remove tab stops with dot leaders from paragraph properties
    /// These cause the "......." lines in Word documents
    /// </summary>
    private void RemoveDotLeaderTabStops(Paragraph para)
    {
        var pPr = para.GetFirstChild<ParagraphProperties>();
        if (pPr == null) return;

        var tabs = pPr.GetFirstChild<Tabs>();
        if (tabs == null) return;

        // Find and remove tab stops with dot leaders
        var dotLeaderTabs = tabs.Elements<TabStop>()
            .Where(t => t.Leader != null && t.Leader.Value == TabStopLeaderCharValues.Dot)
            .ToList();

        foreach (var tab in dotLeaderTabs)
        {
            tab.Remove();
        }

        // If no tabs left, remove the Tabs element
        if (!tabs.Elements<TabStop>().Any())
        {
            tabs.Remove();
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
    /// Count dots at end of string (trailing dots)
    /// </summary>
    private static int CountTrailingDots(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int count = 0;
        for (int i = text.Length - 1; i >= 0; i--)
        {
            if (DotCharacters.Contains(text[i]))
                count++;
            else
                break;
        }
        return count;
    }

    /// <summary>
    /// Count dots at start of string (leading dots)
    /// </summary>
    private static int CountLeadingDots(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int count = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (DotCharacters.Contains(text[i]))
                count++;
            else
                break;
        }
        return count;
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
        data.SampleName = data.SampleName?.Trim() ?? "";
        data.Quantity = data.Quantity?.Trim() ?? "";
        data.SampleInfo = data.SampleInfo?.Trim() ?? "";
        data.Indicators = data.Indicators?.Trim() ?? "";

        var newRow = (TableRow)template.CloneNode(true);

        // Remove fixed row height to allow row to auto-expand with content
        RemoveRowHeightConstraint(newRow);

        // Allow page break inside row for long content
        AllowPageBreakInRow(newRow);

        // Enable text wrapping in all cells to prevent content overflow
        EnableTextWrappingInRow(newRow);

        // Note: Don't apply dashed borders for BM05 (phieu yeu cau) - keep original solid borders from template

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
            indicator.IndicatorName = indicator.IndicatorName?.Trim() ?? "";
            indicator.IndicatorFormattedName = indicator.IndicatorFormattedName?.Trim();
            indicator.UnitPrice = indicator.UnitPrice?.Trim() ?? "";

            var newRow = (TableRow)templateClone.CloneNode(true);

            // Note: Don't apply dashed borders for sub-table - keep original solid borders from template

            // Merge split placeholders
            foreach (var para in newRow.Descendants<Paragraph>())
            {
                MergeSplitPlaceholders(para);
            }

            // Replace placeholders in the new row
            foreach (var text in newRow.Descendants<Text>().ToList())
            {
                if (string.IsNullOrEmpty(text.Text)) continue;

                // Check if this text contains the SubIndicator placeholder
                if (text.Text.Contains("{{SubIndicator}}"))
                {
                    // Use formatted name if available, otherwise plain name
                    var indicatorDisplayName = !string.IsNullOrEmpty(indicator.IndicatorFormattedName)
                        ? indicator.IndicatorFormattedName
                        : indicator.IndicatorName;

                    // If name contains HTML or newlines, we need special handling
                    var hasHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(indicatorDisplayName);
                    var hasNewlines = indicatorDisplayName?.Contains('\n') ?? false;

                    if (hasHtml || hasNewlines)
                    {
                        // Replace other placeholders first
                        text.Text = text.Text
                            .Replace("{{SubTable}}", "")
                            .Replace("{{SubNo}}", indicator.No.ToString())
                            .Replace("{{SubPrice}}", indicator.UnitPrice);

                        // Now handle SubIndicator with HTML/newlines
                        ReplaceTextWithFormattedRuns(text, "{{SubIndicator}}", indicatorDisplayName);
                    }
                    else
                    {
                        // No HTML or newlines - simple replacement
                        text.Text = text.Text
                            .Replace("{{SubTable}}", "")
                            .Replace("{{SubNo}}", indicator.No.ToString())
                            .Replace("{{SubIndicator}}", indicatorDisplayName ?? "")
                            .Replace("{{SubPrice}}", indicator.UnitPrice);
                    }
                }
                else
                {
                    text.Text = text.Text
                        .Replace("{{SubTable}}", "")
                        .Replace("{{SubNo}}", indicator.No.ToString())
                        .Replace("{{SubPrice}}", indicator.UnitPrice);
                }
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

    #region Mixed Result with SubTable (BM02)

    /// <summary>
    /// Process document for mixed result with SubTable structure (BM02)
    /// Template structure: Each row = 1 sample, with SubTable marker for indicators
    /// SubTable is NOT a nested table, just a marker in the same row
    /// </summary>
    private void ProcessMixedResultDocument(string filePath, Dictionary<string, string> placeholders, List<MixedResultSampleRow> sampleData)
    {
        using var document = WordprocessingDocument.Open(filePath, true);
        var mainPart = document.MainDocumentPart;
        var body = mainPart?.Document?.Body;
        if (body == null || mainPart == null) return;

        ConvertFloatingTablesToInline(document);

        // Process table with SubTable marker (not nested table)
        ProcessMixedResultTableWithSubTable(body, sampleData);

        // Handle bold placeholders separately before standard replacement
        if (placeholders.TryGetValue("{{CustomerName}}", out var customerNameValue))
        {
            ReplacePlaceholderWithBold(mainPart, "{{CustomerName}}", customerNameValue?.Trim() ?? "");
            placeholders.Remove("{{CustomerName}}");
        }

        // Replace placeholders in entire document (including headers, footers, textboxes)
        ReplacePlaceholdersInDocument(mainPart, placeholders);

        mainPart.Document?.Save();
    }

    /// <summary>
    /// Process mixed result table for BM02 template
    /// Template has {{TestTable}} for sample info and {{SubTable}} for indicators in SAME row
    /// Each sample creates multiple rows - one for each indicator
    /// </summary>
    private void ProcessMixedResultTableWithSubTable(Body body, List<MixedResultSampleRow> sampleData)
    {
        if (sampleData == null || sampleData.Count == 0) return;

        // 1. Find table containing {{TestTable}} or {{SubTable}}
        var allTables = body.Descendants<Table>().ToList();
        Table? targetTable = null;
        TableRow? templateRow = null;

        foreach (var table in allTables.AsEnumerable().Reverse())
        {
            var directRows = table.Elements<TableRow>().ToList();
            foreach (var row in directRows)
            {
                var rowText = string.Concat(row.Descendants<Text>().Select(t => t.Text ?? ""));
                // Find row that contains both TestTable/No AND SubTable/Parameter markers
                if ((rowText.Contains("{{TestTable}}") || rowText.Contains("{{No}}")) &&
                    (rowText.Contains("{{SubTable}}") || rowText.Contains("{{Parameter}}")))
                {
                    targetTable = table;
                    templateRow = row;
                    _logger.LogInformation("Found mixed result table with TestTable and SubTable markers in same row");
                    break;
                }
            }
            if (targetTable != null) break;
        }

        if (targetTable == null || templateRow == null)
        {
            _logger.LogWarning("No mixed result table with TestTable/SubTable found in document");
            return;
        }

        // 2. Remove parent height constraints
        RemoveParentRowHeightConstraints(targetTable);

        // 3. Clone template and remove original
        var templateClone = (TableRow)templateRow.CloneNode(true);
        var allRows = targetTable.Elements<TableRow>().ToList();
        var templateRowIndex = allRows.IndexOf(templateRow);
        templateRow.Remove();

        // 4. Determine insert position
        allRows = targetTable.Elements<TableRow>().ToList();
        TableRow? insertAfterRow = templateRowIndex > 0 && allRows.Count > 0
            ? allRows[Math.Min(templateRowIndex - 1, allRows.Count - 1)]
            : null;

        // 5. Create rows for each sample × indicator combination
        // BM02 structure: Each indicator creates a new row
        // Sample columns (Stt, Tên mẫu, Mã số mẫu) are vertically merged (row span)
        // NOTE: In OpenXML vertical merge, content MUST be in the first row (Restart cell)
        // LibreOffice/Gotenberg will only display content from the Restart cell
        foreach (var sampleRow in sampleData)
        {
            var totalIndicators = sampleRow.Indicators.Count;

            for (int i = 0; i < totalIndicators; i++)
            {
                var indicator = sampleRow.Indicators[i];
                bool isFirstIndicator = (i == 0);

                var newRow = CloneAndFillMixedResultRowFlat(
                    templateClone,
                    sampleRow,
                    indicator,
                    isFirstIndicator,
                    totalIndicators);

                if (insertAfterRow != null)
                {
                    insertAfterRow.InsertAfterSelf(newRow);
                    insertAfterRow = newRow;
                }
                else
                {
                    var firstRow = targetTable.Elements<TableRow>().FirstOrDefault();
                    if (firstRow != null) firstRow.InsertBeforeSelf(newRow);
                    else targetTable.AppendChild(newRow);
                    insertAfterRow = newRow;
                }
            }
        }

        _logger.LogInformation("Processed {SampleCount} samples with total indicator rows for mixed result table", sampleData.Count);
    }

    /// <summary>
    /// Clone template row and fill with sample + indicator data
    /// For BM02 flat structure (no nested table)
    /// Applies VerticalMerge for sample columns (No, NameOfSample, CodeOfNumber)
    /// </summary>
    /// <param name="template">Template row to clone</param>
    /// <param name="sampleData">Sample data</param>
    /// <param name="indicatorData">Indicator data</param>
    /// <param name="isFirstIndicator">True if this is the first indicator of the sample (Restart merge)</param>
    /// <param name="totalIndicators">Total number of indicators for this sample (for rowspan)</param>
    private TableRow CloneAndFillMixedResultRowFlat(
        TableRow template,
        MixedResultSampleRow sampleData,
        MixedResultIndicatorRow indicatorData,
        bool isFirstIndicator,
        int totalIndicators)
    {
        sampleData.SampleName = sampleData.SampleName?.Trim() ?? "";
        sampleData.SampleCode = sampleData.SampleCode?.Trim() ?? "";
        sampleData.Quantity = sampleData.Quantity?.Trim() ?? "";
        sampleData.SampleDescription = sampleData.SampleDescription?.Trim() ?? "";
        indicatorData.Parameter = indicatorData.Parameter?.Trim() ?? "";
        indicatorData.ParameterFormattedName = indicatorData.ParameterFormattedName?.Trim();
        indicatorData.Unit = indicatorData.Unit?.Trim() ?? "";
        indicatorData.TestMethod = indicatorData.TestMethod?.Trim() ?? "";
        indicatorData.TestMethodFormattedName = indicatorData.TestMethodFormattedName?.Trim();
        indicatorData.Result = indicatorData.Result?.Trim() ?? "";
        indicatorData.AllowedLimit = indicatorData.AllowedLimit?.Trim() ?? "";
        indicatorData.ReferenceLimit = indicatorData.ReferenceLimit?.Trim() ?? "";
        indicatorData.ComparisonOperator = indicatorData.ComparisonOperator?.Trim() ?? "";
        indicatorData.LowerOperator = indicatorData.LowerOperator?.Trim() ?? "";
        indicatorData.LowerLimit = indicatorData.LowerLimit?.Trim() ?? "";
        indicatorData.UpperOperator = indicatorData.UpperOperator?.Trim() ?? "";
        indicatorData.UpperLimit = indicatorData.UpperLimit?.Trim() ?? "";

        var newRow = (TableRow)template.CloneNode(true);

        // Remove row height constraints
        RemoveRowHeightConstraint(newRow);

        // Allow page break inside row for long content
        AllowPageBreakInRow(newRow);

        // Enable text wrapping in all cells to prevent content overflow
        EnableTextWrappingInRow(newRow);

        // Copy cell borders from template to preserve border styles (dashed, dotted, etc.)
        CopyCellBordersFromTemplate(newRow, template);

        // Merge split placeholders
        foreach (var para in newRow.Descendants<Paragraph>())
        {
            MergeSplitPlaceholders(para);
        }

        // Get all cells in the row
        var cells = newRow.Elements<TableCell>().ToList();

        // Define which columns should be merged (0-based index)
        // Columns: 0=Stt, 1=Tên mẫu, 2=Mã số mẫu, 3=Chỉ tiêu, 4=Đơn vị, 5=Phương pháp, 6=Kết quả
        var columnsToMerge = new List<int> { 0, 1, 2 }; // Stt, Tên mẫu, Mã số mẫu

        // Dynamically detect cells containing {{Quantity}} or {{SampleDescription}} for vertical merge
        for (int c = 0; c < cells.Count; c++)
        {
            var cellText = string.Concat(cells[c].Descendants<Text>().Select(t => t.Text ?? ""));
            if (cellText.Contains("{{Quantity}}") || cellText.Contains("{{SampleDescription}}"))
            {
                if (!columnsToMerge.Contains(c))
                    columnsToMerge.Add(c);
            }
        }

        // Apply vertical merge to sample-level columns
        // Column 0 (Stt): center, Column 1 (Tên mẫu): left, others: center
        if (totalIndicators > 1)
        {
            foreach (var colIndex in columnsToMerge)
            {
                if (colIndex < cells.Count)
                {
                    var cell = cells[colIndex];
                    // Column 1 (NameOfSample) = left, others = center
                    var horizontalAlignment = (colIndex == 1) ? JustificationValues.Left : JustificationValues.Center;
                    ApplyVerticalMerge(cell, isFirstIndicator, horizontalAlignment);

                    // Clear content for non-first rows (merged cells should be empty)
                    if (!isFirstIndicator)
                    {
                        ClearCellContent(cell);
                    }
                }
            }
        }

        // Use formatted names if available, otherwise plain names
        var parameterDisplayName = !string.IsNullOrEmpty(indicatorData.ParameterFormattedName)
            ? indicatorData.ParameterFormattedName
            : indicatorData.Parameter;
        var testMethodDisplayName = !string.IsNullOrEmpty(indicatorData.TestMethodFormattedName)
            ? indicatorData.TestMethodFormattedName
            : indicatorData.TestMethod;

        var hasParameterHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(parameterDisplayName);
        var hasTestMethodHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(testMethodDisplayName);
        var hasUnitHtml = HtmlToOpenXmlConverter.ContainsHtmlTags(indicatorData.Unit);
        var hasParameterNewlines = parameterDisplayName?.Contains('\n') ?? false;
        var hasTestMethodNewlines = testMethodDisplayName?.Contains('\n') ?? false;
        var hasResultNewlines = indicatorData.Result?.Contains('\n') ?? false;

        // Replace all placeholders
        foreach (var text in newRow.Descendants<Text>().ToList())
        {
            if (string.IsNullOrEmpty(text.Text)) continue;

            // Check if this text contains placeholders that need special handling
            var hasParameterPlaceholder = text.Text.Contains("{{Parameter}}");
            var hasTestMethodPlaceholder = text.Text.Contains("{{TestMethod}}");
            var hasResultPlaceholder = text.Text.Contains("{{Result}}");

            // Handle Result with newlines specially
            if (hasResultPlaceholder && hasResultNewlines)
            {
                // Replace other placeholders first
                text.Text = text.Text
                    .Replace("{{TestTable}}", "")
                    .Replace("{{SubTable}}", "")
                    .Replace("{{No}}", isFirstIndicator ? sampleData.No.ToString() : "")
                    .Replace("{{NameOfSample}}", isFirstIndicator ? sampleData.SampleName : "")
                    .Replace("{{CodeOfNumber}}", isFirstIndicator ? sampleData.SampleCode : "")
                    .Replace("{{Quantity}}", isFirstIndicator ? sampleData.Quantity : "")
                    .Replace("{{SampleDescription}}", isFirstIndicator ? sampleData.SampleDescription : "");

                // Handle Unit with HTML if applicable
                if (hasUnitHtml && text.Text.Contains("{{Unit}}"))
                {
                    ReplaceTextWithFormattedRuns(text, "{{Unit}}", indicatorData.Unit ?? "");
                }
                else
                {
                    text.Text = text.Text.Replace("{{Unit}}", indicatorData.Unit ?? "");
                }

                // Handle Parameter
                if (hasParameterPlaceholder && (hasParameterHtml || hasParameterNewlines))
                {
                    ReplaceTextWithFormattedRuns(text, "{{Parameter}}", parameterDisplayName);
                }
                else if (hasParameterPlaceholder)
                {
                    text.Text = text.Text.Replace("{{Parameter}}", parameterDisplayName ?? "");
                }

                // Handle TestMethod
                if (hasTestMethodPlaceholder && (hasTestMethodHtml || hasTestMethodNewlines))
                {
                    ReplaceTextWithFormattedRuns(text, "{{TestMethod}}", testMethodDisplayName);
                }
                else if (hasTestMethodPlaceholder)
                {
                    text.Text = text.Text.Replace("{{TestMethod}}", testMethodDisplayName ?? "");
                }

                // Handle Result with line breaks
                if (text.Text.Contains("{{Result}}"))
                {
                    ReplaceTextWithLineBreaks(text, "{{Result}}", indicatorData.Result);
                }
            }
            else if ((hasParameterPlaceholder && (hasParameterHtml || hasParameterNewlines)) ||
                     (hasTestMethodPlaceholder && (hasTestMethodHtml || hasTestMethodNewlines)) ||
                     hasUnitHtml)
            {
                // Replace non-special placeholders first
                text.Text = text.Text
                    .Replace("{{TestTable}}", "")
                    .Replace("{{SubTable}}", "")
                    .Replace("{{No}}", isFirstIndicator ? sampleData.No.ToString() : "")
                    .Replace("{{NameOfSample}}", isFirstIndicator ? sampleData.SampleName : "")
                    .Replace("{{CodeOfNumber}}", isFirstIndicator ? sampleData.SampleCode : "")
                    .Replace("{{Quantity}}", isFirstIndicator ? sampleData.Quantity : "")
                    .Replace("{{SampleDescription}}", isFirstIndicator ? sampleData.SampleDescription : "")
                    .Replace("{{Result}}", indicatorData.Result);

                // Handle Unit with HTML if applicable
                if (hasUnitHtml && text.Text.Contains("{{Unit}}"))
                {
                    ReplaceTextWithFormattedRuns(text, "{{Unit}}", indicatorData.Unit ?? "");
                }
                else
                {
                    text.Text = text.Text.Replace("{{Unit}}", indicatorData.Unit ?? "");
                }

                // Handle Parameter with HTML/newlines if applicable
                if (hasParameterPlaceholder && (hasParameterHtml || hasParameterNewlines))
                {
                    ReplaceTextWithFormattedRuns(text, "{{Parameter}}", parameterDisplayName);
                }
                else if (hasParameterPlaceholder)
                {
                    text.Text = text.Text.Replace("{{Parameter}}", parameterDisplayName ?? "");
                }

                // Handle TestMethod with HTML/newlines if applicable
                if (hasTestMethodPlaceholder && (hasTestMethodHtml || hasTestMethodNewlines))
                {
                    ReplaceTextWithFormattedRuns(text, "{{TestMethod}}", testMethodDisplayName);
                }
                else if (hasTestMethodPlaceholder)
                {
                    text.Text = text.Text.Replace("{{TestMethod}}", testMethodDisplayName ?? "");
                }
            }
            else
            {
                // No HTML and no newlines - simple replacement
                text.Text = text.Text
                    .Replace("{{TestTable}}", "")
                    .Replace("{{SubTable}}", "")
                    .Replace("{{No}}", isFirstIndicator ? sampleData.No.ToString() : "")
                    .Replace("{{NameOfSample}}", isFirstIndicator ? sampleData.SampleName : "")
                    .Replace("{{CodeOfNumber}}", isFirstIndicator ? sampleData.SampleCode : "")
                    .Replace("{{Quantity}}", isFirstIndicator ? sampleData.Quantity : "")
                    .Replace("{{SampleDescription}}", isFirstIndicator ? sampleData.SampleDescription : "")
                    .Replace("{{Parameter}}", parameterDisplayName ?? "")
                    .Replace("{{Unit}}", indicatorData.Unit)
                    .Replace("{{TestMethod}}", testMethodDisplayName ?? "")
                    .Replace("{{Result}}", indicatorData.Result);
            }
        }

        return newRow;
    }

    /// <summary>
    /// Apply vertical merge to a table cell with center alignment
    /// </summary>
    /// <param name="cell">The cell to apply merge to</param>
    /// <param name="isFirstRow">True for Restart (first row of merge), False for Continue (subsequent rows)</param>
    private void ApplyVerticalMerge(TableCell cell, bool isFirstRow)
    {
        ApplyVerticalMerge(cell, isFirstRow, JustificationValues.Center);
    }

    /// <summary>
    /// Apply vertical merge to a table cell with vertical center alignment
    /// NOTE: Vertical centering in merged cells may not work well with LibreOffice/Gotenberg conversion.
    /// This is a known limitation. The code sets the proper OpenXML properties, but the PDF converter
    /// may not render them correctly.
    /// </summary>
    /// <param name="cell">The cell to apply merge to</param>
    /// <param name="isFirstRow">True for Restart (first row of merge), False for Continue (subsequent rows)</param>
    /// <param name="horizontalAlignment">Horizontal text alignment</param>
    private void ApplyVerticalMerge(TableCell cell, bool isFirstRow, JustificationValues horizontalAlignment)
    {
        // Get or create TableCellProperties - must be first child of TableCell
        var tcPr = cell.GetFirstChild<TableCellProperties>();
        if (tcPr == null)
        {
            tcPr = new TableCellProperties();
            cell.InsertAt(tcPr, 0);
        }

        // Remove existing properties we're going to set
        tcPr.GetFirstChild<VerticalMerge>()?.Remove();
        tcPr.GetFirstChild<TableCellVerticalAlignment>()?.Remove();

        // Add VerticalMerge - Restart for first row, Continue for subsequent
        var vMerge = new VerticalMerge
        {
            Val = isFirstRow ? MergedCellValues.Restart : MergedCellValues.Continue
        };
        tcPr.AppendChild(vMerge);

        // Add vertical alignment to center
        // Note: This may not render correctly in LibreOffice-based PDF conversion
        var vAlign = new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center };
        tcPr.AppendChild(vAlign);

        // Process paragraph properties
        foreach (var para in cell.Elements<Paragraph>())
        {
            var pPr = para.GetFirstChild<ParagraphProperties>();
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                para.InsertAt(pPr, 0);
            }

            // Set horizontal alignment only for first row (where content is)
            if (isFirstRow)
            {
                pPr.GetFirstChild<Justification>()?.Remove();
                pPr.AppendChild(new Justification { Val = horizontalAlignment });
            }

            // Remove paragraph spacing
            var existingSpacing = pPr.GetFirstChild<SpacingBetweenLines>();
            existingSpacing?.Remove();
            pPr.AppendChild(new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });
        }
    }

    /// <summary>
    /// Clear content of a merged cell (continuation cells should be empty)
    /// </summary>
    private void ClearCellContent(TableCell cell)
    {
        // Keep the cell structure but clear text content
        foreach (var text in cell.Descendants<Text>().ToList())
        {
            text.Text = "";
        }
    }

    /// <summary>
    /// Replace a placeholder in the document and apply bold formatting to the replacement text.
    /// Processes body, headers, and footers (same scope as ReplacePlaceholdersInDocument).
    /// </summary>
    private void ReplacePlaceholderWithBold(MainDocumentPart mainPart, string placeholder, string value)
    {
        var elements = new List<OpenXmlElement>();
        if (mainPart.Document?.Body != null)
            elements.Add(mainPart.Document.Body);
        foreach (var headerPart in mainPart.HeaderParts)
        {
            if (headerPart.Header != null)
                elements.Add(headerPart.Header);
        }
        foreach (var footerPart in mainPart.FooterParts)
        {
            if (footerPart.Footer != null)
                elements.Add(footerPart.Footer);
        }

        foreach (var element in elements)
        {
            // Merge split placeholders first (same as standard flow)
            foreach (var para in element.Descendants<Paragraph>())
            {
                MergeSplitPlaceholders(para);
            }

            foreach (var text in element.Descendants<Text>().ToList())
            {
                if (string.IsNullOrEmpty(text.Text) || !text.Text.Contains(placeholder)) continue;

                text.Text = text.Text.Replace(placeholder, value);

                // Apply bold to the run containing this text
                var run = text.Parent as Run;
                if (run != null)
                {
                    var rPr = run.GetFirstChild<RunProperties>();
                    if (rPr == null)
                    {
                        rPr = new RunProperties();
                        run.InsertAt(rPr, 0);
                    }
                    rPr.Bold = new Bold();
                    rPr.BoldComplexScript = new BoldComplexScript();
                }
            }
        }
    }

    /// <summary>
    /// Re-apply vertical center alignment for cells that already have it set.
    /// This fixes an issue where watermark/background images in document header
    /// cause LibreOffice/Collabora to render text at the top of cells instead of center.
    /// By removing and re-adding the alignment, we force Collabora to respect it.
    /// Only affects cells that were already set to center - preserves other alignments.
    /// </summary>
    private void EnsureTableCellVerticalAlignment(Body body)
    {
        foreach (var table in body.Descendants<Table>())
        {
            foreach (var row in table.Elements<TableRow>())
            {
                foreach (var cell in row.Elements<TableCell>())
                {
                    var tcPr = cell.GetFirstChild<TableCellProperties>();
                    if (tcPr == null) continue;

                    // Check if vertical alignment is set to center
                    var existingVAlign = tcPr.GetFirstChild<TableCellVerticalAlignment>();
                    if (existingVAlign?.Val != null && existingVAlign.Val.Value == TableVerticalAlignmentValues.Center)
                    {
                        // Remove and re-add to force Collabora to respect it
                        existingVAlign.Remove();
                        tcPr.AppendChild(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
                    }
                }
            }
        }
    }

    #endregion

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
        /// <summary>
        /// Tên có định dạng HTML (nếu có)
        /// </summary>
        public string? IndicatorFormattedName { get; set; }
        public string UnitPrice { get; set; } = "";
    }

    /// <summary>
    /// Sample row for mixed result form (BM02) with nested indicator sub-table
    /// TestTable = samples, SubTable = indicators per sample
    /// </summary>
    private class MixedResultSampleRow
    {
        public int No { get; set; }
        public string SampleName { get; set; } = "";
        public string SampleCode { get; set; } = "";
        public string Quantity { get; set; } = "";
        public string SampleDescription { get; set; } = "";
        public List<MixedResultIndicatorRow> Indicators { get; set; } = new();
    }

    /// <summary>
    /// Indicator row for nested sub-table in mixed result form (BM02)
    /// </summary>
    private class MixedResultIndicatorRow
    {
        public int No { get; set; }
        public string Parameter { get; set; } = "";
        /// <summary>
        /// Tên chỉ tiêu có định dạng HTML (nếu có)
        /// </summary>
        public string? ParameterFormattedName { get; set; }
        public string Unit { get; set; } = "";
        public string TestMethod { get; set; } = "";
        /// <summary>
        /// Tên phương pháp có định dạng HTML (nếu có)
        /// </summary>
        public string? TestMethodFormattedName { get; set; }
        public string Result { get; set; } = "";
        public string ReferenceLimit { get; set; } = ""; // For BM05-QLNN CompareWithStandard (DEPRECATED)
        public Guid? StandardId { get; set; } // For BM05-QLNN footnote mapping
        public Guid? IndicatorId { get; set; } // For StandardIndicator lookup
        public string ComparisonOperator { get; set; } = ""; // Từ StandardIndicator: <=, >=, <, >, =
        public string AllowedLimit { get; set; } = ""; // Từ StandardIndicator: giới hạn cho phép
        public string LowerOperator { get; set; } = ""; // Từ StandardIndicator: >, >= (cận dưới)
        public string LowerLimit { get; set; } = ""; // Từ StandardIndicator: giá trị cận dưới
        public string UpperOperator { get; set; } = ""; // Từ StandardIndicator: <, <= (cận trên)
        public string UpperLimit { get; set; } = ""; // Từ StandardIndicator: giá trị cận trên
    }

    /// <summary>
    /// Build placeholder dictionary for test request form (BM05)
    /// </summary>
    private Dictionary<string, string> BuildTestRequestFormPlaceholders(TestRequest testRequest, List<TestRequestSample> samples)
    {
        // Address = chỉ lấy địa chỉ người dùng nhập, không nối tỉnh/huyện/xã
        var address = testRequest.Address ?? "";

        // Build address parts - lowercase prefix hành chính (Tỉnh → tỉnh, Quận → quận, Phường/Xã → phường/xã)
        var wardText = LowercaseAdministrativePrefix(testRequest.Ward, "Phường", "Xã");
        var districtText = LowercaseAdministrativePrefix(testRequest.District, "Quận");
        var provinceText = LowercaseAdministrativePrefix(testRequest.Province, "Tỉnh", "Thành phố");

        // Calculate certified copies (KQTN yêu cầu - giấy chứng nhận)
        var resultCopies = testRequest.ResultCopyCount ?? "";

        // Get customer test method detail - chỉ hiển thị khi MethodCustomer = true
        var customerTestMethod = testRequest.MethodCustomer ? (testRequest.MethodCustomerDetail ?? "") : "";

        // Compare standard / conformity declaration
        var compareStandard = testRequest.ConformityDeclaration ?? "";

        // Other requirements
        var otherRequirements = testRequest.OtherRequirements ?? "";

        // Subcontract info
        var subContractInfo = testRequest.SubContract ?? "";

        // Format dates
        // DateReceiving = Ngày trả kết quả (ResultDeliveryDate), không phải RequestDate
        var dateReceiving = testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "";
        var contractDate = testRequest.ContractDate?.ToString("dd/MM/yyyy") ?? "";
        var formPrintedDate = testRequest.FormPrintedDate?.ToString("dd/MM/yyyy") ?? "";
        var currentDateText = DateTime.Now.ToString("dd/MM/yyyy");

        // Total amount formatting - Mẫu nhiệm vụ hiển thị 0
        var actualTotalCost = testRequest.SampleTypeTask ? 0 : testRequest.TotalCost;
        var totalAmount = actualTotalCost.ToString("N0");
        var totalByText = testRequest.SampleTypeTask
            ? "Không đồng"
            : (testRequest.TotalCostInWords ?? ConvertNumberToVietnameseWords(testRequest.TotalCost));

        // Receipt number
        var receiptNumber = testRequest.ReceiptNumber ?? "";

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
            { "{{DateOfReceiving}}", contractDate },
            { "{{TotalAmount}}", totalAmount },
            { "{{TotalByText}}", totalByText },
            { "{{ReceiptNumberTTDV}}", receiptNumber },
            { "{{CurrentDate}}", currentDateText },
            { "{{FormPrintedDate}}", formPrintedDate },

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
            { "{{CB_Credit}}", TemplateConstants.UncheckedBox },

            // Người gửi mẫu và người nhận mẫu
            { "{{SampleSender}}", testRequest.ContactPerson ?? "" },
            { "{{SampleReceiver}}", string.IsNullOrWhiteSpace($"{_currentUser.SurName} {_currentUser.Name}".Trim()) ? _currentUser.UserName ?? "" : $"{_currentUser.SurName} {_currentUser.Name}".Trim() }
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
    /// Lowercase prefix hành chính: "Tỉnh Đắk Lắk" → "tỉnh Đắk Lắk", "Quận 1" → "quận 1", etc.
    /// </summary>
    private static string ComputeIndicatorHash(List<Guid> sortedIds)
    {
        var input = string.Join(",", sortedIds);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string LowercaseAdministrativePrefix(string? value, params string[] prefixes)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix + " ", StringComparison.Ordinal))
            {
                return char.ToLower(value[0]) + value[1..];
            }
        }
        return value;
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

        // Calculate TimeOfTesting: Use selected indicator's TestDate and SubmitDate
        var timeOfTesting = CalculateTimeOfTestingForSingleIndicator(labTaskIndicator);

        // Merge notes from QLKT (lab) and QLCL (quality)
        var mergedNote = await GetMergedNoteAsync(labTaskIndicator.TestRequestSampleIndicatorId, input.Note);

        // 3. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var printDate = input.PrintDate ?? DateTime.Now;
        var printDateText = printDate.ToString("dd/MM/yyyy");
        var contractDateText = testRequest.ContractDate?.ToString("dd/MM/yyyy") ?? "";
        var placeholders = new Dictionary<string, string>
        {
            // Common placeholders (BM01, BM03)
            { "{{SampleCode}}", sample.SampleCode ?? "" },
            { "{{CurrentDate}}", printDateText },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{SamplingAddress}}", sample.SamplingAddress ?? "" },
            { "{{NameOfSample}}", sample.SampleName ?? "" },
            { "{{DateOfReceiving}}", contractDateText },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", sample.StatusText ?? "" },
            { "{{Quantity}}", sample.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", mergedNote },

            // Additional placeholders for BM05-QLNN template
            { "{{NumberOfSample}}", sample.SampleCode ?? "" },
            { "{{SampleQuantity}}", sample.SampleQuantity ?? "" },
            { "{{DescriptionOfSample}}", sample.StatusText ?? "" },
            { "{{TimeToSaveSample}}", "" }, // TODO: Add field to Sample if needed
            { "{{DateOfSending}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{PlaceToSendSample}}", testRequest.CustomerName ?? "" },
            { "{{AttachFile}}", "" }, // TODO: Add attachment info if needed
            { "{{Result}}", ConvertToSuperscriptDisplay(labTaskIndicator.Result) },
            { "{{Conclude}}", "" } // BM05-QLNN - Kết luận (để trống, user tự điền)
        };

        // Add Vice President placeholder if provided
        var vicePresidentName = await GetVicePresidentFullNameAsync(input.VicePresidentId);
        placeholders.Add("{{VicePresident}}", vicePresidentName);

        // Add Officer In Charge placeholder (QLKT who approved the indicator)
        // Only shows name if exactly 1 approver, otherwise empty
        var officerInChargeName = await GetOfficerInChargeNameAsync(new[] { labTaskIndicator });
        placeholders.Add("{{OfficerInCharge}}", officerInChargeName);

        // 5. Generate document using selected template or default BM01
        var templatePath = GetTemplatePath(input.TemplateName, TemplateConstants.TemplateBM01);
        var templateFileName = Path.GetFileName(templatePath);
        var isBM05QLNN = IsBM05QLNNTemplate(templateFileName);

        // For BM05-QLNN: Get standard info and add {{Standard}} placeholder
        if (isBM05QLNN && qualityIndicator.StandardId.HasValue)
        {
            var standard = await _standardRepository.FirstOrDefaultAsync(x => x.Id == qualityIndicator.StandardId.Value);
            placeholders.Add("{{Standard}}", standard?.Name ?? "So với QCVN…/ TCVN…./ QĐ…");
        }
        else if (isBM05QLNN)
        {
            placeholders.Add("{{Standard}}", "So với QCVN…/ TCVN…./ QĐ…");
        }

        // Get StandardIndicator for proper limit formatting (with operators and range values)
        var compareWithStandard = qualityIndicator.ReferenceLimit ?? "";
        if (qualityIndicator.StandardId.HasValue)
        {
            var standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(
                x => x.StandardId == qualityIndicator.StandardId.Value && x.IndicatorId == sampleIndicator.IndicatorId);
            if (standardIndicator != null)
            {
                compareWithStandard = BuildCompareWithStandard(
                    standardIndicator.ComparisonOperator,
                    standardIndicator.AllowedLimit,
                    standardIndicator.LowerOperator,
                    standardIndicator.LowerLimit,
                    standardIndicator.UpperOperator,
                    standardIndicator.UpperLimit);
            }
        }

        // 4. Build table data - single indicator
        // Resolve unit with TestMethodIndicator fallback
        var resolvedUnit = labTaskIndicator.Unit;
        if (string.IsNullOrWhiteSpace(resolvedUnit) || !HtmlToOpenXmlConverter.ContainsHtmlTags(resolvedUnit))
        {
            if (labTaskIndicator.MethodId.HasValue)
            {
                var tmi = await _testMethodIndicatorRepository.FirstOrDefaultAsync(x =>
                    x.TestMethodId == labTaskIndicator.MethodId && x.IndicatorId == sampleIndicator.IndicatorId);
                if (!string.IsNullOrWhiteSpace(tmi?.FormattedUnit))
                {
                    resolvedUnit = tmi.FormattedUnit;
                }
            }
        }

        var tableData = new List<ResultFormTableRow>
        {
            new ResultFormTableRow
            {
                No = 1,
                Parameters = indicator.Name ?? "",
                ParametersFormattedName = indicator.FormattedName,
                Unit = string.IsNullOrWhiteSpace(resolvedUnit) ? "-" : resolvedUnit,
                TestMethod = !string.IsNullOrWhiteSpace(labTaskIndicator.SubContractMethodName)
                    ? labTaskIndicator.SubContractMethodName
                    : method?.Name ?? "",
                TestMethodFormattedName = !string.IsNullOrWhiteSpace(labTaskIndicator.SubContractMethodName)
                    ? null
                    : method?.FormattedName,
                Result = ConvertToSuperscriptDisplay(labTaskIndicator.Result),
                Standards = compareWithStandard, // {{Standards}} = properly formatted limit
                CompareWithStandard = compareWithStandard, // Single indicator = no footnote needed
                FootnoteNumber = "", // Single indicator = no footnote needed
                Allowlimit = compareWithStandard, // BM04 - Giới hạn cho phép
                Uncertainty = "",
                Proclaim = ""
            }
        };
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

        // Process document
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

        // Also load parent indicators for sub-indicators (to display parent name in grouped rows)
        var parentIndicatorIds = indicators
            .Where(i => i.ParentIndicatorId.HasValue)
            .Select(i => i.ParentIndicatorId!.Value)
            .Distinct()
            .Except(indicatorIds) // Exclude already loaded indicators
            .ToList();
        if (parentIndicatorIds.Any())
        {
            var parentIndicators = await _indicatorRepository.GetListAsync(x => parentIndicatorIds.Contains(x.Id));
            indicators = indicators.Concat(parentIndicators).ToList();
        }

        var indicatorDict = indicators.ToDictionary(i => i.Id);

        var methodIds = labTaskIndicators.Where(lti => lti.MethodId.HasValue).Select(lti => lti.MethodId!.Value).Distinct().ToList();
        var methods = methodIds.Any()
            ? await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id))
            : new List<TestMethod>();
        var methodDict = methods.ToDictionary(m => m.Id);

        var methodIndicatorsQm = methodIds.Any() && indicatorIds.Any()
            ? await _testMethodIndicatorRepository.GetListAsync(x =>
                methodIds.Contains(x.TestMethodId) && indicatorIds.Contains(x.IndicatorId))
            : new List<TestMethodIndicator>();
        var sampleIndicatorToIndicatorIdQm = sampleIndicators.ToDictionary(si => si.Id, si => si.IndicatorId);

        string ResolveUnitQm(LaboratoryTaskIndicator lti)
        {
            var unit = lti.Unit;
            if (!string.IsNullOrWhiteSpace(unit) && HtmlToOpenXmlConverter.ContainsHtmlTags(unit))
                return unit;
            if (lti.MethodId != null && sampleIndicatorToIndicatorIdQm.TryGetValue(lti.TestRequestSampleIndicatorId, out var indicatorId))
            {
                var tmi = methodIndicatorsQm.FirstOrDefault(x =>
                    x.TestMethodId == lti.MethodId && x.IndicatorId == indicatorId);
                if (!string.IsNullOrWhiteSpace(tmi?.FormattedUnit))
                    return tmi.FormattedUnit;
            }
            return string.IsNullOrWhiteSpace(unit) ? "-" : unit;
        }

        // Get StandardIndicator data for BM05-QLNN template (ComparisonOperator, AllowedLimit)
        // Query all StandardIndicators that match any combination of (StandardId, IndicatorId) used
        // Note: StandardId có thể ở QualityManagementIndicator hoặc LaboratoryTaskIndicator
        var standardIdsFromQuality = qualityIndicators
            .Where(qi => qi.StandardId.HasValue)
            .Select(qi => qi.StandardId!.Value);
        var standardIdsFromLabTask = labTaskIndicators
            .Where(lti => lti.StandardId.HasValue)
            .Select(lti => lti.StandardId!.Value);
        var standardIds = standardIdsFromQuality
            .Concat(standardIdsFromLabTask)
            .Distinct()
            .ToList();
        var standardIndicators = standardIds.Any()
            ? await _standardIndicatorRepository.GetListAsync(
                x => standardIds.Contains(x.StandardId) && indicatorIds.Contains(x.IndicatorId))
            : new List<StandardIndicator>();
        // Create lookup: (StandardId, IndicatorId) -> StandardIndicator
        // Note: Same indicator can have multiple StandardIndicators with different SpecimenTypeId
        // Use GroupBy to take the first one (without SpecimenType or first match)
        var standardIndicatorLookup = standardIndicators
            .GroupBy(si => (si.StandardId, si.IndicatorId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(si => si.SpecimenTypeId.HasValue ? 1 : 0).First());

        _logger.LogInformation("BM05-QLNN Debug: standardIds={StandardIds}, indicatorIds={IndicatorIds}, standardIndicators.Count={Count}",
            string.Join(",", standardIds), string.Join(",", indicatorIds), standardIndicators.Count);
        foreach (var si in standardIndicators)
        {
            _logger.LogInformation("StandardIndicator: StandardId={StandardId}, IndicatorId={IndicatorId}, ComparisonOperator={Op}, AllowedLimit={Limit}, LowerOperator={LowerOp}, LowerLimit={LowerLim}, UpperOperator={UpperOp}, UpperLimit={UpperLim}",
                si.StandardId, si.IndicatorId, si.ComparisonOperator, si.AllowedLimit, si.LowerOperator, si.LowerLimit, si.UpperOperator, si.UpperLimit);
        }

        // Calculate TimeOfTesting: Min(TestDate) - Max(SubmitDate) of selected indicators only
        var timeOfTesting = CalculateTimeOfTesting(labTaskIndicators);

        // Note: FE đã auto-append notes từ FreeTextNote và MasterDataNotes vào input.Note
        // nên không cần merge ở BE nữa, chỉ sử dụng input.Note trực tiếp

        // 4. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var printDate = input.PrintDate ?? DateTime.Now;
        var contractDateText = testRequest.ContractDate?.ToString("dd/MM/yyyy") ?? "";
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", firstSample?.SampleCode ?? "" },
            { "{{CurrentDate}}", printDate.ToString("dd/MM/yyyy") },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{SamplingAddress}}", firstSample?.SamplingAddress ?? "" },
            { "{{NameOfSample}}", firstSample?.SampleName ?? "" },
            { "{{DateOfReceiving}}", contractDateText },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", firstSample?.StatusText ?? "" },
            { "{{Quantity}}", firstSample?.SampleQuantity ?? "" },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", input.Note ?? "" },
            // BM02 specific placeholders
            { "{{ContractNumber}}", testRequest.ContractNumber ?? "" },
            { "{{Date}}", printDate.Day.ToString("D2") },
            { "{{Month}}", printDate.Month.ToString("D2") },
            { "{{Year}}", printDate.Year.ToString() },
            { "{{CustomerName}}", testRequest.CustomerName ?? "" },
            { "{{FullAddress}}", address },
            // BM05-QLNN specific placeholders
            { "{{NumberOfSample}}", firstSample?.SampleCode ?? "" },
            { "{{SampleQuantity}}", firstSample?.SampleQuantity ?? "" },
            { "{{DescriptionOfSample}}", firstSample?.StatusText ?? "" },
            { "{{TimeToSaveSample}}", "" },
            { "{{DateOfSending}}", testRequest.RequestDate.ToString("dd/MM/yyyy") },
            { "{{PlaceToSendSample}}", testRequest.CustomerName ?? "" },
            { "{{AttachFile}}", "" },
            { "{{Conclude}}", "" }
        };

        // Add Vice President placeholder if provided
        var vicePresidentName = await GetVicePresidentFullNameAsync(input.VicePresidentId);
        placeholders.Add("{{VicePresident}}", vicePresidentName);

        // Add Officer In Charge placeholder (QLKT who approved the indicators)
        // Only shows name if exactly 1 approver, otherwise empty
        var officerInChargeName = await GetOfficerInChargeNameAsync(labTaskIndicators);
        placeholders.Add("{{OfficerInCharge}}", officerInChargeName);

        // Check if template is BM05-QLNN or BM04 and handle Standard/Footnotes logic
        var templatePath = GetTemplatePath(input.TemplateName, TemplateConstants.TemplateBM02);
        var templateFileName = Path.GetFileName(templatePath);
        var isBM05QLNN = IsBM05QLNNTemplate(templateFileName);
        var isBM04 = IsBM04Template(templateFileName);
        BM05StandardInfo? bm05StandardInfo = null;
        BM05StandardInfo? bm04StandardInfo = null;

        if (isBM05QLNN)
        {
            // Get BM05 Standard info for placeholder {{Standard}} and footnotes
            // Truyền cả labTaskIndicators để lấy StandardId nếu QualityIndicator không có
            bm05StandardInfo = await GetBM05StandardInfoAsync(qualityIndicators, labTaskIndicators);
            placeholders.Add("{{Standard}}", bm05StandardInfo.StandardText);

            // Append footnotes to Note if multiple standards
            if (!bm05StandardInfo.IsSingleStandard && !string.IsNullOrEmpty(bm05StandardInfo.FootnotesText))
            {
                var currentNote = placeholders.GetValueOrDefault("{{Note}}", "");
                var updatedNote = string.IsNullOrWhiteSpace(currentNote)
                    ? bm05StandardInfo.FootnotesText
                    : $"{currentNote}\n\n{bm05StandardInfo.FootnotesText}";
                placeholders["{{Note}}"] = updatedNote;
            }
        }

        if (isBM04)
        {
            // Get BM04 Standard info for footnotes (luôn đánh footnote kể cả single standard)
            bm04StandardInfo = await GetBM04StandardInfoAsync(qualityIndicators, labTaskIndicators);

            // BM04: Luôn append footnotes to Note
            if (!string.IsNullOrEmpty(bm04StandardInfo.FootnotesText))
            {
                var currentNote = placeholders.GetValueOrDefault("{{Note}}", "");
                var updatedNote = string.IsNullOrWhiteSpace(currentNote)
                    ? bm04StandardInfo.FootnotesText
                    : $"{currentNote}\n\n{bm04StandardInfo.FootnotesText}";
                placeholders["{{Note}}"] = updatedNote;
            }
        }

        // 5. Build nested data structure - group indicators by sample for BM02 template
        var sampleData = new List<MixedResultSampleRow>();

        // Create a lookup from qualityIndicator to its related data
        var indicatorDataList = qualityIndicators
            .Select(qi => new
            {
                QualityIndicator = qi,
                LabTaskIndicator = labTaskIndicators.FirstOrDefault(lti => lti.Id == qi.LaboratoryTaskIndicatorId)
            })
            .Where(x => x.LabTaskIndicator != null)
            .Select(x => new
            {
                x.QualityIndicator,
                LabTaskIndicator = x.LabTaskIndicator!,
                SampleIndicator = sampleIndicators.FirstOrDefault(si => si.Id == x.LabTaskIndicator!.TestRequestSampleIndicatorId)
            })
            .Where(x => x.SampleIndicator != null)
            .ToList();

        // Group by sample
        var groupedBySample = indicatorDataList.GroupBy(x => x.SampleIndicator!.TestRequestSampleId);

        int sampleNo = 1;
        foreach (var group in groupedBySample)
        {
            var sample = sampleDict.GetValueOrDefault(group.Key);
            if (sample == null) continue;

            var sampleRow = new MixedResultSampleRow
            {
                No = sampleNo++,
                SampleName = sample.SampleName ?? "",
                SampleCode = sample.SampleCode ?? "",
                Quantity = sample.SampleQuantity ?? "",
                SampleDescription = sample.StatusText ?? "",
                Indicators = new List<MixedResultIndicatorRow>()
            };

            // Group indicators by parent-child relationship
            var itemsWithIndicator = group
                .Select(item => new
                {
                    Item = item,
                    Indicator = indicatorDict.GetValueOrDefault(item.SampleIndicator!.IndicatorId)
                })
                .Where(x => x.Indicator != null)
                .ToList();

            // Separate children (have ParentIndicatorId) and non-children
            var childItems = itemsWithIndicator
                .Where(x => x.Indicator!.ParentIndicatorId.HasValue)
                .ToList();
            var standaloneItems = itemsWithIndicator
                .Where(x => !x.Indicator!.ParentIndicatorId.HasValue)
                .ToList();

            // Group children by (parentIndicatorId, methodId) to handle same indicator with different methods
            var childrenByParentMethod = childItems
                .GroupBy(x => (
                    ParentId: x.Indicator!.ParentIndicatorId!.Value,
                    MethodId: x.Item.LabTaskIndicator.MethodId
                ))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Track processed parent+method combinations
            var processedParentMethodKeys = new HashSet<(Guid, Guid?)>();

            // Process standalone indicators (may be parents with children)
            foreach (var itemData in standaloneItems.OrderBy(x => x.Indicator!.SortOrder))
            {
                var item = itemData.Item;
                var indicator = itemData.Indicator!;
                var parentMethodId = item.LabTaskIndicator.MethodId;

                // Check if this standalone indicator has children with same method in this sample
                var childKey = (indicator.Id, parentMethodId);
                if (childrenByParentMethod.TryGetValue(childKey, out var children) && children.Any())
                {
                    processedParentMethodKeys.Add(childKey);

                    // Build combined Parameter, Result, and Limits
                    var paramPlainBuilder = new StringBuilder();
                    var paramFormattedBuilder = new StringBuilder();
                    var resultBuilder = new StringBuilder();
                    var limitBuilder = new StringBuilder();
                    var hasAnyFormattedName = !string.IsNullOrEmpty(indicator.FormattedName);

                    // Add parent name
                    paramPlainBuilder.Append(indicator.Name);
                    paramFormattedBuilder.Append(indicator.FormattedName ?? indicator.Name);

                    // Add children
                    foreach (var childData in children.OrderBy(c => c.Indicator!.SortOrder))
                    {
                        var childInd = childData.Indicator!;
                        var childItem = childData.Item;

                        if (!string.IsNullOrEmpty(childInd.FormattedName))
                            hasAnyFormattedName = true;

                        paramPlainBuilder.Append("\n- ");
                        paramPlainBuilder.Append(childInd.Name);

                        paramFormattedBuilder.Append("\n- ");
                        paramFormattedBuilder.Append(childInd.FormattedName ?? childInd.Name);

                        if (resultBuilder.Length > 0) resultBuilder.Append('\n');
                        resultBuilder.Append(ConvertToSuperscriptDisplay(childItem.LabTaskIndicator.Result));

                        // Build limit for this child (use plain text version for combined display)
                        var childStandardId = childItem.QualityIndicator.StandardId ?? childItem.LabTaskIndicator.StandardId;
                        if (childStandardId.HasValue)
                        {
                            var stdLookupKey = (childStandardId.Value, childInd.Id);
                            if (standardIndicatorLookup.TryGetValue(stdLookupKey, out var childStdIndicator) && childStdIndicator != null)
                            {
                                var childLimit = BuildCompareWithStandardPlainText(
                                    childStdIndicator.ComparisonOperator,
                                    childStdIndicator.AllowedLimit,
                                    childStdIndicator.LowerOperator,
                                    childStdIndicator.LowerLimit,
                                    childStdIndicator.UpperOperator,
                                    childStdIndicator.UpperLimit);
                                if (!string.IsNullOrWhiteSpace(childLimit))
                                {
                                    if (limitBuilder.Length > 0) limitBuilder.Append('\n');
                                    limitBuilder.Append(childLimit);
                                }
                            }
                        }
                    }

                    // Get first child's method and unit
                    var firstChild = children.First();
                    TestMethod? method = null;
                    if (firstChild.Item.LabTaskIndicator.MethodId.HasValue)
                    {
                        method = methodDict.GetValueOrDefault(firstChild.Item.LabTaskIndicator.MethodId.Value);
                    }

                    sampleRow.Indicators.Add(new MixedResultIndicatorRow
                    {
                        Parameter = paramPlainBuilder.ToString(),
                        ParameterFormattedName = hasAnyFormattedName ? paramFormattedBuilder.ToString() : null,
                        Unit = ResolveUnitQm(firstChild.Item.LabTaskIndicator),
                        TestMethod = !string.IsNullOrWhiteSpace(firstChild.Item.LabTaskIndicator.SubContractMethodName)
                            ? firstChild.Item.LabTaskIndicator.SubContractMethodName
                            : method?.Name ?? "",
                        TestMethodFormattedName = !string.IsNullOrWhiteSpace(firstChild.Item.LabTaskIndicator.SubContractMethodName)
                            ? null
                            : method?.FormattedName,
                        Result = resultBuilder.ToString(),
                        ReferenceLimit = "",
                        StandardId = null,
                        IndicatorId = indicator.Id,
                        // Store combined limits directly in AllowedLimit (BuildCompareWithStandard will return it as-is)
                        AllowedLimit = limitBuilder.ToString()
                    });
                }
                else
                {
                    // Standalone indicator without children
                    TestMethod? method = null;
                    if (item.LabTaskIndicator.MethodId.HasValue)
                    {
                        method = methodDict.GetValueOrDefault(item.LabTaskIndicator.MethodId.Value);
                    }

                    var resultText = ConvertToSuperscriptDisplay(item.LabTaskIndicator.Result);
                    var effectiveStandardId = item.QualityIndicator.StandardId ?? item.LabTaskIndicator.StandardId;

                    // Get StandardIndicator data
                    var comparisonOperator = "";
                    var allowedLimit = "";
                    var lowerOperator = "";
                    var lowerLimit = "";
                    var upperOperator = "";
                    var upperLimit = "";

                    if (effectiveStandardId.HasValue)
                    {
                        var key = (effectiveStandardId.Value, indicator.Id);
                        if (standardIndicatorLookup.TryGetValue(key, out var stdIndicator) && stdIndicator != null)
                        {
                            comparisonOperator = stdIndicator.ComparisonOperator ?? "";
                            allowedLimit = stdIndicator.AllowedLimit ?? "";
                            lowerOperator = stdIndicator.LowerOperator ?? "";
                            lowerLimit = stdIndicator.LowerLimit ?? "";
                            upperOperator = stdIndicator.UpperOperator ?? "";
                            upperLimit = stdIndicator.UpperLimit ?? "";
                            _logger.LogInformation("StandardIndicator Lookup: IndicatorId={IndicatorId}, LowerOp={LowerOp}, LowerLim={LowerLim}, UpperOp={UpperOp}, UpperLim={UpperLim}",
                                indicator.Id, lowerOperator, lowerLimit, upperOperator, upperLimit);
                        }
                        else
                        {
                            _logger.LogWarning("StandardIndicator NOT FOUND for StandardId={StandardId}, IndicatorId={IndicatorId}",
                                effectiveStandardId.Value, indicator.Id);
                        }
                    }

                    sampleRow.Indicators.Add(new MixedResultIndicatorRow
                    {
                        Parameter = indicator.Name ?? "",
                        ParameterFormattedName = indicator.FormattedName,
                        Unit = ResolveUnitQm(item.LabTaskIndicator),
                        TestMethod = !string.IsNullOrWhiteSpace(item.LabTaskIndicator.SubContractMethodName)
                            ? item.LabTaskIndicator.SubContractMethodName
                            : method?.Name ?? "",
                        TestMethodFormattedName = !string.IsNullOrWhiteSpace(item.LabTaskIndicator.SubContractMethodName)
                            ? null
                            : method?.FormattedName,
                        Result = resultText,
                        ReferenceLimit = item.QualityIndicator.ReferenceLimit ?? "",
                        StandardId = effectiveStandardId,
                        IndicatorId = indicator.Id,
                        ComparisonOperator = comparisonOperator,
                        AllowedLimit = allowedLimit,
                        LowerOperator = lowerOperator,
                        LowerLimit = lowerLimit,
                        UpperOperator = upperOperator,
                        UpperLimit = upperLimit
                    });
                }
            }

            // Process orphan children (parent not in this sample or with different method)
            var orphanKeys = childrenByParentMethod.Keys.Except(processedParentMethodKeys).ToList();
            foreach (var orphanKey in orphanKeys)
            {
                var children = childrenByParentMethod[orphanKey];
                var parentIndicator = indicatorDict.GetValueOrDefault(orphanKey.Item1);

                var paramPlainBuilder = new StringBuilder();
                var paramFormattedBuilder = new StringBuilder();
                var resultBuilder = new StringBuilder();
                var limitBuilder = new StringBuilder();
                var hasAnyFormattedName = parentIndicator != null && !string.IsNullOrEmpty(parentIndicator.FormattedName);

                // Add parent name
                if (parentIndicator != null)
                {
                    paramPlainBuilder.Append(parentIndicator.Name);
                    paramFormattedBuilder.Append(parentIndicator.FormattedName ?? parentIndicator.Name);
                }

                // Add children
                foreach (var childData in children.OrderBy(c => c.Indicator!.SortOrder))
                {
                    var childInd = childData.Indicator!;
                    var childItem = childData.Item;

                    if (!string.IsNullOrEmpty(childInd.FormattedName))
                        hasAnyFormattedName = true;

                    if (paramPlainBuilder.Length > 0) paramPlainBuilder.Append('\n');
                    paramPlainBuilder.Append("- ");
                    paramPlainBuilder.Append(childInd.Name);

                    if (paramFormattedBuilder.Length > 0) paramFormattedBuilder.Append('\n');
                    paramFormattedBuilder.Append("- ");
                    paramFormattedBuilder.Append(childInd.FormattedName ?? childInd.Name);

                    if (resultBuilder.Length > 0) resultBuilder.Append('\n');
                    resultBuilder.Append(ConvertToSuperscriptDisplay(childItem.LabTaskIndicator.Result));

                    // Build limit for this child (use plain text version for combined display)
                    var childStandardId = childItem.QualityIndicator.StandardId ?? childItem.LabTaskIndicator.StandardId;
                    if (childStandardId.HasValue)
                    {
                        var childKey = (childStandardId.Value, childInd.Id);
                        if (standardIndicatorLookup.TryGetValue(childKey, out var childStdIndicator) && childStdIndicator != null)
                        {
                            var childLimit = BuildCompareWithStandardPlainText(
                                childStdIndicator.ComparisonOperator,
                                childStdIndicator.AllowedLimit,
                                childStdIndicator.LowerOperator,
                                childStdIndicator.LowerLimit,
                                childStdIndicator.UpperOperator,
                                childStdIndicator.UpperLimit);
                            if (!string.IsNullOrWhiteSpace(childLimit))
                            {
                                if (limitBuilder.Length > 0) limitBuilder.Append('\n');
                                limitBuilder.Append(childLimit);
                            }
                        }
                    }
                }

                // Get first child's method and unit
                var firstChild = children.First();
                TestMethod? method = null;
                if (firstChild.Item.LabTaskIndicator.MethodId.HasValue)
                {
                    method = methodDict.GetValueOrDefault(firstChild.Item.LabTaskIndicator.MethodId.Value);
                }

                sampleRow.Indicators.Add(new MixedResultIndicatorRow
                {
                    Parameter = paramPlainBuilder.ToString(),
                    ParameterFormattedName = hasAnyFormattedName ? paramFormattedBuilder.ToString() : null,
                    Unit = ResolveUnitQm(firstChild.Item.LabTaskIndicator),
                    TestMethod = !string.IsNullOrWhiteSpace(firstChild.Item.LabTaskIndicator.SubContractMethodName)
                        ? firstChild.Item.LabTaskIndicator.SubContractMethodName
                        : method?.Name ?? "",
                    TestMethodFormattedName = !string.IsNullOrWhiteSpace(firstChild.Item.LabTaskIndicator.SubContractMethodName)
                        ? null
                        : method?.FormattedName,
                    Result = resultBuilder.ToString(),
                    ReferenceLimit = "",
                    StandardId = null,
                    IndicatorId = parentIndicator?.Id,
                    // Store combined limits directly in AllowedLimit
                    AllowedLimit = limitBuilder.ToString()
                });
            }

            // Sort indicators by TestMethod so same-method indicators are grouped together
            sampleRow.Indicators = sampleRow.Indicators
                .OrderBy(r => r.TestMethod, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Re-number indicators within sample
            for (int i = 0; i < sampleRow.Indicators.Count; i++)
            {
                sampleRow.Indicators[i].No = i + 1;
            }

            sampleData.Add(sampleRow);
        }

        _logger.LogInformation("Built {SampleCount} sample rows for mixed result", sampleData.Count);

        // 6. Generate document using BM02 template (default for mixed result with nested SubTable)
        // templatePath already defined above for BM05-QLNN check
        if (!File.Exists(templatePath))
        {
            throw new EntityNotFoundException("Template", input.TemplateName ?? TemplateConstants.TemplateBM02);
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

        // Determine template type and process accordingly
        // BM02 uses nested table structure with {{TestTable}}/{{SubTable}} markers
        // Other templates (BM01, BM04, QLNN) use flat table structure with {{No}}/{{Parameter}} markers
        var isBM02Template = templateFileName.Contains("BM02") || templateFileName.Contains("tổng hợp");

        if (isBM02Template)
        {
            // Use nested table processing for BM02
            _logger.LogInformation("Using BM02 nested table processing for template: {Template}", templateFileName);
            ProcessMixedResultDocument(outputPath, placeholders, sampleData);
        }
        else
        {
            // Convert sampleData to flat tableData for BM01 and other templates
            _logger.LogInformation("Using flat table processing for template: {Template}", templateFileName);
            var tableData = new List<ResultFormTableRow>();
            int rowIndex = 1;

            foreach (var sample in sampleData)
            {
                foreach (var indicator in sample.Indicators)
                {
                    // Get footnote number for {{n}} placeholder
                    // BM05-QLNN: chỉ đánh footnote khi multiple standards
                    // BM04: luôn đánh footnote (kể cả single standard)
                    var footnoteNumber = "";
                    if (isBM05QLNN && bm05StandardInfo != null)
                    {
                        footnoteNumber = GetBM05FootnoteNumber(indicator.StandardId, bm05StandardInfo);
                    }
                    else if (isBM04 && bm04StandardInfo != null)
                    {
                        footnoteNumber = GetBM04FootnoteNumber(indicator.StandardId, bm04StandardInfo);
                    }

                    // Build CompareWithStandard from ComparisonOperator + AllowedLimit (or range fields)
                    // Format: "≤ 4", "≥ 6", "= 7", "> 5 và < 10", etc.
                    _logger.LogInformation("BuildCompareWithStandard: Parameter={Parameter}, LowerOp={LowerOp}, LowerLim={LowerLim}, UpperOp={UpperOp}, UpperLim={UpperLim}",
                        indicator.Parameter, indicator.LowerOperator, indicator.LowerLimit, indicator.UpperOperator, indicator.UpperLimit);
                    var compareWithStandard = BuildCompareWithStandard(
                        indicator.ComparisonOperator,
                        indicator.AllowedLimit,
                        indicator.LowerOperator,
                        indicator.LowerLimit,
                        indicator.UpperOperator,
                        indicator.UpperLimit);

                    tableData.Add(new ResultFormTableRow
                    {
                        No = rowIndex++,
                        Parameters = indicator.Parameter,
                        ParametersFormattedName = indicator.ParameterFormattedName,
                        Unit = indicator.Unit,
                        TestMethod = indicator.TestMethod,
                        TestMethodFormattedName = indicator.TestMethodFormattedName,
                        Result = indicator.Result,
                        Standards = compareWithStandard, // {{Standards}} = same as CompareWithStandard for BM03 template
                        CompareWithStandard = compareWithStandard, // {{CompareWithStandard}} = ComparisonOperator + AllowedLimit
                        FootnoteNumber = footnoteNumber, // {{n}} = Superscript footnote (BM04 & BM05)
                        Allowlimit = indicator.AllowedLimit,
                        Uncertainty = "",
                        Proclaim = ""
                    });
                }
            }

            // Sort by TestMethod name so indicators with the same method are grouped together
            tableData = tableData
                .OrderBy(r => r.TestMethod, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Re-number after sorting
            for (int i = 0; i < tableData.Count; i++)
            {
                tableData[i].No = i + 1;
            }

            // Process document
            ProcessDocument(outputPath, placeholders, tableData);
        }

        // 7. Return DOCX file directly (no PDF conversion) for OnlyOffice editing
        var docxFileName = Path.GetFileName(outputPath);
        _logger.LogInformation("Mixed indicator result form generated: {Path}", outputPath);

        return new GenerateResultFormOutput
        {
            FileName = docxFileName,
            SampleCode = firstSample?.SampleCode ?? ""
        };
    }

    /// <summary>
    /// Generate mixed samples result form (In KQ Hỗn hợp cho nhiều mẫu)
    /// Used from TestRequestDetail page - combines indicators from multiple samples into one DOCX
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

        // Load TestMethodIndicator for per-indicator formatted unit
        var methodIndicators = methodIds.Any() && indicatorIds.Any()
            ? await _testMethodIndicatorRepository.GetListAsync(x =>
                methodIds.Contains(x.TestMethodId) && indicatorIds.Contains(x.IndicatorId))
            : new List<TestMethodIndicator>();

        // Calculate TimeOfTesting: Min(TestDate) - Max(SubmitDate) of selected sample's indicators only
        // - Ngày chỉ tiêu đầu tiên thử nghiệm: Ngày KTV check-in chỉ tiêu đầu tiên của mẫu được chọn
        // - Ngày gửi kết quả cuối cùng: Ngày gửi kết quả (SubmitDate) muộn nhất của mẫu được chọn
        var timeOfTesting = CalculateTimeOfTesting(taskIndicators);

        // Use first sample for header info (or combine if needed)
        var firstSample = samples.FirstOrDefault();
        var sampleCodes = string.Join(", ", samples.Select(s => s.SampleCode));
        var sampleNames = string.Join(", ", samples.Select(s => s.SampleName).Distinct());

        // 2. Prepare placeholder values
        var address = BuildAddress(testRequest);
        var printDate = input.PrintDate
            ?? await GetFirstPrintDateForTestRequestAsync(input.TestRequestId)
            ?? DateTime.Now;
        var printDateText = printDate.ToString("dd/MM/yyyy");
        var contractDateText = testRequest.ContractDate?.ToString("dd/MM/yyyy") ?? "";
        var placeholders = new Dictionary<string, string>
        {
            { "{{SampleCode}}", sampleCodes },
            { "{{CurrentDate}}", printDateText },
            { "{{ContractNo}}", testRequest.ContractNumber ?? "" },
            { "{{Customer}}", testRequest.CustomerName ?? "" },
            { "{{Adrress}}", address },
            { "{{Address}}", address },
            { "{{SamplingAddress}}", firstSample?.SamplingAddress ?? "" },
            { "{{NameOfSample}}", sampleNames },
            { "{{DateOfReceiving}}", contractDateText },
            { "{{TimeOfTesting}}", timeOfTesting },
            { "{{SampleDescription}}", firstSample?.StatusText ?? "" },
            { "{{Quantity}}", string.Join(", ", samples.Select(s => s.SampleQuantity).Where(q => !string.IsNullOrEmpty(q))) },
            { "{{DateOfIssue}}", testRequest.ResultDeliveryDate?.ToString("dd/MM/yyyy") ?? "" },
            { "{{Note}}", input.Note ?? "" }
        };

        // Add Officer In Charge placeholder (QLKT who approved the indicators)
        // Only shows name if exactly 1 approver, otherwise empty
        var officerInChargeName = await GetOfficerInChargeNameAsync(taskIndicators);
        placeholders.Add("{{OfficerInCharge}}", officerInChargeName);

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

            // Resolve unit with TestMethodIndicator fallback
            var resolvedUnit = taskIndicator?.Unit;
            if (taskIndicator != null && (string.IsNullOrWhiteSpace(resolvedUnit) || !HtmlToOpenXmlConverter.ContainsHtmlTags(resolvedUnit)))
            {
                if (taskIndicator.MethodId != null && sampleIndicator != null)
                {
                    var tmi = methodIndicators.FirstOrDefault(x =>
                        x.TestMethodId == taskIndicator.MethodId && x.IndicatorId == sampleIndicator.IndicatorId);
                    if (!string.IsNullOrWhiteSpace(tmi?.FormattedUnit))
                    {
                        resolvedUnit = tmi.FormattedUnit;
                    }
                }
            }

            tableData.Add(new ResultFormTableRow
            {
                No = rowIndex++,
                Parameters = indicator?.Name ?? "",
                ParametersFormattedName = indicator?.FormattedName,
                Unit = string.IsNullOrWhiteSpace(resolvedUnit) ? "-" : resolvedUnit,
                TestMethod = !string.IsNullOrWhiteSpace(taskIndicator?.SubContractMethodName)
                    ? taskIndicator?.SubContractMethodName ?? ""
                    : method?.Name ?? "",
                TestMethodFormattedName = !string.IsNullOrWhiteSpace(taskIndicator?.SubContractMethodName)
                    ? null
                    : method?.FormattedName,
                Result = ConvertToSuperscriptDisplay(taskIndicator?.Result),
                Standards = "",
                Uncertainty = "",
                Proclaim = ""
            });
        }

        // Sort by TestMethod name so indicators with the same method are grouped together
        tableData = tableData
            .OrderBy(r => r.TestMethod, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Re-number after sorting
        for (int i = 0; i < tableData.Count; i++)
        {
            tableData[i].No = i + 1;
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

        // Process document
        ProcessDocument(outputPath, placeholders, tableData);

        // 5. Return DOCX file directly (no PDF conversion) for OnlyOffice editing
        var docxFileName = Path.GetFileName(outputPath);
        _logger.LogInformation("Mixed samples result form generated: {Path}", outputPath);

        // 6. Update TestRequest status to PendingApproval after printing
        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await _testRequestRepository.UpdateAsync(testRequest);
        }

        return new GenerateResultFormOutput
        {
            FileName = docxFileName,
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
    /// Get list of templates available for mixed result printing (In KQ Hỗn hợp)
    /// </summary>
    public Task<List<TemplateDto>> GetMixedTemplatesAsync()
    {
        var templatesDir = Path.Combine(_environment.WebRootPath ?? "", "templates");
        var templates = new List<TemplateDto>();

        foreach (var templateFileName in TemplateConstants.MixedTemplates)
        {
            var filePath = Path.Combine(templatesDir, templateFileName);
            if (File.Exists(filePath))
            {
                var displayName = TemplateConstants.GetDisplayName(templateFileName);
                templates.Add(new TemplateDto
                {
                    Name = displayName,
                    DisplayName = displayName,
                    FileName = templateFileName
                });
            }
        }

        return Task.FromResult(templates);
    }

    /// <summary>
    /// Generate mixed indicator result form per sample (each sample = 1 separate file)
    /// Groups indicators by sample, then calls GenerateMixedIndicatorResultFormAsync for each group
    /// </summary>
    public async Task<List<GenerateResultFormOutput>> GenerateMixedPerSampleResultFormAsync(GenerateMixedIndicatorInput input)
    {
        if (input.IndicatorIds == null || input.IndicatorIds.Count == 0)
            throw new ValidationException("indicatorIds", "Vui lòng chọn ít nhất một chỉ tiêu");

        var qualityIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => input.IndicatorIds.Contains(x.Id));

        var labTaskIndicatorIds = qualityIndicators.Select(qi => qi.LaboratoryTaskIndicatorId).ToList();
        var labTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => labTaskIndicatorIds.Contains(x.Id));

        var sampleIndicatorIds = labTaskIndicators.Select(lti => lti.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(x => sampleIndicatorIds.Contains(x.Id));

        var qiToSampleId = qualityIndicators.ToDictionary(
            qi => qi.Id,
            qi =>
            {
                var lti = labTaskIndicators.FirstOrDefault(l => l.Id == qi.LaboratoryTaskIndicatorId);
                if (lti == null) return Guid.Empty;
                var si = sampleIndicators.FirstOrDefault(s => s.Id == lti.TestRequestSampleIndicatorId);
                return si?.TestRequestSampleId ?? Guid.Empty;
            });

        var groupedBySample = input.IndicatorIds
            .GroupBy(id => qiToSampleId.GetValueOrDefault(id))
            .Where(g => g.Key != Guid.Empty)
            .ToList();

        var results = new List<GenerateResultFormOutput>();
        foreach (var sampleGroup in groupedBySample)
        {
            var perSampleInput = new GenerateMixedIndicatorInput
            {
                TaskId = input.TaskId,
                IndicatorIds = sampleGroup.ToList(),
                Note = input.Note,
                TemplateName = input.TemplateName,
                VicePresidentId = input.VicePresidentId,
                PrintDate = input.PrintDate
            };
            var result = await GenerateMixedIndicatorResultFormAsync(perSampleInput);
            results.Add(result);
        }

        return results;
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
    /// Unicode Superscript character mapping (for exponents: ^)
    /// </summary>
    private static readonly Dictionary<char, char> SuperscriptMap = new()
    {
        {'0', '⁰'}, {'1', '¹'}, {'2', '²'}, {'3', '³'}, {'4', '⁴'},
        {'5', '⁵'}, {'6', '⁶'}, {'7', '⁷'}, {'8', '⁸'}, {'9', '⁹'},
        {'-', '⁻'}, {'+', '⁺'}, {'.', '·'}
    };

    /// <summary>
    /// Unicode Subscript character mapping (for chemical formulas: _)
    /// </summary>
    private static readonly Dictionary<char, char> SubscriptMap = new()
    {
        {'0', '₀'}, {'1', '₁'}, {'2', '₂'}, {'3', '₃'}, {'4', '₄'},
        {'5', '₅'}, {'6', '₆'}, {'7', '₇'}, {'8', '₈'}, {'9', '₉'},
        {'-', '₋'}, {'+', '₊'}
    };

    /// <summary>
    /// Convert caret (^) and underscore (_) notation to Unicode superscript/subscript for display in reports
    /// Also adds line breaks before '(' and after ')' for better formatting
    ///
    /// Superscript examples (^):
    /// - 2^2 → 2²
    /// - 10^-3 → 10⁻³
    /// - 1.5x10^-6 → 1.5×10⁻⁶
    /// - Fe^2+ → Fe²⁺
    /// - SO_4^2- → SO₄²⁻
    ///
    /// Subscript examples (_) - for chemical formulas:
    /// - H_2O → H₂O
    /// - CO_2 → CO₂
    /// - H_2SO_4 → H₂SO₄
    /// - Ca(OH)_2 → Ca(OH)₂
    /// - CuSO_4.5H_2O → CuSO₄.5H₂O
    ///
    /// Parentheses formatting:
    /// - "Không phát hiện (LOD = 1) Test" → "Không phát hiện\n(LOD = 1)\nTest"
    ///
    /// Already-unicode superscript/subscript text is passed through unchanged
    /// </summary>
    private static string ConvertToSuperscriptDisplay(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? "";

        var result = text;

        // 1. Convert 'x' or 'X' between numbers to '×' (multiplication sign)
        // Pattern: number followed by x/X followed by number
        result = Regex.Replace(result, @"(\d)\s*[xX]\s*(\d)", "$1×$2");

        // 2. Convert underscore notation to subscript (for chemical formulas)
        // Pattern: _followed by digits and optional +/- (e.g., H_2, SO_4, NO_3-)
        // Must process before ^ to handle cases like SO_4^2-
        result = Regex.Replace(result, @"_(\d+[+-]?)", match =>
        {
            var subscriptPart = match.Groups[1].Value;
            return new string(subscriptPart.Select(c =>
                SubscriptMap.GetValueOrDefault(c, c)).ToArray());
        });

        // 3. Convert caret notation to superscript (for exponents and ions)
        // Pattern: ^followed by optional sign and digits (e.g., ^2, ^-3, ^2+, ^2-)
        result = Regex.Replace(result, @"\^([+-]?\d+[+-]?)", match =>
        {
            var exponent = match.Groups[1].Value;
            return new string(exponent.Select(c =>
                SuperscriptMap.GetValueOrDefault(c, c)).ToArray());
        });

        // 4. Add line breaks for parentheses content
        // Insert newline before '(' if there's content before it (not at start)
        // Insert newline after ')' if there's content after it (not at end)
        // Example: "Không phát hiện (LOD = 1) Test" → "Không phát hiện\n(LOD = 1)\nTest"
        result = Regex.Replace(result, @"(\S)\s*\(", "$1\n("); // Add newline before '('
        result = Regex.Replace(result, @"\)\s*(\S)", ")\n$1"); // Add newline after ')'

        return result;
    }

    /// <summary>
    /// Merge ghi chú từ QLKT (LaboratoryTaskIndicator) với ghi chú từ QLCL (input parameter)
    /// Format: Ghi chú QLKT + newline + Ghi chú QLCL
    /// </summary>
    private async Task<string> GetMergedNoteAsync(Guid testRequestSampleIndicatorId, string? qualityNote)
    {
        var parts = new List<string>();

        // 1. Lấy ghi chú từ QLKT (LaboratoryTaskIndicator - new model)
        var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
            x => x.TestRequestSampleIndicatorId == testRequestSampleIndicatorId
        );

        if (labTaskIndicator != null)
        {
            var labNote = new List<string>();

            // Add FreeTextNote if exists
            if (!string.IsNullOrWhiteSpace(labTaskIndicator.FreeTextNote))
            {
                labNote.Add(labTaskIndicator.FreeTextNote.Trim());
            }

            // Add MasterDataNoteIds if exists
            if (!string.IsNullOrWhiteSpace(labTaskIndicator.MasterDataNoteIds))
            {
                var noteIds = ParseNoteIds(labTaskIndicator.MasterDataNoteIds);
                if (noteIds.Any())
                {
                    var masterNotes = await _noteRepository.GetListAsync(x => noteIds.Contains(x.Id) && x.IsActive);
                    foreach (var note in masterNotes.OrderBy(x => x.SortOrder))
                    {
                        labNote.Add(note.Content?.Trim() ?? "");
                    }
                }
            }

            if (labNote.Any())
            {
                parts.Add(string.Join("\n", labNote));
            }
        }

        // 2. Add ghi chú từ QLCL
        if (!string.IsNullOrWhiteSpace(qualityNote))
        {
            parts.Add(qualityNote.Trim());
        }

        // 3. Merge: Nối đuôi nhưng tách biệt 2 đoạn ra
        return parts.Count > 0 ? string.Join("\n\n", parts) : string.Empty;
    }

    /// <summary>
    /// Merge ghi chú từ nhiều chỉ tiêu (QLKT) với ghi chú từ QLCL cho In KQ Hỗn Hợp
    /// Logic:
    /// - Gộp tất cả FreeTextNote từ các chỉ tiêu và loại bỏ trùng lặp (nếu giống nhau chỉ lấy 1)
    /// - Gộp tất cả MasterDataNoteIds và loại bỏ trùng lặp
    /// - Thêm ghi chú từ QLCL ở cuối
    /// Format: QLKT FreeText notes + Master data notes + QLCL note
    /// </summary>
    private async Task<string> GetMergedNotesForMultipleIndicatorsAsync(List<Guid> testRequestSampleIndicatorIds, string? qualityNote)
    {
        var parts = new List<string>();

        // 1. Lấy tất cả ghi chú từ QLKT (LaboratoryTaskIndicator - new model)
        var labTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => testRequestSampleIndicatorIds.Contains(x.TestRequestSampleIndicatorId)
        );

        if (labTaskIndicators.Any())
        {
            var allFreeTextNotes = new HashSet<string>(); // Use HashSet to avoid duplicates
            var allMasterNoteIds = new HashSet<Guid>(); // Use HashSet to avoid duplicates

            // Collect all FreeTextNote and MasterDataNoteIds
            foreach (var indicator in labTaskIndicators)
            {
                // Add FreeTextNote if exists (HashSet automatically handles duplicates)
                if (!string.IsNullOrWhiteSpace(indicator.FreeTextNote))
                {
                    allFreeTextNotes.Add(indicator.FreeTextNote.Trim());
                }

                // Collect master data note IDs
                if (!string.IsNullOrWhiteSpace(indicator.MasterDataNoteIds))
                {
                    var noteIds = ParseNoteIds(indicator.MasterDataNoteIds);
                    foreach (var id in noteIds)
                    {
                        allMasterNoteIds.Add(id); // HashSet automatically handles duplicates
                    }
                }
            }

            // Add all FreeText notes (distinct - if same content, only add once)
            if (allFreeTextNotes.Any())
            {
                parts.Add(string.Join("\n", allFreeTextNotes));
            }

            // Fetch and add master data notes (distinct)
            if (allMasterNoteIds.Any())
            {
                var masterNotes = await _noteRepository.GetListAsync(x => allMasterNoteIds.Contains(x.Id) && x.IsActive);
                var masterNoteLines = masterNotes
                    .OrderBy(x => x.SortOrder)
                    .Select(x => x.Content?.Trim() ?? "")
                    .ToList();

                if (masterNoteLines.Any())
                {
                    parts.Add(string.Join("\n", masterNoteLines));
                }
            }
        }

        // 2. Add ghi chú từ QLCL
        if (!string.IsNullOrWhiteSpace(qualityNote))
        {
            parts.Add(qualityNote.Trim());
        }

        // 3. Merge: Nối đuôi nhưng tách biệt các đoạn ra
        return parts.Count > 0 ? string.Join("\n\n", parts) : string.Empty;
    }

    /// <summary>
    /// Parse comma-separated note IDs string to list of Guids
    /// </summary>
    private static List<Guid> ParseNoteIds(string? noteIdsString)
    {
        if (string.IsNullOrEmpty(noteIdsString))
            return new List<Guid>();

        return noteIdsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }

    /// <summary>
    /// Helper class chứa thông tin Standard mapping cho BM05-QLNN template
    /// </summary>
    private class BM05StandardInfo
    {
        /// <summary>
        /// Giá trị cho placeholder {{Standard}}
        /// - Nếu tất cả chỉ tiêu dùng cùng 1 tiêu chuẩn: Tên tiêu chuẩn
        /// - Nếu nhiều tiêu chuẩn: "So với QCVN…/ TCVN…./ QĐ…"
        /// </summary>
        public string StandardText { get; set; } = "";

        /// <summary>
        /// Dictionary mapping StandardId -> Footnote number (1, 2, 3...)
        /// Chỉ sử dụng khi có nhiều tiêu chuẩn
        /// </summary>
        public Dictionary<Guid, int> FootnoteMap { get; set; } = new();

        /// <summary>
        /// Dictionary mapping StandardId -> Standard Name (cho footnotes)
        /// </summary>
        public Dictionary<Guid, string> StandardNames { get; set; } = new();

        /// <summary>
        /// True nếu tất cả chỉ tiêu dùng cùng 1 tiêu chuẩn
        /// </summary>
        public bool IsSingleStandard { get; set; }

        /// <summary>
        /// Text footnotes để append vào {{Note}}
        /// Format: "(1) Tên tiêu chuẩn 1\n(2) Tên tiêu chuẩn 2"
        /// </summary>
        public string FootnotesText { get; set; } = "";
    }

    /// <summary>
    /// Phân tích các chỉ tiêu và tạo thông tin Standard cho BM05-QLNN template
    /// </summary>
    /// <param name="qualityIndicators">Danh sách QualityManagementIndicator</param>
    /// <param name="labTaskIndicators">Danh sách LaboratoryTaskIndicator (optional - để lấy StandardId nếu QualityIndicator không có)</param>
    /// <returns>BM05StandardInfo chứa thông tin placeholder và footnotes</returns>
    private async Task<BM05StandardInfo> GetBM05StandardInfoAsync(
        List<QualityManagementIndicator> qualityIndicators,
        List<LaboratoryTaskIndicator>? labTaskIndicators = null)
    {
        var result = new BM05StandardInfo();

        // Lấy danh sách StandardId từ QualityManagementIndicator
        var standardIdsFromQuality = qualityIndicators
            .Where(qi => qi.StandardId.HasValue)
            .Select(qi => qi.StandardId!.Value);

        // Lấy danh sách StandardId từ LaboratoryTaskIndicator (nếu có)
        var standardIdsFromLabTask = labTaskIndicators?
            .Where(lti => lti.StandardId.HasValue)
            .Select(lti => lti.StandardId!.Value)
            ?? Enumerable.Empty<Guid>();

        // Merge và distinct
        var standardIds = standardIdsFromQuality
            .Concat(standardIdsFromLabTask)
            .Distinct()
            .ToList();

        if (!standardIds.Any())
        {
            // Không có tiêu chuẩn nào được chọn - hiển thị text mặc định
            result.IsSingleStandard = true;
            result.StandardText = "So với QCVN…/ TCVN…./ QĐ…";
            return result;
        }

        // Lấy thông tin các Standard từ database
        var standards = await _standardRepository.GetListAsync(x => standardIds.Contains(x.Id));
        result.StandardNames = standards.ToDictionary(s => s.Id, s => s.Name);

        if (standardIds.Count == 1)
        {
            // Tất cả chỉ tiêu dùng cùng 1 tiêu chuẩn
            result.IsSingleStandard = true;
            var standard = standards.FirstOrDefault();
            result.StandardText = standard?.Name ?? "";
        }
        else
        {
            // Nhiều tiêu chuẩn khác nhau
            result.IsSingleStandard = false;
            result.StandardText = "So với QCVN…/ TCVN…./ QĐ…";

            // Tạo footnote map: gán số thứ tự cho mỗi tiêu chuẩn
            int footnoteNumber = 1;
            foreach (var standardId in standardIds.OrderBy(id => result.StandardNames.GetValueOrDefault(id, "")))
            {
                result.FootnoteMap[standardId] = footnoteNumber++;
            }

            // Tạo footnotes text
            var footnotesLines = result.FootnoteMap
                .OrderBy(kv => kv.Value)
                .Select(kv => $"({kv.Value}) {result.StandardNames.GetValueOrDefault(kv.Key, "")}")
                .ToList();
            result.FootnotesText = string.Join("\n", footnotesLines);
        }

        return result;
    }

    /// <summary>
    /// Lấy footnote number (superscript) cho BM05-QLNN template
    /// - Nếu single standard: trả về empty string
    /// - Nếu multiple standards: trả về superscript footnote như ⁽¹⁾, ⁽²⁾
    /// </summary>
    /// <param name="standardId">StandardId của chỉ tiêu</param>
    /// <param name="standardInfo">BM05StandardInfo chứa footnote mapping</param>
    /// <returns>Footnote number string (superscript) hoặc empty</returns>
    private static string GetBM05FootnoteNumber(Guid? standardId, BM05StandardInfo standardInfo)
    {
        if (standardInfo.IsSingleStandard || !standardId.HasValue)
        {
            // Single standard hoặc không có standardId: không cần footnote
            return "";
        }

        // Multiple standards: trả về superscript footnote number
        if (standardInfo.FootnoteMap.TryGetValue(standardId.Value, out int footnoteNumber))
        {
            // Chuyển số thành superscript: 1 → ⁽¹⁾, 2 → ⁽²⁾, etc.
            var superscriptNumber = new string(footnoteNumber.ToString()
                .Select(c => SuperscriptMap.GetValueOrDefault(c, c))
                .ToArray());
            return $"⁽{superscriptNumber}⁾";
        }

        return "";
    }

    /// <summary>
    /// Kiểm tra template có phải BM05-QLNN không
    /// </summary>
    private static bool IsBM05QLNNTemplate(string? templateFileName)
    {
        if (string.IsNullOrEmpty(templateFileName)) return false;
        return templateFileName.Contains("BM05", StringComparison.OrdinalIgnoreCase)
            && templateFileName.Contains("QLNN", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kiểm tra template có phải BM04 không
    /// </summary>
    private static bool IsBM04Template(string? templateFileName)
    {
        if (string.IsNullOrEmpty(templateFileName)) return false;
        return templateFileName.Contains("BM04", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Lấy footnote number (superscript) cho BM04 template
    /// BM04 luôn đánh footnote cho mọi chỉ tiêu (kể cả single standard)
    /// </summary>
    private static string GetBM04FootnoteNumber(Guid? standardId, BM05StandardInfo standardInfo)
    {
        if (!standardId.HasValue)
        {
            return "";
        }

        // BM04: Luôn trả về superscript footnote number
        if (standardInfo.FootnoteMap.TryGetValue(standardId.Value, out int footnoteNumber))
        {
            var superscriptNumber = new string(footnoteNumber.ToString()
                .Select(c => SuperscriptMap.GetValueOrDefault(c, c))
                .ToArray());
            return $"⁽{superscriptNumber}⁾";
        }

        return "";
    }

    /// <summary>
    /// Phân tích các chỉ tiêu và tạo thông tin Standard cho BM04 template
    /// BM04 luôn tạo footnote map (kể cả single standard)
    /// </summary>
    private async Task<BM05StandardInfo> GetBM04StandardInfoAsync(
        List<QualityManagementIndicator> qualityIndicators,
        List<LaboratoryTaskIndicator>? labTaskIndicators = null)
    {
        var result = new BM05StandardInfo();

        // Lấy danh sách StandardId từ QualityManagementIndicator
        var standardIdsFromQuality = qualityIndicators
            .Where(qi => qi.StandardId.HasValue)
            .Select(qi => qi.StandardId!.Value);

        // Lấy danh sách StandardId từ LaboratoryTaskIndicator (nếu có)
        var standardIdsFromLabTask = labTaskIndicators?
            .Where(lti => lti.StandardId.HasValue)
            .Select(lti => lti.StandardId!.Value)
            ?? Enumerable.Empty<Guid>();

        // Merge và distinct
        var standardIds = standardIdsFromQuality
            .Concat(standardIdsFromLabTask)
            .Distinct()
            .ToList();

        if (!standardIds.Any())
        {
            result.IsSingleStandard = true;
            result.StandardText = "";
            return result;
        }

        // Lấy thông tin các Standard từ database
        var standards = await _standardRepository.GetListAsync(x => standardIds.Contains(x.Id));
        result.StandardNames = standards.ToDictionary(s => s.Id, s => s.Name);

        // BM04: Luôn tạo footnote map (kể cả single standard)
        result.IsSingleStandard = standardIds.Count == 1;

        // Tạo footnote map cho tất cả standards
        int footnoteNumber = 1;
        foreach (var standardId in standardIds.OrderBy(id => result.StandardNames.GetValueOrDefault(id, "")))
        {
            result.FootnoteMap[standardId] = footnoteNumber++;
        }

        // Tạo footnotes text
        var footnotesLines = result.FootnoteMap
            .OrderBy(kv => kv.Value)
            .Select(kv => $"({kv.Value}) {result.StandardNames.GetValueOrDefault(kv.Key, "")}")
            .ToList();
        result.FootnotesText = string.Join("\n", footnotesLines);

        return result;
    }

    /// <summary>
    /// Build CompareWithStandard từ ComparisonOperator và AllowedLimit
    /// Format: "<= 4", ">= 6", "= 7", etc.
    /// </summary>
    /// <param name="comparisonOperator">Ký hiệu so sánh: <=, >=, <, >, =</param>
    /// <param name="allowedLimit">Giới hạn cho phép</param>
    /// <returns>Chuỗi ghép: "comparisonOperator allowedLimit" hoặc empty nếu không có dữ liệu</returns>
    private static string BuildCompareWithStandard(
        string? comparisonOperator,
        string? allowedLimit,
        string? lowerOperator = null,
        string? lowerLimit = null,
        string? upperOperator = null,
        string? upperLimit = null)
    {
        // Nếu có range (cả lowerLimit và upperLimit đều có giá trị)
        if (!string.IsNullOrWhiteSpace(lowerLimit) && !string.IsNullOrWhiteSpace(upperLimit))
        {
            // Default operators: > for lower, < for upper
            // HTML-encode operators to avoid confusion with HTML tags when limits contain HTML (e.g., <sup>)
            var lower = string.IsNullOrWhiteSpace(lowerOperator) ? "&gt;" : HtmlEncodeOperator(lowerOperator);
            var upper = string.IsNullOrWhiteSpace(upperOperator) ? "&lt;" : HtmlEncodeOperator(upperOperator);
            return $"{lower} {lowerLimit.Trim()} và {upper} {upperLimit.Trim()}";
        }

        // Nếu không có allowedLimit, trả về empty
        if (string.IsNullOrWhiteSpace(allowedLimit))
        {
            return "";
        }

        // Nếu có allowedLimit nhưng không có comparisonOperator, chỉ trả về allowedLimit
        if (string.IsNullOrWhiteSpace(comparisonOperator))
        {
            return allowedLimit.Trim();
        }

        // Ghép: "comparisonOperator allowedLimit" (e.g., "≤ 4", "≥ 6", "= 7")
        // HTML-encode operator to avoid confusion with HTML tags
        var op = HtmlEncodeOperator(comparisonOperator);
        return $"{op} {allowedLimit.Trim()}";
    }

    /// <summary>
    /// HTML-encode comparison operators to avoid confusion with HTML tags
    /// Converts < to &lt;, > to &gt;, and also handles <= and >= symbols
    /// </summary>
    private static string HtmlEncodeOperator(string? op)
    {
        if (string.IsNullOrWhiteSpace(op)) return "";

        return op.Trim() switch
        {
            "<=" => "≤",
            ">=" => "≥",
            "<" => "&lt;",
            ">" => "&gt;",
            _ => System.Net.WebUtility.HtmlEncode(op.Trim())
        };
    }

    /// <summary>
    /// Chuyển đổi ký hiệu so sánh từ <= thành ≤, >= thành ≥
    /// </summary>
    private static string ConvertOperatorSymbol(string? op)
    {
        if (string.IsNullOrWhiteSpace(op)) return "";

        return op.Trim() switch
        {
            "<=" => "≤",
            ">=" => "≥",
            _ => op.Trim()
        };
    }

    /// <summary>
    /// Build CompareWithStandard as plain text (no HTML encoding)
    /// Used for combined limits where values are stripped to plain text
    /// </summary>
    private static string BuildCompareWithStandardPlainText(
        string? comparisonOperator,
        string? allowedLimit,
        string? lowerOperator = null,
        string? lowerLimit = null,
        string? upperOperator = null,
        string? upperLimit = null)
    {
        // Strip HTML from limit values to get plain text
        var plainAllowedLimit = HtmlToOpenXmlConverter.ConvertHtmlToPlainText(allowedLimit);
        var plainLowerLimit = HtmlToOpenXmlConverter.ConvertHtmlToPlainText(lowerLimit);
        var plainUpperLimit = HtmlToOpenXmlConverter.ConvertHtmlToPlainText(upperLimit);

        // Nếu có range (cả lowerLimit và upperLimit đều có giá trị)
        if (!string.IsNullOrWhiteSpace(plainLowerLimit) && !string.IsNullOrWhiteSpace(plainUpperLimit))
        {
            // Default operators: > for lower, < for upper
            // Use ConvertOperatorSymbol (no HTML encoding since values are plain text)
            var lower = string.IsNullOrWhiteSpace(lowerOperator) ? ">" : ConvertOperatorSymbol(lowerOperator);
            var upper = string.IsNullOrWhiteSpace(upperOperator) ? "<" : ConvertOperatorSymbol(upperOperator);
            return $"{lower} {plainLowerLimit.Trim()} và {upper} {plainUpperLimit.Trim()}";
        }

        // Nếu không có allowedLimit, trả về empty
        if (string.IsNullOrWhiteSpace(plainAllowedLimit))
        {
            return "";
        }

        // Nếu có allowedLimit nhưng không có comparisonOperator, chỉ trả về allowedLimit
        if (string.IsNullOrWhiteSpace(comparisonOperator))
        {
            return plainAllowedLimit.Trim();
        }

        // Ghép: "comparisonOperator allowedLimit" (e.g., "≤ 4", "≥ 6", "= 7")
        var op = ConvertOperatorSymbol(comparisonOperator);
        return $"{op} {plainAllowedLimit.Trim()}";
    }

    #endregion
}
