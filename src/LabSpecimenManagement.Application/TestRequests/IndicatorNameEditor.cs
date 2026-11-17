using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Quotations;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Users;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Admin sửa tên chỉ tiêu trên chứng từ (phiếu thử nghiệm / báo giá).
/// - Chỉ ghi vào snapshot của dòng chỉ tiêu → mọi màn đọc tên qua snapshot (QLCV, QLCL, thử nghiệm, phiếu) tự cập nhật.
/// - KHÔNG đổi master data.
/// - Mỗi lần đổi tên ghi 1 dòng <see cref="IndicatorNameChangeHistory"/>.
/// </summary>
public class IndicatorNameEditor : ITransientDependency
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private const int QuotationIndicatorNameMaxLength = 256;

    private readonly IRepository<IndicatorNameChangeHistory, Guid> _historyRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentUser _currentUser;

    public IndicatorNameEditor(
        IRepository<IndicatorNameChangeHistory, Guid> historyRepository,
        IGuidGenerator guidGenerator,
        ICurrentUser currentUser)
    {
        _historyRepository = historyRepository;
        _guidGenerator = guidGenerator;
        _currentUser = currentUser;
    }

    /// <summary>Chỉ admin được sửa tên chỉ tiêu trên chứng từ</summary>
    public bool CanEdit => _currentUser.IsInRole(AppConstants.RoleAdmin);

    /// <summary>
    /// Chuẩn hóa tên nhận từ FE. Trả null nếu không có tên (= không sửa).
    /// Thiếu tên định dạng → dùng tên thường; thiếu tên thường → bỏ thẻ HTML của tên định dạng.
    /// </summary>
    public static (string Name, string FormattedName)? Normalize(string? name, string? formattedName)
    {
        var formatted = formattedName?.Trim();
        var plain = name?.Trim();

        if (string.IsNullOrEmpty(plain) && !string.IsNullOrEmpty(formatted))
        {
            plain = ToPlainText(formatted);
        }
        if (string.IsNullOrEmpty(plain))
        {
            return null;
        }
        if (string.IsNullOrEmpty(formatted))
        {
            formatted = WebUtility.HtmlEncode(plain);
        }
        return (plain, formatted);
    }

    /// <summary>Bỏ thẻ HTML, decode entity, gộp khoảng trắng</summary>
    public static string ToPlainText(string html)
    {
        var text = HtmlTagRegex.Replace(html, string.Empty);
        text = WebUtility.HtmlDecode(text);
        return WhitespaceRegex.Replace(text, " ").Trim();
    }

    /// <summary>
    /// Áp tên sửa cho dòng chỉ tiêu của phiếu (gọi SAU khi đã chụp snapshot từ master).
    /// allowed = false → bỏ qua (user không có quyền).
    /// </summary>
    public async Task ApplyAsync(
        TestRequestSampleIndicator item,
        Guid testRequestId,
        string? name,
        string? formattedName,
        bool allowed)
    {
        if (!allowed)
        {
            return;
        }

        var normalized = Normalize(name, formattedName);
        if (normalized == null || IsSame(normalized.Value, Normalize(item.IndicatorName, item.IndicatorFormattedName)))
        {
            return;
        }

        await LogAsync(IndicatorNameChangeSource.TestRequest, testRequestId, item.Id, item.IndicatorId,
            item.IndicatorName, item.IndicatorFormattedName, normalized.Value.Name, normalized.Value.FormattedName);

        item.IndicatorName = normalized.Value.Name;
        item.IndicatorFormattedName = normalized.Value.FormattedName;
    }

    /// <summary>
    /// Áp tên sửa cho dòng chỉ tiêu của báo giá. Trả true nếu đã áp.
    /// </summary>
    public async Task<bool> ApplyAsync(
        QuotationSampleIndicator item,
        Guid quotationId,
        string? name,
        string? formattedName)
    {
        if (!CanEdit)
        {
            return false;
        }

        var normalized = Normalize(name, formattedName);
        if (normalized == null)
        {
            return false;
        }
        if (normalized.Value.Name.Length > QuotationIndicatorNameMaxLength)
        {
            throw new UserFriendlyException($"Tên chỉ tiêu trên báo giá tối đa {QuotationIndicatorNameMaxLength} ký tự");
        }
        if (IsSame(normalized.Value, Normalize(item.IndicatorName, item.IndicatorFormattedName)))
        {
            return true;
        }

        await LogAsync(IndicatorNameChangeSource.Quotation, quotationId, item.Id, item.IndicatorId,
            item.IndicatorName, item.IndicatorFormattedName, normalized.Value.Name, normalized.Value.FormattedName);

        item.IndicatorName = normalized.Value.Name;
        item.IndicatorFormattedName = normalized.Value.FormattedName;
        return true;
    }

    /// <summary>
    /// So tên không phân biệt dạng mã hóa Unicode (dữ liệu cũ lưu tiếng Việt dạng tổ hợp NFD, editor gửi NFC)
    /// </summary>
    private static bool IsSame((string Name, string FormattedName) a, (string Name, string FormattedName)? b)
    {
        return b != null
            && a.Name.Normalize(NormalizationForm.FormC) == b.Value.Name.Normalize(NormalizationForm.FormC)
            && a.FormattedName.Normalize(NormalizationForm.FormC) == b.Value.FormattedName.Normalize(NormalizationForm.FormC);
    }

    private async Task LogAsync(
        IndicatorNameChangeSource source,
        Guid documentId,
        Guid lineId,
        Guid indicatorId,
        string? oldName,
        string? oldFormattedName,
        string newName,
        string newFormattedName)
    {
        await _historyRepository.InsertAsync(new IndicatorNameChangeHistory(
            _guidGenerator.Create(),
            source,
            documentId,
            lineId,
            indicatorId,
            oldName,
            oldFormattedName,
            newName,
            newFormattedName));
    }
}
