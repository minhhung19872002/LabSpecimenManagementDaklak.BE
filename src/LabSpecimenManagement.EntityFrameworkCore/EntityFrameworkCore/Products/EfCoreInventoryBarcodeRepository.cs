using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using NpgsqlTypes;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace LabSpecimenManagement.EntityFrameworkCore.Products;

public class EfCoreInventoryBarcodeRepository
    : EfCoreRepository<LabSpecimenManagementDbContext, InventoryBarcode, Guid>,
      IInventoryBarcodeRepository
{
    public EfCoreInventoryBarcodeRepository(
        IDbContextProvider<LabSpecimenManagementDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task BulkInsertAsync(IReadOnlyCollection<InventoryBarcode> barcodes)
    {
        if (barcodes.Count == 0)
            return;

        var dbContext = await GetDbContextAsync();

        // COPY is PostgreSQL only (unit tests run on SQLite).
        if (dbContext.Database.GetDbConnection() is not NpgsqlConnection)
        {
            await InsertManyAsync(barcodes, autoSave: true);
            return;
        }

        var auditPropertySetter = LazyServiceProvider.LazyGetRequiredService<IAuditPropertySetter>();
        foreach (var barcode in barcodes)
        {
            auditPropertySetter.SetCreationProperties(barcode);

            // Same as an EF insert: Status has a database default (NotUsed), so EF omits the
            // CLR default value (InUse = 0) from the INSERT and the row gets NotUsed.
            if (barcode.Status == default)
                barcode.Status = InventoryBarcodeStatus.NotUsed;
        }

        var cancellationToken = GetCancellationToken();

        // Same as InsertAsync(autoSave: true): pending changes of the unit of work are saved too.
        if (dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesAsync(cancellationToken);

        var entityType = dbContext.Model.FindEntityType(typeof(InventoryBarcode))!;
        var table = entityType.GetSchema() is { } schema
            ? $"\"{schema}\".\"{entityType.GetTableName()}\""
            : $"\"{entityType.GetTableName()}\"";
        string Column(string propertyName) => $"\"{entityType.FindProperty(propertyName)!.GetColumnName()}\"";

        var copyCommand =
            $"COPY {table} (" +
            $"{Column(nameof(InventoryBarcode.Id))}, " +
            $"{Column(nameof(InventoryBarcode.Code))}, " +
            $"{Column(nameof(InventoryBarcode.ProductId))}, " +
            $"{Column(nameof(InventoryBarcode.SkuId))}, " +
            $"{Column(nameof(InventoryBarcode.OriginWarehouseImportReceiptItemId))}, " +
            $"{Column(nameof(InventoryBarcode.CurrentWarehouseId))}, " +
            $"{Column(nameof(InventoryBarcode.LastMaterialReceiptId))}, " +
            $"{Column(nameof(InventoryBarcode.Status))}, " +
            $"{Column(nameof(InventoryBarcode.CreationTime))}, " +
            $"{Column(nameof(InventoryBarcode.CreatorId))}" +
            ") FROM STDIN (FORMAT BINARY)";

        // COPY runs on the unit of work's connection, so it joins the UoW transaction.
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
            await using var importer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken);
            foreach (var barcode in barcodes)
            {
                await importer.StartRowAsync(cancellationToken);
                await importer.WriteAsync(barcode.Id, NpgsqlDbType.Uuid, cancellationToken);
                await importer.WriteAsync(barcode.Code, NpgsqlDbType.Varchar, cancellationToken);
                await importer.WriteAsync(barcode.ProductId, NpgsqlDbType.Uuid, cancellationToken);
                await WriteNullableAsync(importer, barcode.SkuId, cancellationToken);
                await importer.WriteAsync(barcode.OriginWarehouseImportReceiptItemId, NpgsqlDbType.Uuid, cancellationToken);
                await importer.WriteAsync(barcode.CurrentWarehouseId, NpgsqlDbType.Uuid, cancellationToken);
                await WriteNullableAsync(importer, barcode.LastMaterialReceiptId, cancellationToken);
                await importer.WriteAsync((int)barcode.Status, NpgsqlDbType.Integer, cancellationToken);
                await importer.WriteAsync(barcode.CreationTime, NpgsqlDbType.Timestamp, cancellationToken);
                await WriteNullableAsync(importer, barcode.CreatorId, cancellationToken);
            }
            await importer.CompleteAsync(cancellationToken);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    public async Task MoveToWarehouseAsync(
        IReadOnlyCollection<Guid> ids, Guid warehouseId, Guid lastMaterialReceiptId)
    {
        if (ids.Count == 0)
            return;

        var idArray = await SavePendingChangesAsync(ids);
        await (await GetDbSetAsync())
            .Where(x => idArray.Contains(x.Id))
            .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CurrentWarehouseId, warehouseId)
                    .SetProperty(x => x.LastMaterialReceiptId, lastMaterialReceiptId),
                GetCancellationToken());

        await SyncTrackedAsync(idArray, entry =>
        {
            entry.Entity.CurrentWarehouseId = warehouseId;
            entry.Entity.LastMaterialReceiptId = lastMaterialReceiptId;
            MarkSaved(entry.Property(x => x.CurrentWarehouseId), warehouseId);
            MarkSaved(entry.Property(x => x.LastMaterialReceiptId), lastMaterialReceiptId);
        });
    }

    public async Task UpdateStatusAsync(IReadOnlyCollection<Guid> ids, InventoryBarcodeStatus status)
    {
        if (ids.Count == 0)
            return;

        var idArray = await SavePendingChangesAsync(ids);
        await (await GetDbSetAsync())
            .Where(x => idArray.Contains(x.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status), GetCancellationToken());

        await SyncTrackedAsync(idArray, entry =>
        {
            entry.Entity.Status = status;
            MarkSaved(entry.Property(x => x.Status), status);
        });
    }

    public async Task<Dictionary<Guid, (InventoryBarcodeStatus Status, string Code)>> GetActiveCodeByReceiptItemAsync(
        IReadOnlyCollection<Guid> receiptItemIds)
    {
        var result = new Dictionary<Guid, (InventoryBarcodeStatus Status, string Code)>();
        if (receiptItemIds.Count == 0)
            return result;

        var idArray = receiptItemIds as Guid[] ?? receiptItemIds.ToArray();
        var rows = await (await GetDbSetAsync())
            .Where(x => idArray.Contains(x.OriginWarehouseImportReceiptItemId)
                        && (x.Status == InventoryBarcodeStatus.InUse || x.Status == InventoryBarcodeStatus.NotUsed))
            .GroupBy(x => new { x.OriginWarehouseImportReceiptItemId, x.Status })
            .Select(g => new { g.Key.OriginWarehouseImportReceiptItemId, g.Key.Status, Code = g.Min(x => x.Code) })
            .ToListAsync(GetCancellationToken());

        foreach (var row in rows)
        {
            if (!result.ContainsKey(row.OriginWarehouseImportReceiptItemId) || row.Status == InventoryBarcodeStatus.InUse)
                result[row.OriginWarehouseImportReceiptItemId] = (row.Status, row.Code);
        }

        return result;
    }

    private static void MarkSaved<TProperty>(PropertyEntry<InventoryBarcode, TProperty> property, TProperty value)
    {
        property.OriginalValue = value;
        property.IsModified = false;
    }

    // Same as UpdateManyAsync(autoSave: true): pending changes of the unit of work are saved too.
    private async Task<Guid[]> SavePendingChangesAsync(IReadOnlyCollection<Guid> ids)
    {
        var dbContext = await GetDbContextAsync();
        if (dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesAsync(GetCancellationToken());
        return ids as Guid[] ?? ids.ToArray();
    }

    // ExecuteUpdate bypasses the change tracker. Barcodes the unit of work already tracks get the
    // new values as saved values (state stays Unchanged), as after UpdateManyAsync(autoSave: true),
    // so a later save cannot write the old values back.
    private async Task SyncTrackedAsync(Guid[] ids, Action<EntityEntry<InventoryBarcode>> apply)
    {
        var dbContext = await GetDbContextAsync();
        HashSet<Guid>? idSet = null;
        foreach (var entry in dbContext.ChangeTracker.Entries<InventoryBarcode>())
        {
            idSet ??= ids.ToHashSet();
            if (idSet.Contains(entry.Entity.Id))
                apply(entry);
        }
    }

    private static Task WriteNullableAsync(
        NpgsqlBinaryImporter importer, Guid? value, System.Threading.CancellationToken cancellationToken)
    {
        return value.HasValue
            ? importer.WriteAsync(value.Value, NpgsqlDbType.Uuid, cancellationToken)
            : importer.WriteNullAsync(cancellationToken);
    }
}
