using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// Input for bulk updating indicator method (KTV bulk cập nhật phương pháp)
/// </summary>
public class BulkUpdateIndicatorMethodInput
{
    /// <summary>
    /// Danh sách LaboratoryTaskIndicator IDs cần cập nhật phương pháp
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Phương pháp mới - Method ID (null = xóa phương pháp)
    /// </summary>
    public Guid? MethodId { get; set; }
}
