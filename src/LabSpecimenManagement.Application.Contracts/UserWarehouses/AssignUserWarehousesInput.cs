using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.UserWarehouses;

public class AssignUserWarehousesInput
{
    public List<Guid> WarehouseIds { get; set; } = new();
}
