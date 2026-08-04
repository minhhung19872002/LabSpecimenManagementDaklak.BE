using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.RoleWarehouses;

public class AssignRoleWarehousesInput
{
    public List<Guid> WarehouseIds { get; set; } = new();
}
