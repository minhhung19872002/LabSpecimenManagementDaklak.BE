using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.MasterData.Histories;

/// <summary>
/// Một lần thay đổi của bản ghi master data
/// </summary>
public class MasterDataChangeDto
{
    public Guid Id { get; set; }

    public DateTime ChangeTime { get; set; }

    /// <summary>
    /// Created / Updated / Deleted (enum ABP)
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Nhãn hiển thị: Tạo mới / Sửa / Ngừng hoạt động / Kích hoạt lại / Xóa
    /// </summary>
    public string Action { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public List<MasterDataPropertyChangeDto> Properties { get; set; } = new();
}

public class MasterDataPropertyChangeDto
{
    public string PropertyName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? OriginalValue { get; set; }

    public string? NewValue { get; set; }
}
