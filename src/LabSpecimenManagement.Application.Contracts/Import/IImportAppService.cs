using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Validation;

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

    /// <summary>
    /// Get TestMethod with Indicators import template
    /// Template columns: Tên phương pháp, Mô tả, Đơn vị tính, Chỉ tiêu, Đơn giá
    /// </summary>
    byte[] GetTestMethodWithIndicatorsTemplate();

    #endregion

    #region TestMethod with Indicators Import

    /// <summary>
    /// Preview TestMethod with Indicators import - validates data without saving
    /// Groups rows by TestMethodName, validates Indicators exist
    /// </summary>
    [DisableValidation]
    Task<TestMethodImportPreviewDto> PreviewTestMethodsWithIndicatorsAsync(byte[] fileBytes);

    /// <summary>
    /// Import TestMethods with Indicators from Excel file
    /// Upsert logic: match by TestMethodName (trim, case-insensitive)
    /// </summary>
    [DisableValidation]
    Task<ImportResultDto> ImportTestMethodsWithIndicatorsAsync(byte[] fileBytes);

    #endregion

    #region Standard with Indicators Import

    /// <summary>
    /// Get Standard with Indicators import template
    /// Template columns: Tên tiêu chuẩn, Chỉ tiêu, Phương pháp, Đơn vị, So sánh, Giới hạn,
    /// So sánh giới hạn dưới, Giới hạn dưới, So sánh giới hạn trên, Giới hạn trên
    /// </summary>
    byte[] GetStandardWithIndicatorsTemplate();

    /// <summary>
    /// Preview Standard with Indicators import - validates data without saving
    /// Groups rows by StandardName, validates Indicators and TestMethods exist
    /// </summary>
    [DisableValidation]
    Task<StandardImportPreviewDto> PreviewStandardsWithIndicatorsAsync(byte[] fileBytes);

    /// <summary>
    /// Import Standards with Indicators from Excel file
    /// Upsert logic: match by StandardName (trim, case-insensitive)
    /// </summary>
    [DisableValidation]
    Task<ImportResultDto> ImportStandardsWithIndicatorsAsync(byte[] fileBytes);

    #endregion

    #region Organization + TestRequest Import (Legacy Data)

    /// <summary>
    /// Get Organization + TestRequest import template
    /// Template columns: Tên cơ sở, Người liên hệ, Điện thoại, Địa chỉ, Tỉnh/thành, Phường/xã,
    /// Mã số thuế, Số hợp đồng, Tên mẫu, Tình trạng, Mã số mẫu, Chỉ tiêu
    /// </summary>
    byte[] GetOrganizationTestRequestTemplate();

    /// <summary>
    /// Preview Organization + TestRequest import - validates data without saving
    /// Organization upsert: match by OrganizationName (trim, case-insensitive)
    /// TestRequest: always create new with status = Completed
    /// </summary>
    [DisableValidation]
    Task<OrganizationTestRequestImportPreviewDto> PreviewOrganizationTestRequestsAsync(byte[] fileBytes);

    /// <summary>
    /// Import Organization + TestRequest from Excel file
    /// Organization upsert logic: match by OrganizationName (trim, case-insensitive)
    /// TestRequest: always create new with status = Completed
    /// </summary>
    [DisableValidation]
    Task<ImportResultDto> ImportOrganizationTestRequestsAsync(byte[] fileBytes);

    #endregion

    #region IndicatorGroup + Indicator Import

    /// <summary>
    /// Get IndicatorGroup + Indicator import template
    /// Template columns: Tên nhóm chỉ tiêu, Khoa phòng thực hiện, Tên chỉ tiêu
    /// </summary>
    byte[] GetIndicatorGroupTemplate();

    /// <summary>
    /// Preview IndicatorGroup + Indicator import - validates data without saving
    /// IndicatorGroup upsert: match by Name (trim, case-insensitive)
    /// Indicator upsert: match by Name (trim, case-insensitive)
    /// </summary>
    [DisableValidation]
    Task<IndicatorGroupImportPreviewDto> PreviewIndicatorGroupsAsync(byte[] fileBytes);

    /// <summary>
    /// Import IndicatorGroup + Indicator from Excel file
    /// IndicatorGroup upsert: match by Name (trim, case-insensitive)
    /// Indicator upsert: match by Name (trim, case-insensitive)
    /// </summary>
    [DisableValidation]
    Task<ImportResultDto> ImportIndicatorGroupsAsync(byte[] fileBytes);

    #endregion
}
