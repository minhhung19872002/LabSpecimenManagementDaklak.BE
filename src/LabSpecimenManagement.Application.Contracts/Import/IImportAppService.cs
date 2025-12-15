using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Import;

/// <summary>
/// Service for importing data from Excel files
/// </summary>
public interface IImportAppService : IApplicationService
{
    #region Preview (Validation only)

    /// <summary>
    /// Preview TestRequest import - validates data without saving
    /// </summary>
    Task<ImportPreviewDto<TestRequestImportRow>> PreviewTestRequestsAsync(Stream fileStream);

    /// <summary>
    /// Preview Specimen import - validates data without saving
    /// </summary>
    Task<ImportPreviewDto<SpecimenImportRow>> PreviewSpecimensAsync(Stream fileStream);

    /// <summary>
    /// Preview Customer import - validates data without saving
    /// </summary>
    Task<ImportPreviewDto<CustomerImportRow>> PreviewCustomersAsync(Stream fileStream);

    /// <summary>
    /// Preview TestMethod import - validates data without saving
    /// </summary>
    Task<ImportPreviewDto<TestMethodImportRow>> PreviewTestMethodsAsync(Stream fileStream);

    #endregion

    #region Import (Save to database)

    /// <summary>
    /// Import TestRequests from Excel file
    /// </summary>
    Task<ImportResultDto> ImportTestRequestsAsync(Stream fileStream);

    /// <summary>
    /// Import Specimens from Excel file
    /// </summary>
    Task<ImportResultDto> ImportSpecimensAsync(Stream fileStream);

    /// <summary>
    /// Import Customers from Excel file
    /// </summary>
    Task<ImportResultDto> ImportCustomersAsync(Stream fileStream);

    /// <summary>
    /// Import TestMethods from Excel file
    /// </summary>
    Task<ImportResultDto> ImportTestMethodsAsync(Stream fileStream);

    #endregion

    #region Templates

    /// <summary>
    /// Get TestRequest import template
    /// </summary>
    byte[] GetTestRequestTemplate();

    /// <summary>
    /// Get Specimen import template
    /// </summary>
    byte[] GetSpecimenTemplate();

    /// <summary>
    /// Get Customer import template
    /// </summary>
    byte[] GetCustomerTemplate();

    /// <summary>
    /// Get TestMethod import template
    /// </summary>
    byte[] GetTestMethodTemplate();

    #endregion
}
