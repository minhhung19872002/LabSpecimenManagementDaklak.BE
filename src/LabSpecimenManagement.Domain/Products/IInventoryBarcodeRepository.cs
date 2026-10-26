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
    /// </summary>
    Task BulkInsertAsync(IReadOnlyCollection<InventoryBarcode> barcodes);
}
