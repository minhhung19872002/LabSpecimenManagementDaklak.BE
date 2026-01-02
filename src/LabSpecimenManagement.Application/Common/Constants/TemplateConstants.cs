namespace LabSpecimenManagement.Common;

/// <summary>
/// Constants for document template filenames and related strings.
/// </summary>
public static class TemplateConstants
{
    #region Template Extensions

    /// <summary>
    /// Template file extension pattern
    /// </summary>
    public const string TemplateExtension = ".template.docx";

    #endregion

    #region Result Form Templates (BM - Biểu mẫu)

    /// <summary>
    /// BM03 - Form trả kết quả đơn lẻ (single indicator result)
    /// </summary>
    public const string TemplateBM03 = "Form trả kq BM03-QT13-BCKQ_3.2.template.docx";

    /// <summary>
    /// BM01 - Form trả kết quả (standard result form)
    /// </summary>
    public const string TemplateBM01 = "Form trả KQ - BM01-QT13-BCKQ_3.2.template.docx";

    /// <summary>
    /// BM02 - Form trả kết quả tổng hợp (mixed indicator result)
    /// </summary>
    public const string TemplateBM02 = "Form trả KQ tổng hợp - BM02-QT13-BCKQ_3.2.template.docx";

    /// <summary>
    /// BM05 - Phiếu yêu cầu (request form)
    /// </summary>
    public const string TemplateBM05 = "BM01-QT07- XXYCĐNHĐ_3.2 phieu yeu cau.template.docx";

    /// <summary>
    /// BM04 - Form thêm cột giới hạn
    /// </summary>
    public const string TemplateBM04 = "Form thêm cột GH BM04-QT13-BCKQ_3.2.template.docx";

    /// <summary>
    /// QLNN - Form mẫu quản lý nhà nước
    /// </summary>
    public const string TemplateQLNN = "form mẫu QLNN BM05-QT13-BCKQ_3.2.template.docx";

    #endregion

    #region Template Display Names

    /// <summary>
    /// Default template name when not specified
    /// </summary>
    public const string DefaultTemplateName = "BM01";

    #endregion

    #region Checkbox Characters (Unicode)

    /// <summary>
    /// Checked checkbox character (U+2611 - BALLOT BOX WITH CHECK)
    /// </summary>
    public const string CheckedBox = "☑";

    /// <summary>
    /// Unchecked checkbox character (U+2610 - BALLOT BOX)
    /// </summary>
    public const string UncheckedBox = "☐";

    #endregion

    #region Standard-based Templates (Templates with specific standards)

    /// <summary>
    /// List of templates that support standard-based indicator display
    /// </summary>
    public static readonly string[] StandardBasedTemplates = new[]
    {
        TemplateQLNN,
        TemplateBM04,
        TemplateBM01,
        TemplateBM03
    };

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the checkbox character based on boolean value
    /// </summary>
    /// <param name="isChecked">Whether the checkbox is checked</param>
    /// <returns>Checked or unchecked box character</returns>
    public static string GetCheckbox(bool isChecked) => isChecked ? CheckedBox : UncheckedBox;

    /// <summary>
    /// Converts template filename to display name by removing extension
    /// </summary>
    /// <param name="templateFileName">Full template filename</param>
    /// <returns>Display name without .template.docx extension</returns>
    public static string GetDisplayName(string templateFileName)
    {
        return templateFileName.Replace(TemplateExtension, "");
    }

    /// <summary>
    /// Gets the full template filename from a template name
    /// </summary>
    /// <param name="templateName">Template name without extension</param>
    /// <returns>Full template filename with extension</returns>
    public static string GetTemplateFileName(string templateName)
    {
        return $"{templateName}{TemplateExtension}";
    }

    #endregion
}
