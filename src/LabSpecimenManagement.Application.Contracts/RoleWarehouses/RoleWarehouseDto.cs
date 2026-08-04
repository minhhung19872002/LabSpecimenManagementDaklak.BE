using System;

namespace LabSpecimenManagement.RoleWarehouses;

public class RoleWarehouseDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int WarehouseLevel { get; set; }
}
