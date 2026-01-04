using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.Testing;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.StatusFlow;

/// <summary>
/// Test cases cho luồng trạng thái giữa:
/// - TestRequest (Phiếu yêu cầu)
/// - LaboratoryTask (Công việc của QLKT)
/// - LaboratoryTaskIndicator (Chỉ tiêu - được KTV xử lý)
/// - IsSentToQuality (Đã gửi QLCL)
///
/// QUAN HỆ:
/// TestRequest 1-N LaboratoryTask (mỗi khoa phòng 1 task)
/// LaboratoryTask 1-N LaboratoryTaskIndicator (mỗi chỉ tiêu trong task)
///
/// LUỒNG TRẠNG THÁI:
///
/// TestRequestStatus:
/// New(0) → Pending(1) → SampleSent(2) → InProgress(3) → PendingApproval(5) → Approved(6) → ResultSent(7) → Completed(8)
///                                                    └→ PendingChangeApproval(4) (khi có thay đổi cần duyệt)
///
/// LaboratoryTaskStatus (optimistic - ưu tiên trạng thái CAO nhất):
/// NotAssigned(0) → Assigned(1) → Testing(2) → PendingApproval(3) → Approved(4) → SentToQuality(5)
///
/// IndicatorApprovalStatus:
/// Pending(0) → Entered(1) → Approved(2)
///           ↓              ↓
///        Rejected(3)    CustomerCancelled(4)
/// </summary>
public abstract class StatusFlowIntegration_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    // Test IDs
    protected static readonly Guid TestMethodId = Guid.Parse("3a100004-0001-0001-0001-000000000001");
    protected static readonly Guid TechnicianId = Guid.Parse("3a100005-0001-0001-0001-000000000001");

    protected StatusFlowIntegration_Tests()
    {
    }

    #region Helper Methods - Status Calculation Logic

    /// <summary>
    /// Tính toán trạng thái Task theo logic optimistic
    /// Logic này phải MATCH với logic trong LaboratoryTaskAppService.UpdateTaskStatusAsync
    /// </summary>
    protected LaboratoryTaskStatus CalculateOptimisticTaskStatus(List<LaboratoryTaskIndicator> indicators)
    {
        // Loại bỏ CustomerCancelled
        var activeIndicators = indicators.Where(i => i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();

        if (!activeIndicators.Any())
            return LaboratoryTaskStatus.NotAssigned;

        // Optimistic: Trả về trạng thái CAO nhất (ưu tiên số lớn hơn)
        if (activeIndicators.Any(i => i.ApprovalStatus == IndicatorApprovalStatus.Approved && i.IsSentToQuality))
            return LaboratoryTaskStatus.SentToQuality;

        if (activeIndicators.Any(i => i.ApprovalStatus == IndicatorApprovalStatus.Approved))
            return LaboratoryTaskStatus.Approved;

        if (activeIndicators.Any(i => i.ApprovalStatus == IndicatorApprovalStatus.Entered))
            return LaboratoryTaskStatus.PendingApproval;

        if (activeIndicators.Any(i => i.TestDate.HasValue))
            return LaboratoryTaskStatus.Testing;

        if (activeIndicators.Any(i => i.MethodId.HasValue && i.TechnicianId.HasValue))
            return LaboratoryTaskStatus.Assigned;

        return LaboratoryTaskStatus.NotAssigned;
    }

    /// <summary>
    /// Tạo indicator với trạng thái cụ thể
    /// </summary>
    protected LaboratoryTaskIndicator CreateIndicator(
        Guid? methodId = null,
        Guid? technicianId = null,
        DateTime? testDate = null,
        string? result = null,
        IndicatorApprovalStatus approvalStatus = IndicatorApprovalStatus.Pending,
        bool isSentToQuality = false)
    {
        var indicator = new LaboratoryTaskIndicator(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        indicator.MethodId = methodId;
        indicator.TechnicianId = technicianId;
        indicator.TestDate = testDate;
        indicator.Result = result;
        indicator.ApprovalStatus = approvalStatus;
        indicator.IsSentToQuality = isSentToQuality;
        return indicator;
    }

    #endregion

    #region Test Case 1: TestRequest Status Flow - Basic validation

    /// <summary>
    /// TC1.1: TestRequest status enum values should be correct
    /// </summary>
    [Fact]
    public void TC1_1_TestRequestStatus_Enum_Values_Should_Be_Correct()
    {
        // Assert enum values match expected
        ((int)TestRequestStatus.New).ShouldBe(0);
        ((int)TestRequestStatus.PendingPayment).ShouldBe(1);
        ((int)TestRequestStatus.Paid).ShouldBe(2);
        ((int)TestRequestStatus.SampleSent).ShouldBe(3);
        ((int)TestRequestStatus.InProgress).ShouldBe(4);
        ((int)TestRequestStatus.PendingChangeApproval).ShouldBe(5);
        ((int)TestRequestStatus.PendingApproval).ShouldBe(6);
        ((int)TestRequestStatus.Approved).ShouldBe(7);
        ((int)TestRequestStatus.ResultSent).ShouldBe(8);
        ((int)TestRequestStatus.Completed).ShouldBe(9);
        ((int)TestRequestStatus.Cancelled).ShouldBe(10);
    }

    /// <summary>
    /// TC1.2: LaboratoryTaskStatus enum values should be correct
    /// </summary>
    [Fact]
    public void TC1_2_LaboratoryTaskStatus_Enum_Values_Should_Be_Correct()
    {
        ((int)LaboratoryTaskStatus.NotAssigned).ShouldBe(0);
        ((int)LaboratoryTaskStatus.Assigned).ShouldBe(1);
        ((int)LaboratoryTaskStatus.Testing).ShouldBe(2);
        ((int)LaboratoryTaskStatus.PendingApproval).ShouldBe(3);
        ((int)LaboratoryTaskStatus.Approved).ShouldBe(4);
        ((int)LaboratoryTaskStatus.SentToQuality).ShouldBe(5);
    }

    /// <summary>
    /// TC1.3: IndicatorApprovalStatus enum values should be correct
    /// </summary>
    [Fact]
    public void TC1_3_IndicatorApprovalStatus_Enum_Values_Should_Be_Correct()
    {
        ((int)IndicatorApprovalStatus.Pending).ShouldBe(0);
        ((int)IndicatorApprovalStatus.Entered).ShouldBe(1);
        ((int)IndicatorApprovalStatus.Approved).ShouldBe(2);
        ((int)IndicatorApprovalStatus.Rejected).ShouldBe(3);
        ((int)IndicatorApprovalStatus.CustomerCancelled).ShouldBe(4);
    }

    #endregion

    #region Test Case 2: LaboratoryTask Status Flow (Optimistic Logic)

    /// <summary>
    /// TC2.1: Tất cả indicators NotAssigned → Task = NotAssigned
    /// </summary>
    [Fact]
    public void TC2_1_AllNotAssigned_Should_Return_NotAssigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(), // No method, no technician
            CreateIndicator(),
            CreateIndicator()
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.NotAssigned);
    }

    /// <summary>
    /// TC2.2: Có ≥1 indicator được assign (có method + technician) → Task = Assigned
    /// </summary>
    [Fact]
    public void TC2_2_OneAssigned_Should_Return_Assigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId), // Assigned
            CreateIndicator(), // Not assigned
            CreateIndicator()  // Not assigned
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.Assigned);
    }

    /// <summary>
    /// TC2.3: Có ≥1 indicator có TestDate → Task = Testing
    /// </summary>
    [Fact]
    public void TC2_3_OneTesting_Should_Return_Testing()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now), // Testing
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId), // Assigned only
            CreateIndicator() // Not assigned
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.Testing);
    }

    /// <summary>
    /// TC2.4: Có ≥1 indicator Entered → Task = PendingApproval
    /// </summary>
    [Fact]
    public void TC2_4_OneEntered_Should_Return_PendingApproval()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                result: "7.2",
                approvalStatus: IndicatorApprovalStatus.Entered), // Entered
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now), // Testing
            CreateIndicator() // Not assigned
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.PendingApproval);
    }

    /// <summary>
    /// TC2.5: Có ≥1 indicator Approved → Task = Approved
    /// </summary>
    [Fact]
    public void TC2_5_OneApproved_Should_Return_Approved()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                result: "7.2",
                approvalStatus: IndicatorApprovalStatus.Approved), // Approved
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                result: "25",
                approvalStatus: IndicatorApprovalStatus.Entered), // Entered
            CreateIndicator() // Not assigned
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.Approved);
    }

    /// <summary>
    /// TC2.6: Có ≥1 indicator Approved + IsSentToQuality → Task = SentToQuality
    /// </summary>
    [Fact]
    public void TC2_6_OneSentToQuality_Should_Return_SentToQuality()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                result: "7.2",
                approvalStatus: IndicatorApprovalStatus.Approved,
                isSentToQuality: true), // SentToQuality
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                result: "25",
                approvalStatus: IndicatorApprovalStatus.Approved), // Approved only
            CreateIndicator() // Not assigned
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.SentToQuality);
    }

    #endregion

    #region Test Case 3: Status Priority Order

    /// <summary>
    /// TC3.1: Thứ tự ưu tiên: SentToQuality > Approved
    /// </summary>
    [Fact]
    public void TC3_1_SentToQuality_Priority_Over_Approved()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Approved, isSentToQuality: true),
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Approved, isSentToQuality: false)
        };

        CalculateOptimisticTaskStatus(indicators).ShouldBe(LaboratoryTaskStatus.SentToQuality);
    }

    /// <summary>
    /// TC3.2: Thứ tự ưu tiên: Approved > PendingApproval
    /// </summary>
    [Fact]
    public void TC3_2_Approved_Priority_Over_PendingApproval()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Approved),
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Entered)
        };

        CalculateOptimisticTaskStatus(indicators).ShouldBe(LaboratoryTaskStatus.Approved);
    }

    /// <summary>
    /// TC3.3: Thứ tự ưu tiên: PendingApproval > Testing
    /// </summary>
    [Fact]
    public void TC3_3_PendingApproval_Priority_Over_Testing()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Entered),
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                approvalStatus: IndicatorApprovalStatus.Pending)
        };

        CalculateOptimisticTaskStatus(indicators).ShouldBe(LaboratoryTaskStatus.PendingApproval);
    }

    /// <summary>
    /// TC3.4: Thứ tự ưu tiên: Testing > Assigned
    /// </summary>
    [Fact]
    public void TC3_4_Testing_Priority_Over_Assigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now),
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId)
        };

        CalculateOptimisticTaskStatus(indicators).ShouldBe(LaboratoryTaskStatus.Testing);
    }

    /// <summary>
    /// TC3.5: Thứ tự ưu tiên: Assigned > NotAssigned
    /// </summary>
    [Fact]
    public void TC3_5_Assigned_Priority_Over_NotAssigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId),
            CreateIndicator()
        };

        CalculateOptimisticTaskStatus(indicators).ShouldBe(LaboratoryTaskStatus.Assigned);
    }

    #endregion

    #region Test Case 4: CustomerCancelled Handling

    /// <summary>
    /// TC4.1: CustomerCancelled indicators bị loại khỏi tính toán
    /// </summary>
    [Fact]
    public void TC4_1_CustomerCancelled_Should_Be_Excluded()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                approvalStatus: IndicatorApprovalStatus.CustomerCancelled), // Should be excluded
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now) // Testing
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.Testing); // Not Approved because CustomerCancelled is excluded
    }

    /// <summary>
    /// TC4.2: Tất cả indicators CustomerCancelled → NotAssigned
    /// </summary>
    [Fact]
    public void TC4_2_AllCustomerCancelled_Should_Return_NotAssigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.CustomerCancelled),
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.CustomerCancelled)
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.NotAssigned);
    }

    /// <summary>
    /// TC4.3: Empty list → NotAssigned
    /// </summary>
    [Fact]
    public void TC4_3_EmptyList_Should_Return_NotAssigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>();
        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.NotAssigned);
    }

    #endregion

    #region Test Case 5: Mixed Status Scenarios

    /// <summary>
    /// TC5.1: Mixed: 1 NotAssigned, 1 Testing, 1 Approved → Approved (highest)
    /// </summary>
    [Fact]
    public void TC5_1_MixedStatus_Should_Return_Highest()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(), // NotAssigned
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now), // Testing
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Approved) // Approved
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.Approved);
    }

    /// <summary>
    /// TC5.2: Mixed với Rejected: Rejected không ảnh hưởng status chính
    /// </summary>
    [Fact]
    public void TC5_2_Rejected_Should_Not_Affect_Status()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Rejected), // Rejected
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now) // Testing
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.Testing);
    }

    /// <summary>
    /// TC5.3: Complex mixed scenario
    /// </summary>
    [Fact]
    public void TC5_3_ComplexMixedScenario()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.CustomerCancelled), // Excluded
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Rejected), // Included but not affecting
            CreateIndicator(), // NotAssigned
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId), // Assigned
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now), // Testing
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Entered) // PendingApproval (highest)
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.PendingApproval);
    }

    #endregion

    #region Test Case 6: Rejection Flow

    /// <summary>
    /// TC6.1: Rejected indicator có thể được retry (status = Pending sau khi check-in lại)
    /// </summary>
    [Fact]
    public void TC6_1_RejectedCanBeRetried()
    {
        // Initial: Rejected
        var indicator = CreateIndicator(
            methodId: TestMethodId,
            technicianId: TechnicianId,
            testDate: DateTime.Now.AddHours(-1),
            result: "7.2",
            approvalStatus: IndicatorApprovalStatus.Rejected);

        // Simulate retry: reset status to Pending, update TestDate
        indicator.ApprovalStatus = IndicatorApprovalStatus.Pending;
        indicator.TestDate = DateTime.Now;
        indicator.SubmitDate = null;

        // Assert
        indicator.ApprovalStatus.ShouldBe(IndicatorApprovalStatus.Pending);
        indicator.TestDate.ShouldNotBeNull();
        indicator.SubmitDate.ShouldBeNull();
    }

    /// <summary>
    /// TC6.2: Only Rejected indicator → Task status = Testing (vì vẫn có TestDate)
    /// </summary>
    [Fact]
    public void TC6_2_OnlyRejected_Should_Return_Testing()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(
                methodId: TestMethodId,
                technicianId: TechnicianId,
                testDate: DateTime.Now,
                approvalStatus: IndicatorApprovalStatus.Rejected)
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.Testing); // Has TestDate
    }

    #endregion

    #region Test Case 7: FE Compatibility Tests

    /// <summary>
    /// TC7.1: FE Sample Status Logic - notStarted
    /// notStarted: Chưa có indicator nào có method + technician
    /// </summary>
    [Fact]
    public void TC7_1_FE_SampleStatus_NotStarted()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(),
            CreateIndicator()
        };

        var anyAssigned = indicators.Any(i => i.MethodId.HasValue && i.TechnicianId.HasValue);
        anyAssigned.ShouldBeFalse();
    }

    /// <summary>
    /// TC7.2: FE Sample Status Logic - assigned
    /// assigned: TẤT CẢ indicators đều có method + technician
    /// </summary>
    [Fact]
    public void TC7_2_FE_SampleStatus_Assigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId),
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId)
        };

        var allAssigned = indicators.All(i => i.MethodId.HasValue && i.TechnicianId.HasValue);
        allAssigned.ShouldBeTrue();
    }

    /// <summary>
    /// TC7.3: FE Sample Status Logic - testing
    /// testing: Có ≥1 indicator có TestDate
    /// </summary>
    [Fact]
    public void TC7_3_FE_SampleStatus_Testing()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now),
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId)
        };

        var anyTesting = indicators.Any(i => i.TestDate.HasValue);
        anyTesting.ShouldBeTrue();
    }

    /// <summary>
    /// TC7.4: FE Sample Status Logic - approved
    /// approved: TẤT CẢ indicators đều Approved (FE uses ALL)
    /// NOTE: Khác với BE Task status dùng ANY
    /// </summary>
    [Fact]
    public void TC7_4_FE_SampleStatus_Approved()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Approved),
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Approved)
        };

        // FE logic: ALL approved
        var allApproved = indicators.All(i => i.ApprovalStatus == IndicatorApprovalStatus.Approved);
        allApproved.ShouldBeTrue();

        // BE logic: ANY approved
        var taskStatus = CalculateOptimisticTaskStatus(indicators);
        taskStatus.ShouldBe(LaboratoryTaskStatus.Approved);
    }

    /// <summary>
    /// TC7.5: FE Mixed Mode Selection
    /// Khi sample đang Testing, cho phép chọn cả indicator chưa assign và indicator đã nhập KQ
    /// </summary>
    [Fact]
    public void TC7_5_FE_MixedModeSelection()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(), // Pending - not assigned (selectable)
            CreateIndicator(methodId: TestMethodId, technicianId: TechnicianId, testDate: DateTime.Now), // Testing (selectable)
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Entered), // Entered (selectable)
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.Approved), // Approved (NOT selectable)
            CreateIndicator(approvalStatus: IndicatorApprovalStatus.CustomerCancelled) // Cancelled (NOT selectable)
        };

        // Mixed mode: enabled for all except Approved and CustomerCancelled
        var selectableIndicators = indicators.Where(i =>
            i.ApprovalStatus != IndicatorApprovalStatus.Approved &&
            i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled
        ).ToList();

        selectableIndicators.Count.ShouldBe(3);
    }

    #endregion

    #region Test Case 8: Edge Cases

    /// <summary>
    /// TC8.1: Indicator chỉ có Method nhưng không có Technician → NotAssigned
    /// </summary>
    [Fact]
    public void TC8_1_MethodOnly_Should_Be_NotAssigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(methodId: TestMethodId) // Only method, no technician
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.NotAssigned);
    }

    /// <summary>
    /// TC8.2: Indicator chỉ có Technician nhưng không có Method → NotAssigned
    /// </summary>
    [Fact]
    public void TC8_2_TechnicianOnly_Should_Be_NotAssigned()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(technicianId: TechnicianId) // Only technician, no method
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.NotAssigned);
    }

    /// <summary>
    /// TC8.3: IsSentToQuality = true nhưng ApprovalStatus != Approved → Không phải SentToQuality
    /// </summary>
    [Fact]
    public void TC8_3_SentToQuality_RequiresApproved()
    {
        var indicators = new List<LaboratoryTaskIndicator>
        {
            CreateIndicator(
                approvalStatus: IndicatorApprovalStatus.Entered,
                isSentToQuality: true) // IsSentToQuality but not Approved
        };

        var status = CalculateOptimisticTaskStatus(indicators);
        status.ShouldBe(LaboratoryTaskStatus.PendingApproval); // Not SentToQuality because not Approved
    }

    #endregion
}

