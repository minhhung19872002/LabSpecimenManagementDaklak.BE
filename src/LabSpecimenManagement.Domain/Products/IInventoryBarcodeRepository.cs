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

    /// <summary>
    /// Sets CurrentWarehouseId and LastMaterialReceiptId of the given barcodes with one UPDATE statement.
    /// Drop-in for changing the entities and calling UpdateManyAsync(autoSave: true), without loading
    /// or tracking them: pending changes are saved first, and barcodes already tracked by the unit of
    /// work get the new values.
    /// </summary>
    Task MoveToWarehouseAsync(IReadOnlyCollection<Guid> ids, Guid warehouseId, Guid lastMaterialReceiptId);

    /// <summary>
    /// Sets Status of the given barcodes with one UPDATE statement.
    /// Same drop-in guarantees as <see cref="MoveToWarehouseAsync"/>.
    /// </summary>
    Task UpdateStatusAsync(IReadOnlyCollection<Guid> ids, InventoryBarcodeStatus status);
}
