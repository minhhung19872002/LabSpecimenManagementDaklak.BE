using System;

namespace LabSpecimenManagement.UserWarehouses;

public class UserWarehouseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int WarehouseLevel { get; set; }
}
