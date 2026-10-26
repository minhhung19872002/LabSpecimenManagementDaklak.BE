using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Products;

public interface IInventoryBarcodeRepository : IRepository<InventoryBarcode, Guid>
{
    /// <summary>
    /// Inserts barcodes in the current unit of work without EF change tracking.
    /// One import line can produce tens of thousands of barcodes (1 per storage unit),
    /// which is too much for InsertManyAsync to track in memory.
    /// Drop-in for InsertManyAsync(autoSave: true): pending changes are saved first and the rows
    /// are visible to later queries of the same unit of work. The inserted entities are not tracked.
    /// </summary>
    Task BulkInsertAsync(IReadOnlyCollection<InventoryBarcode> barcodes);
}
