using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// Application service interface for result form generation
/// </summary>
public interface IResultFormAppService : IApplicationService
{
    /// <summary>
    /// Generate result form document from template (BM03)
    /// </summary>
    Task<GenerateResultFormOutput> GenerateResultFormAsync(GenerateResultFormInput input);

    /// <summary>
    /// Get samples for result form selection with testing status
    /// </summary>
    Task<List<SampleForSelectionDto>> GetSamplesForResultFormAsync(Guid testRequestId);

    /// <summary>
    /// Generate BM01 result form document for single sample
    /// </summary>
    Task<GenerateResultFormOutput> GenerateBM01ResultFormAsync(GenerateBM01ResultFormInput input);

    /// <summary>
    /// Generate test request form document (BM05 - phiếu yêu cầu XN)
    /// </summary>
    Task<GenerateTestRequestFormOutput> GenerateTestRequestFormAsync(GenerateTestRequestFormInput input);

    /// <summary>
    /// Generate single indicator result form (In KQ Đơn) for Quality Management
    /// </summary>
    Task<GenerateResultFormOutput> GenerateSingleIndicatorResultFormAsync(GenerateSingleIndicatorInput input);

    /// <summary>
    /// Generate mixed indicator result form (In KQ Hỗn hợp) for Quality Management
    /// </summary>
    Task<GenerateResultFormOutput> GenerateMixedIndicatorResultFormAsync(GenerateMixedIndicatorInput input);

    /// <summary>
    /// Generate mixed samples result form (In KQ Hỗn hợp cho nhiều mẫu)
    /// Used from TestRequestDetail page - combines indicators from multiple samples
    /// </summary>
    Task<GenerateResultFormOutput> GenerateMixedSamplesResultFormAsync(GenerateMixedSamplesResultFormInput input);

    /// <summary>
    /// Get list of available templates (files with .template.docx extension)
    /// </summary>
    Task<List<TemplateDto>> GetTemplatesAsync();
}
