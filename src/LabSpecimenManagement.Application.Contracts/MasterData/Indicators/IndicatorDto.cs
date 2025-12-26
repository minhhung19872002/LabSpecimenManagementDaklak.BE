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
    public bool IsActive { get; set; }
    public Guid IndicatorGroupId { get; set; }
    public string IndicatorGroupName { get; set; }
    public int SortOrder { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>
/// DTO tạo Chỉ tiêu (Code sẽ được tự động sinh)
/// </summary>
public class CreateIndicatorDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Vui lòng chọn nhóm chỉ tiêu")]
    public Guid IndicatorGroupId { get; set; }

    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// DTO cập nhật Chỉ tiêu
/// </summary>
public class UpdateIndicatorDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; }

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
/// DTO Chỉ tiêu với danh sách phương pháp thử
/// </summary>
public class IndicatorWithMethodsDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
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
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
