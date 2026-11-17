using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using System.Threading.Tasks;

namespace LabSpecimenManagement.MasterData.BatchNorms;

/// <summary>
/// Phương pháp trong danh sách bên trái màn L3
/// </summary>
public class BatchNormMethodDto
{
    public Guid TestMethodId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? FormattedName { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Số dịch vụ (chỉ tiêu × nền mẫu) dùng phương pháp</summary>
    public int ServiceCount { get; set; }

    /// <summary>Số dòng định mức đang hoạt động</summary>
    public int NormCount { get; set; }

    /// <summary>Có ít nhất 1 dòng đủ vật tư + tổng tiêu hao</summary>
    public bool HasCompleteNorm { get; set; }
}

public class GetBatchNormMethodsInput
{
    public string? Keyword { get; set; }

    /// <summary>null = tất cả, true = đã khai báo, false = chưa có định mức</summary>
    public bool? HasNorm { get; set; }
}

public class MethodBatchNormDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public Guid TestMethodId { get; set; }
    public string? TestMethodCode { get; set; }
    public string? TestMethodName { get; set; }

    public Guid? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductFormattedName { get; set; }

    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }

    public decimal? BatchQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsComplete { get; set; }

    /// <summary>Đã phát sinh tiêu hao → không xóa được, chỉ ngừng (BR-06)</summary>
    public bool IsUsed { get; set; }

    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

public class CreateUpdateMethodBatchNormInput
{
    [Required]
    public Guid TestMethodId { get; set; }

    /// <summary>Không bắt buộc — thiếu thì phương pháp hiển thị "cần thiết lập tiêu hao"</summary>
    public Guid? ProductId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BatchQuantity { get; set; }
}

public interface IMethodBatchNormAppService : IApplicationService
{
    Task<ListResultDto<BatchNormMethodDto>> GetMethodsAsync(GetBatchNormMethodsInput input);

    Task<ListResultDto<MethodBatchNormDto>> GetByMethodAsync(Guid testMethodId);

    Task<MethodBatchNormDto> CreateAsync(CreateUpdateMethodBatchNormInput input);

    Task<MethodBatchNormDto> UpdateAsync(Guid id, CreateUpdateMethodBatchNormInput input);

    Task<MethodBatchNormDto> ToggleActiveAsync(Guid id);

    Task DeleteAsync(Guid id);
}
