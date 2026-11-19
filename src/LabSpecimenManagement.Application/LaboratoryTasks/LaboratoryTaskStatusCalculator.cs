using System.Collections.Generic;
using System.Linq;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Nguồn DUY NHẤT tính trạng thái LaboratoryTask từ các chỉ tiêu.
/// Dùng chung cho LaboratoryTaskAppService (Quản lý công việc) và TestingAppService (Quản lý thử nghiệm)
/// để 2 trang luôn đồng bộ.
/// </summary>
internal static class LaboratoryTaskStatusCalculator
{
    /// <summary>
    /// Loại các chỉ tiêu đã "chốt" khỏi việc tính trạng thái:
    /// - CustomerCancelled: KH đã hủy chỉ tiêu
    /// - TestingRejected + CustomerConfirmationDate: QLKT từ chối TN và KH đã xác nhận
    /// (Chưa loại chỉ tiêu cha có con — việc đó cần DB, làm ở FilterIndicatorsForStatusCalculationAsync.)
    /// </summary>
    public static List<LaboratoryTaskIndicator> ExcludeFinalized(IEnumerable<LaboratoryTaskIndicator> indicators)
    {
        return indicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
                         !(ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected && ti.CustomerConfirmationDate.HasValue))
            .ToList();
    }

    /// <summary>
    /// KTV đã bắt đầu thử nghiệm chỉ tiêu: có ngày TN (check-in / chọn ngày) HOẶC đã lưu kết quả (EntryDate).
    /// Khớp với điều kiện "Đang thí nghiệm" của TestingAppService.CalculateIndicatorTestingStatus.
    /// </summary>
    public static bool HasStarted(LaboratoryTaskIndicator ti) => ti.TestDate.HasValue || ti.EntryDate.HasValue;

    /// <summary>
    /// Tính trạng thái task theo thứ tự ưu tiên BA: hiển thị trạng thái "cần xử lý nhất".
    /// Đầu vào: chỉ tiêu đã qua ExcludeFinalized + lọc chỉ tiêu cha. Trả về null nếu không còn chỉ tiêu nào
    /// (caller giữ nguyên trạng thái hiện tại).
    /// </summary>
    public static LaboratoryTaskStatus? Calculate(IReadOnlyCollection<LaboratoryTaskIndicator> activeIndicators)
    {
        if (activeIndicators.Count == 0)
        {
            return null;
        }

        // Priority 1: Đánh giá TN - có chỉ tiêu cần QLKT đánh giá
        if (activeIndicators.Any(ti =>
                ti.ApprovalStatus == IndicatorApprovalStatus.CannotTest ||
                ti.ApprovalStatus == IndicatorApprovalStatus.SupplementaryRequested ||
                (ti.ApprovalStatus == IndicatorApprovalStatus.Pending && !ti.MethodId.HasValue && !HasStarted(ti))))
        {
            return LaboratoryTaskStatus.UnderEvaluation;
        }

        // Priority 2: Cần phân công - đã chấp nhận TN nhưng chưa giao đủ PP + KTV
        if (activeIndicators.Any(ti =>
                (ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
                 ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged) &&
                (!ti.MethodId.HasValue || !ti.TechnicianId.HasValue)))
        {
            return LaboratoryTaskStatus.FullyAccepted;
        }

        // Priority 3: Chờ KH phản hồi
        if (activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.MethodChangeRequested))
        {
            return LaboratoryTaskStatus.WaitingCustomerResponse;
        }

        // Priority 4: Đã phân công - đã giao KTV nhưng chưa bắt đầu TN
        if (activeIndicators.Any(ti =>
                ti.MethodId.HasValue && ti.TechnicianId.HasValue && !HasStarted(ti) &&
                (ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
                 ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
                 ti.ApprovalStatus == IndicatorApprovalStatus.Pending)))
        {
            return LaboratoryTaskStatus.Assigned;
        }

        // Priority 5: Chờ duyệt KQ - KTV đã gửi kết quả
        if (activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered))
        {
            return LaboratoryTaskStatus.WaitingApproval;
        }

        // Priority 6: Từ chối KQ - QLKT từ chối, KTV cần nhập lại
        if (activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Rejected))
        {
            return LaboratoryTaskStatus.Rejected;
        }

        // Priority 7: Đang TN - KTV đã bắt đầu (check-in / chọn ngày TN / lưu kết quả) nhưng chưa gửi
        if (activeIndicators.Any(ti =>
                HasStarted(ti) &&
                (ti.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                 ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
                 ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged)))
        {
            return LaboratoryTaskStatus.InProgress;
        }

        // Priority 8: Đã gửi QLCL - tất cả đã gửi
        if (activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved && ti.IsSentToQuality))
        {
            return LaboratoryTaskStatus.SentToQuality;
        }

        // Priority 9: Từ chối TN - tất cả bị từ chối
        if (activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected))
        {
            return LaboratoryTaskStatus.TestingRejected;
        }

        // Đã duyệt (tất cả approved nhưng chưa gửi hết QLCL)
        if (activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved))
        {
            return LaboratoryTaskStatus.Approved;
        }

        return LaboratoryTaskStatus.UnderEvaluation;
    }
}
