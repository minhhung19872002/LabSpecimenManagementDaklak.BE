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
}
