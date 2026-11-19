using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using System.Threading.Tasks;

namespace LabSpecimenManagement.MasterData.BatchNorms;

/// <summary>
/// 1 định mức theo mẻ của 1 dịch vụ (Nền mẫu × Chỉ tiêu × Phương pháp) — 1 dòng PP trên màn L3
/// </summary>
public class ServiceBatchNormDto
{
    public Guid Id { get; set; }
    public Guid TestMethodIndicatorId { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceGroupName { get; set; }

    public Guid TestMethodId { get; set; }
    public string? TestMethodCode { get; set; }
    public string? TestMethodName { get; set; }
    public string? TestMethodFormattedName { get; set; }

    public Guid IndicatorId { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorName { get; set; }
    public string? IndicatorFormattedName { get; set; }

    /// <summary>null = Mọi nền mẫu</summary>
    public Guid? SpecimenTypeId { get; set; }
    public string? SpecimenTypeName { get; set; }
    public int SpecimenTypeSortOrder { get; set; }
    public Guid? SampleTypeCategoryId { get; set; }
    public string? SampleTypeCategoryName { get; set; }

    /// <summary>Trạng thái sử dụng — ngừng thì không trừ tiêu hao</summary>
    public bool IsActive { get; set; }

    /// <summary>Số vật tư (dòng định mức đang hoạt động có vật tư)</summary>
    public int MaterialCount { get; set; }

    /// <summary>Thiết bị: chưa lưu (màn sửa định mức sẽ thiết kế lại)</summary>
    public int EquipmentCount { get; set; }

    /// <summary>Đã phát sinh tiêu hao → không xóa được, chỉ ngừng (BR-06)</summary>
    public bool IsUsed { get; set; }

    public DateTime CreationTime { get; set; }
}

/// <summary>
/// Dịch vụ đã khai báo (Phiếu khai báo dịch vụ) nhưng chưa thêm vào định mức —
/// chỉ trả về cho cặp (nền mẫu, chỉ tiêu) đã có ít nhất 1 định mức.
/// </summary>
public class BatchNormCandidateDto
{
    public Guid TestMethodIndicatorId { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceGroupName { get; set; }
    public Guid TestMethodId { get; set; }
    public string? TestMethodCode { get; set; }
    public string? TestMethodName { get; set; }
    public Guid IndicatorId { get; set; }
    public Guid? SpecimenTypeId { get; set; }
}

public class BatchNormOverviewDto
{
    public List<ServiceBatchNormDto> Norms { get; set; } = new();
    public List<BatchNormCandidateDto> Candidates { get; set; } = new();
}

public class AddServiceBatchNormsInput
{
    [Required]
    [MinLength(1)]
    public List<Guid> TestMethodIndicatorIds { get; set; } = new();
}

/// <summary>Dòng vật tư trong định mức của dịch vụ</summary>
public class MethodBatchNormDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public Guid? ServiceBatchNormId { get; set; }
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
    /// <summary>Dịch vụ (nền mẫu × chỉ tiêu × PP). Chưa có định mức thì tự tạo.</summary>
    [Required]
    public Guid TestMethodIndicatorId { get; set; }

    /// <summary>Không bắt buộc — thiếu thì dịch vụ hiển thị "cần thiết lập tiêu hao"</summary>
    public Guid? ProductId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BatchQuantity { get; set; }
}

public interface IMethodBatchNormAppService : IApplicationService
{
    Task<BatchNormOverviewDto> GetOverviewAsync();

    Task<ListResultDto<ServiceBatchNormDto>> AddServicesAsync(AddServiceBatchNormsInput input);

    Task<ServiceBatchNormDto> ToggleServiceActiveAsync(Guid id);

    Task DeleteServiceAsync(Guid id);

    /// <summary>Hoàn tác xóa</summary>
    Task<ServiceBatchNormDto> RestoreServiceAsync(Guid id);

    Task<ListResultDto<MethodBatchNormDto>> GetByServiceAsync(Guid testMethodIndicatorId);

    Task<MethodBatchNormDto> CreateAsync(CreateUpdateMethodBatchNormInput input);

    Task<MethodBatchNormDto> UpdateAsync(Guid id, CreateUpdateMethodBatchNormInput input);

    Task<MethodBatchNormDto> ToggleActiveAsync(Guid id);

    Task DeleteAsync(Guid id);
}