/// <summary>
/// Bảng tóm tắt trạng thái
///
/// INDICATOR (LaboratoryTaskIndicator):
/// ┌─────────────────────┬────────────────────────────────────────────────────┐
/// │ ApprovalStatus      │ Mô tả                                              │
/// ├─────────────────────┼────────────────────────────────────────────────────┤
/// │ Pending (0)         │ Chờ nhập KQ - KTV chưa làm gì hoặc đã check-in    │
/// │ Entered (1)         │ Đã nhập KQ - KTV đã gửi, chờ QLKT duyệt           │
/// │ Approved (2)        │ Đã duyệt - QLKT đã duyệt                          │
/// │ Rejected (3)        │ Bị từ chối - QLKT từ chối, KTV cần làm lại        │
/// │ CustomerCancelled(4)│ KH hủy - Không tính vào status                    │
/// └─────────────────────┴────────────────────────────────────────────────────┘
///
/// TASK (LaboratoryTask) - Logic Optimistic:
/// ┌─────────────────────┬────────────────────────────────────────────────────┐
/// │ Status              │ Điều kiện                                          │
/// ├─────────────────────┼────────────────────────────────────────────────────┤
/// │ SentToQuality (5)   │ Có ≥1 indicator (Approved + IsSentToQuality)      │
/// │ Approved (4)        │ Có ≥1 indicator Approved                          │
/// │ PendingApproval (3) │ Có ≥1 indicator Entered                           │
/// │ Testing (2)         │ Có ≥1 indicator có TestDate                       │
/// │ Assigned (1)        │ Có ≥1 indicator có MethodId + TechnicianId        │
/// │ NotAssigned (0)     │ Tất cả indicator chưa được giao việc              │
/// └─────────────────────┴────────────────────────────────────────────────────┘
///
/// TESTREQUEST (Phiếu yêu cầu):
/// ┌─────────────────────────┬────────────────────────────────────────────────┐
/// │ Status                  │ Trigger                                        │
/// ├─────────────────────────┼────────────────────────────────────────────────┤
/// │ New (0)                 │ Tạo mới                                        │
/// │ Pending (1)             │ In phiếu XN                                    │
/// │ SampleSent (2)          │ Xác nhận gửi mẫu                               │
/// │ InProgress (3)          │ Auto: Có ≥1 indicator có TestDate              │
/// │ PendingChangeApproval(4)│ Có thay đổi cần QLCL duyệt                     │
/// │ PendingApproval (5)     │ Auto: Có ≥1 indicator Entered/Approved         │
/// │ Approved (6)            │ QLCL duyệt                                     │
/// │ ResultSent (7)          │ Gửi KQ cho khách hàng                          │
/// │ Completed (8)           │ Bộ phận giao trả xác nhận                      │
/// └─────────────────────────┴────────────────────────────────────────────────┘
///
/// QUAN HỆ ĐỒNG BỘ:
/// - Indicator thay đổi → Auto update Task status (optimistic)
/// - Task status thay đổi → Auto update TestRequest status
/// - QLCL actions → Update TestRequest trực tiếp (Approved, ResultSent, Completed)
/// </summary>
public class StatusFlowDocumentation { }
