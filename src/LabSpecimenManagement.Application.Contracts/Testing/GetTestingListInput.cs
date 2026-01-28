using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// Input for filtering Testing list
/// </summary>
public class GetTestingListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Tab type: MyAssignments (phân công của tôi) or All (hiển thị tất cả)
    /// </summary>
    public TestingTabType Tab { get; set; } = TestingTabType.MyAssignments;

    /// <summary>
    /// Tìm kiếm theo số hợp đồng - Search by Contract Number
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Lọc theo trạng thái (multiple select) - Filter by Status
    /// </summary>
    public List<TestingStatus>? Statuses { get; set; }

    /// <summary>
    /// Lọc theo Thử nghiệm viên (multiple select) - Filter by Technician IDs
    /// AND logic với current user nếu đang ở tab MyAssignments
    /// </summary>
    public List<Guid>? TechnicianIds { get; set; }

    /// <summary>
    /// Lọc theo khoa phòng - Filter by Laboratory ID
    /// </summary>
    public Guid? LaboratoryId { get; set; }
}

/// <summary>
/// Tab type for Testing list
/// </summary>
public enum TestingTabType
{
    /// <summary>
    /// Phân công của tôi - Chỉ hiển thị công việc được assign cho current user
    /// </summary>
    MyAssignments = 0,

    /// <summary>
    /// Hiển thị tất cả - Hiển thị tất cả công việc
    /// </summary>
    All = 1
}
