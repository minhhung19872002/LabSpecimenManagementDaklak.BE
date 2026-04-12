using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Indicators;

/// <summary>
/// DTO cho Chỉ tiêu
/// </summary>
public class IndicatorDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    /// <summary>
    /// Tên chỉ tiêu có định dạng HTML (bold, italic, underline, sub, sup)
    /// Nếu null thì hiển thị Name
    /// </summary>
    public string? FormattedName { get; set; }
    public bool IsActive { get; set; }
    public Guid IndicatorGroupId { get; set; }
    public string IndicatorGroupName { get; set; }
    public int SortOrder { get; set; }
    public bool CanDelete { get; set; }

    /// <summary>
    /// ID chỉ tiêu cha (null nếu là chỉ tiêu gốc)
    /// </summary>
    public Guid? ParentIndicatorId { get; set; }

    /// <summary>
    /// Mã chỉ tiêu cha
    /// </summary>
    public string? ParentIndicatorCode { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu phụ
    /// </summary>
    public List<IndicatorDto> SubIndicators { get; set; } = new();

    /// <summary>
    /// Có chỉ tiêu phụ hay không
    /// </summary>
    public bool HasSubIndicators { get; set; }
}

/// <summary>
/// DTO tạo Chỉ tiêu (Code sẽ được tự động sinh)
/// </summary>
public class CreateIndicatorDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    /// <summary>
    /// Tên chỉ tiêu có định dạng HTML (bold, italic, underline, sub, sup)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Tên định dạng không được vượt quá 1000 ký tự")]
    public string? FormattedName { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Vui lòng chọn nhóm chỉ tiêu")]
    public Guid IndicatorGroupId { get; set; }

    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// ID chỉ tiêu cha (null nếu là chỉ tiêu gốc)
    /// </summary>
    public Guid? ParentIndicatorId { get; set; }
}

/// <summary>
/// DTO cập nhật Chỉ tiêu
/// </summary>
public class UpdateIndicatorDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    /// <summary>
    /// Tên chỉ tiêu có định dạng HTML (bold, italic, underline, sub, sup)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Tên định dạng không được vượt quá 1000 ký tự")]
    public string? FormattedName { get; set; }

    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhóm chỉ tiêu")]
    public Guid IndicatorGroupId { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>
/// DTO tạo nhiều Chỉ tiêu cùng lúc
/// </summary>
public class CreateBulkIndicatorsDto
{
    [Required(ErrorMessage = "Vui lòng chọn nhóm chỉ tiêu")]
    public Guid IndicatorGroupId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập danh sách chỉ tiêu")]
    [MinLength(1, ErrorMessage = "Danh sách chỉ tiêu phải có ít nhất 1 phần tử")]
    public List<BulkIndicatorItemDto> Indicators { get; set; } = new();
}

/// <summary>
/// DTO cho một item trong danh sách tạo bulk
/// </summary>
public class BulkIndicatorItemDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    /// <summary>
    /// Tên chỉ tiêu có định dạng HTML (nếu có rich text)
    /// </summary>
    [StringLength(1000)]
    public string? FormattedName { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu phụ (tùy chọn)
    /// </summary>
    public List<BulkSubIndicatorItemDto> SubIndicators { get; set; } = new();
}

/// <summary>
/// DTO cho một chỉ tiêu phụ trong danh sách tạo bulk
/// </summary>
public class BulkSubIndicatorItemDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu phụ")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu phụ không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    /// <summary>
    /// Tên chỉ tiêu phụ có định dạng HTML (nếu có rich text)
    /// </summary>
    [StringLength(1000)]
    public string? FormattedName { get; set; }
}

/// <summary>
/// DTO lọc/tìm kiếm Chỉ tiêu
/// </summary>
public class IndicatorFilterDto : PagedAndSortedResultRequestDto
{
    public string? Search { get; set; }
    public Guid? IndicatorGroupId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO kết quả import Excel
/// </summary>
public class ImportIndicatorsResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportIndicatorErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// DTO lỗi import
/// </summary>
public class ImportIndicatorErrorDto
{
    public int RowNumber { get; set; }
    public string IndicatorName { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>
/// DTO cho batch delete/deactivate
/// </summary>
public class BatchIndicatorIdsDto
{
    [Required(ErrorMessage = "Vui lòng chọn ít nhất một chỉ tiêu")]
    [MinLength(1, ErrorMessage = "Danh sách chỉ tiêu phải có ít nhất 1 phần tử")]
    public List<Guid> Ids { get; set; } = new();
}

/// <summary>
/// DTO kết quả batch operation
/// </summary>
public class BatchOperationResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<BatchOperationErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// DTO lỗi batch operation
/// </summary>
public class BatchOperationErrorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>
/// DTO cập nhật chỉ tiêu cha kèm quản lý chỉ tiêu phụ (thêm/sửa/xóa)
/// </summary>
public class UpdateIndicatorWithSubsDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu")]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string? FormattedName { get; set; }

    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhóm chỉ tiêu")]
    public Guid IndicatorGroupId { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu phụ sau khi chỉnh sửa.
    /// Id == null → tạo mới; Id có giá trị → cập nhật existing.
    /// Sub-indicators bị bỏ khỏi danh sách → sẽ bị xóa (nếu chưa sử dụng).
    /// </summary>
    public List<UpsertSubIndicatorDto> SubIndicators { get; set; } = new();
}

/// <summary>
/// DTO cho một chỉ tiêu phụ trong UpdateWithSubs
/// </summary>
public class UpsertSubIndicatorDto
{
    /// <summary>
    /// null = tạo mới; có giá trị = cập nhật existing
    /// </summary>
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu phụ")]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string? FormattedName { get; set; }
}

/// <summary>
/// DTO Chỉ tiêu với danh sách phương pháp thử
/// </summary>
public class IndicatorWithMethodsDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string? FormattedName { get; set; }
    public Guid IndicatorGroupId { get; set; }
    public string IndicatorGroupName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<IndicatorMethodDto> Methods { get; set; } = new();
}

/// <summary>
/// DTO Phương pháp thử cho một chỉ tiêu
/// </summary>
public class IndicatorMethodDto
{
    public Guid TestMethodId { get; set; }
    public string TestMethodCode { get; set; }
    public string TestMethodName { get; set; }
    public string? TestMethodFormattedName { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Nền mẫu (Loại mẫu)
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }
    public string? SpecimenTypeCode { get; set; }
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// Đơn vị tính (từ TestMethodIndicator, fallback TestMethod.Unit)
    /// </summary>
    public string? Unit { get; set; }
    public string? FormattedUnit { get; set; }
}
