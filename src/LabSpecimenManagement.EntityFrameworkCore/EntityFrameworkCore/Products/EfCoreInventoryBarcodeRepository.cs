using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using Microsoft.EntityFrameworkCore;
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

        var auditPropertySetter = LazyServiceProvider.LazyGetRequiredService<IAuditPropertySetter>();
        foreach (var barcode in barcodes)
            auditPropertySetter.SetCreationProperties(barcode);

        var dbContext = await GetDbContextAsync();
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

        var cancellationToken = GetCancellationToken();

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

    private static Task WriteNullableAsync(
        NpgsqlBinaryImporter importer, Guid? value, System.Threading.CancellationToken cancellationToken)
    {
        return value.HasValue
            ? importer.WriteAsync(value.Value, NpgsqlDbType.Uuid, cancellationToken)
            : importer.WriteNullAsync(cancellationToken);
    }
}
