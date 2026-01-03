using System;
using System.Collections.Generic;
using LabSpecimenManagement.LaboratoryTasks;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO for Testing detail view
/// </summary>
public class TestingDetailDto : EntityDto<Guid>
{
    /// <summary>
    /// LaboratoryTask ID
    /// </summary>
    public Guid LaboratoryTaskId { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Trạng thái công việc xét nghiệm
    /// </summary>
    public TestingStatus TestingStatus { get; set; }

    /// <summary>
    /// Danh sách tên kỹ thuật viên (comma separated)
    /// </summary>
    public string? TechnicianNames { get; set; }

    /// <summary>
    /// Tên khoa phòng
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Laboratory ID
    /// </summary>
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    /// <summary>
    /// Tên khách hàng
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// ID Quản lý kỹ thuật
    /// </summary>
    public Guid? TechnicalManagerId { get; set; }

    /// <summary>
    /// Tên Quản lý kỹ thuật
    /// </summary>
    public string? TechnicalManagerName { get; set; }

    /// <summary>
    /// Số chỉ tiêu đã hoàn thành (Approved)
    /// </summary>
    public int CompletedIndicatorCount { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu
    /// </summary>
    public int TotalIndicatorCount { get; set; }

    /// <summary>
    /// Danh sách mẫu và chỉ tiêu (grouped by sample)
    /// </summary>
    public List<TestingSampleDto> Samples { get; set; } = new();
}

/// <summary>
/// DTO for sample in Testing detail
/// </summary>
public class TestingSampleDto
{
    /// <summary>
    /// Sample ID (TestRequestSample ID)
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Mã mẫu - Sample Code
    /// </summary>
    public string? SampleCode { get; set; }

    /// <summary>
    /// Tên mẫu - Sample Name
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Tình trạng mẫu - Status Text
    /// </summary>
    public string? StatusText { get; set; }

    /// <summary>
    /// Hình ảnh mẫu từ TestRequest (readonly)
    /// </summary>
    public List<SampleAttachmentDto> SampleImages { get; set; } = new();

    /// <summary>
    /// Hình trước thử nghiệm (max 5)
    /// </summary>
    public List<SampleTestingImageDto> BeforeTestingImages { get; set; } = new();

    /// <summary>
    /// Hình sau thử nghiệm (max 5)
    /// </summary>
    public List<SampleTestingImageDto> AfterTestingImages { get; set; } = new();

    /// <summary>
    /// Danh sách chỉ tiêu của mẫu
    /// </summary>
    public List<TestingIndicatorDto> Indicators { get; set; } = new();
}

/// <summary>
/// DTO for sample attachment (from TestRequest)
/// </summary>
public class SampleAttachmentDto
{
    public Guid Id { get; set; }
    public string FileId { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
}

/// <summary>
/// DTO for sample testing image (before/after testing)
/// </summary>
public class SampleTestingImageDto
{
    public Guid Id { get; set; }
    public Guid SampleId { get; set; }
    public string FileId { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
    public SampleTestingImageType ImageType { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
}

/// <summary>
/// DTO for indicator in Testing detail
/// </summary>
public class TestingIndicatorDto
{
    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TestRequestSampleIndicator ID
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

    /// <summary>
    /// Indicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu - Indicator Name
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// ID phương pháp - Method ID
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// Tên phương pháp - Method Name
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Đơn vị tính - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Kết quả - Result
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Ghi chú - Note
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Ngày nhập kết quả - Entry Date
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// Ngày giao việc - Assigned Date
    /// </summary>
    public DateTime? AssignedDate { get; set; }

    /// <summary>
    /// Ngày bắt đầu thử nghiệm (khi ấn "Thực hiện") - Start Date/Test Date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Ngày gửi kết quả cho QLKT - Submit Date
    /// </summary>
    public DateTime? SubmitDate { get; set; }

    /// <summary>
    /// Ngày hoàn thành (ngày QL duyệt) - Approval Date
    /// </summary>
    public DateTime? CompletionDate { get; set; }

    /// <summary>
    /// Trạng thái duyệt - Approval Status
    /// </summary>
    public IndicatorApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Trạng thái thử nghiệm của chỉ tiêu (tính từ ApprovalStatus)
    /// </summary>
    public IndicatorTestingStatus TestingStatus { get; set; }

    /// <summary>
    /// Lý do từ chối mới nhất - Latest Rejection Reason
    /// </summary>
    public string? LatestRejectionReason { get; set; }

    /// <summary>
    /// Số lượng lịch sử từ chối
    /// </summary>
    public int RejectionHistoryCount { get; set; }

    /// <summary>
    /// ID kỹ thuật viên
    /// </summary>
    public Guid? TechnicianId { get; set; }

    /// <summary>
    /// Tên kỹ thuật viên
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// Có thể chỉnh sửa hay không (dựa vào ApprovalStatus và quyền)
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Có thể check-in (Thực hiện) hay không
    /// </summary>
    public bool CanCheckIn { get; set; }

    /// <summary>
    /// Có thể gửi kết quả hay không
    /// </summary>
    public bool CanSubmit { get; set; }

    /// <summary>
    /// Danh sách hình ảnh của chỉ tiêu (max 5)
    /// </summary>
    public List<IndicatorImageDto> Images { get; set; } = new();
}
