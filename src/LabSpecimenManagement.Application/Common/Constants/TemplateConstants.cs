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
    /// BM02 2Lab - Form trả kết quả tổng hợp cho 2 phòng thử nghiệm (Hoá + Vi sinh)
    /// </summary>
    public const string TemplateBM02_2Lab = "Form trả KQ tổng hợp - BM02-QT13-BCKQ_3.2.template_2lab.docx";

    /// <summary>
    /// BM05 - Phiếu yêu cầu (request form)
    /// </summary>
    public const string TemplateBM05 = "BM01-QT07- XXYCĐNHĐ_4.0 phieu yeu cau.template.docx";

    /// <summary>
    /// BM03-QT07 - Sổ quản lý mẫu (phiếu bàn giao KH).
    /// Không dùng đuôi .template.docx để không xuất hiện trong danh sách chọn template in KQ.
    /// </summary>
    public const string TemplateSampleManagementBook = "BM03-QT07-XXYCĐNHĐ _4.0.docx";

    /// <summary>
    /// BM04 - Form thêm cột giới hạn
    /// </summary>
    public const string TemplateBM04 = "Form thêm cột GH BM04-QT13-BCKQ_3.2.template.docx";

    /// <summary>
    /// QLNN - Form mẫu quản lý nhà nước
    /// </summary>
    public const string TemplateQLNN = "form mẫu QLNN BM05-QT13-BCKQ_3.2.template.docx";

    /// <summary>
    /// QLNN 2Lab - Form mẫu quản lý nhà nước cho 2 phòng thử nghiệm (Hoá + Vi sinh)
    /// </summary>
    public const string TemplateQLNN_2Lab = "form mẫu QLNN BM05-QT13-BCKQ_3.2.template_2lab.docx";

    /// <summary>
    /// BM01 No Logo - Form trả kết quả (không có logo)
    /// </summary>
    public const string TemplateBM01_NoLogo = "Form trả KQ - BM01-QT13-BCKQ_3.2.no_logo.template.docx";

    /// <summary>
    /// BM04 No Logo - Form thêm cột giới hạn (không có logo)
    /// </summary>
    public const string TemplateBM04_NoLogo = "Form thêm cột GH BM04-QT13-BCKQ_3.2.no_logo.template.docx";

    /// <summary>
    /// QLNN No Logo - Form mẫu quản lý nhà nước (không có logo)
    /// </summary>
    public const string TemplateQLNN_NoLogo = "form mẫu QLNN BM05-QT13-BCKQ_3.2.no_logo.template.docx";

    /// <summary>
    /// BM03 No Logo - Form trả kết quả đơn lẻ (không có logo)
    /// </summary>
    public const string TemplateBM03_NoLogo = "Form trả kq BM03-QT13-BCKQ_3.2.no_logo.template.docx";

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

    #region No Logo Templates

    /// <summary>
    /// Templates without logo (không có logo)
    /// </summary>
    public static readonly string[] NoLogoTemplates = new[]
    {
        TemplateQLNN_NoLogo,
        TemplateBM04_NoLogo,
        TemplateBM01_NoLogo,
        TemplateBM03_NoLogo
    };

    #endregion

    #region Mixed Result Templates

    /// <summary>
    /// Templates available for mixed result printing (In KQ Hỗn hợp)
    /// </summary>
    public static readonly string[] MixedTemplates = new[]
    {
        TemplateBM02,
        TemplateQLNN
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
