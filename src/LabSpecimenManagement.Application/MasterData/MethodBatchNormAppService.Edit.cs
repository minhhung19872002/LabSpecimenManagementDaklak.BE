using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.BatchNorms;
using LabSpecimenManagement.Products;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Sửa định mức theo mẻ của 1 bộ (Nền mẫu × Chỉ tiêu × PP) + áp dụng cùng nội dung cho các bộ khác.
/// Bộ đích đã có định mức → ghi đè toàn bộ vật tư / thiết bị (không gộp).
/// Dòng vật tư bị bỏ mà đã phát sinh tiêu hao → chuyển ngừng (giữ lịch sử), chưa dùng → xóa.
/// </summary>
public partial class MethodBatchNormAppService
{
    public async Task<BatchNormDetailDto> GetDetailAsync(Guid testMethodIndicatorId)
    {
        var service = await _serviceRepository.FindAsync(testMethodIndicatorId)
                      ?? throw new UserFriendlyException("Bộ nền mẫu · chỉ tiêu · phương pháp không tồn tại hoặc đã bị xóa");
        var header = await _serviceNormRepository.FirstOrDefaultAsync(h => h.TestMethodIndicatorId == service.Id)
                     ?? throw new UserFriendlyException("Bộ này chưa có định mức");

        var lines = (await _normRepository.GetListAsync(l => l.ServiceBatchNormId == header.Id && l.IsActive && l.ProductId != null))
            .OrderBy(l => l.CreationTime)
            .ToList();
        var productIds = lines.Select(l => l.ProductId!.Value).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _productRepository.GetListAsync(p => productIds.Contains(p.Id))).ToDictionary(p => p.Id);
        var equipments = (await _equipmentRepository.GetListAsync(e => e.ServiceBatchNormId == header.Id))
            .OrderBy(e => e.CreationTime)
            .ToList();

        var combo = (await MapCombosAsync(new List<TestMethodIndicator> { service },
            new Dictionary<Guid, string> { [service.Id] = header.Code })).FirstOrDefault()
            ?? throw new UserFriendlyException("Chỉ tiêu hoặc phương pháp của bộ đã bị xóa");

        return new BatchNormDetailDto
        {
            Combo = combo,
            Materials = lines
                .Where(l => products.ContainsKey(l.ProductId!.Value))
                .Select(l =>
                {
                    var product = products[l.ProductId!.Value];
                    return new BulkCreateBatchNormMaterialInput
                    {
                        ProductId = product.Id,
                        Quantity = l.BatchQuantity ?? 0,
                        UnitId = l.UnitId ?? product.VolumeUnitId ?? product.BaseUnitId ?? Guid.Empty,
                    };
                })
                .ToList(),
            Equipments = equipments
                .Select(e => new BulkCreateBatchNormEquipmentInput
                {
                    ProductId = e.ProductId,
                    Capacity = e.Capacity,
                    CapacityUnit = e.CapacityUnit,
                })
                .ToList(),
        };
    }

    public async Task<ListResultDto<ServiceBatchNormDto>> UpdateNormAsync(Guid testMethodIndicatorId, UpdateBatchNormInput input)
    {
        var edited = await _serviceRepository.FindAsync(testMethodIndicatorId)
                     ?? throw new UserFriendlyException("Bộ nền mẫu · chỉ tiêu · phương pháp không tồn tại hoặc đã bị xóa");
        if (!await _serviceNormRepository.AnyAsync(h => h.TestMethodIndicatorId == edited.Id))
        {
            throw new UserFriendlyException("Bộ này chưa có định mức. Vui lòng tải lại.");
        }

        var applyIds = (input.ApplyToTestMethodIndicatorIds ?? new()).Distinct().Where(id => id != edited.Id).ToList();
        var services = new Dictionary<Guid, TestMethodIndicator> { [edited.Id] = edited };
        if (applyIds.Count > 0)
        {
            var applyServices = (await GetSelectableServicesAsync()).Where(s => applyIds.Contains(s.Id)).ToList();
            if (applyServices.Count != applyIds.Count)
            {
                throw new UserFriendlyException("Có bộ áp dụng không tồn tại, đã ngừng hoặc là thầu phụ. Vui lòng tải lại.");
            }

            foreach (var s in applyServices)
            {
                services[s.Id] = s;
            }
        }

        var materials = await ValidateMaterialsAsync(input.Materials);
        var equipments = await ValidateEquipmentsAsync(input.Equipments ?? new());

        var targetIds = services.Keys.ToList();
        var headers = (await _serviceNormRepository.GetListAsync(h => targetIds.Contains(h.TestMethodIndicatorId)))
            .ToDictionary(h => h.TestMethodIndicatorId);

        // Định mức chưa có → tạo mới
        var newHeaders = new List<ServiceBatchNorm>();
        var missing = targetIds.Where(id => !headers.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var nextCode = await CreateCodeGeneratorAsync();
            foreach (var id in missing)
            {
                var header = new ServiceBatchNorm(GuidGenerator.Create(), nextCode(), id);
                newHeaders.Add(header);
                headers[id] = header;
            }

            await _serviceNormRepository.InsertManyAsync(newHeaders);
        }

        var headerIds = headers.Values.Select(h => h.Id).ToList();
        var existingLines = (await _normRepository.GetListAsync(l => l.ServiceBatchNormId != null && headerIds.Contains(l.ServiceBatchNormId.Value)))
            .GroupBy(l => l.ServiceBatchNormId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var existingEquipments = (await _equipmentRepository.GetListAsync(e => headerIds.Contains(e.ServiceBatchNormId)))
            .GroupBy(e => e.ServiceBatchNormId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var lineIds = existingLines.Values.SelectMany(l => l).Select(l => l.Id).ToList();
        var usedLineIds = new HashSet<Guid>();
        if (lineIds.Count > 0)
        {
            var consumptionQuery = await _consumptionRepository.GetQueryableAsync();
            usedLineIds = (await AsyncExecuter.ToListAsync(consumptionQuery
                    .Where(c => c.MethodBatchNormId != null && lineIds.Contains(c.MethodBatchNormId.Value))
                    .Select(c => c.MethodBatchNormId!.Value)
                    .Distinct()))
                .ToHashSet();
        }

        var insertLines = new List<MethodBatchNorm>();
        var updateLines = new List<MethodBatchNorm>();
        var deleteLines = new List<MethodBatchNorm>();
        var insertEquipments = new List<ServiceBatchNormEquipment>();
        var updateEquipments = new List<ServiceBatchNormEquipment>();
        var deleteEquipments = new List<ServiceBatchNormEquipment>();

        foreach (var (serviceId, header) in headers)
        {
            var service = services[serviceId];

            // ── Vật tư: khớp theo vật tư (ưu tiên dòng đang dùng) để giữ Id dòng đã phát sinh tiêu hao
            var remaining = existingLines.GetValueOrDefault(header.Id) ?? new List<MethodBatchNorm>();
            foreach (var m in materials)
            {
                var line = remaining
                    .Where(l => l.ProductId == m.ProductId)
                    .OrderByDescending(l => l.IsActive)
                    .FirstOrDefault();
                if (line == null)
                {
                    insertLines.Add(new MethodBatchNorm(
                        GuidGenerator.Create(),
                        null!, // DB sinh mã DMxxxx (sequence)
                        header.Id,
                        service.TestMethodId,
                        m.ProductId,
                        m.UnitId,
                        m.Quantity));
                    continue;
                }

                remaining = remaining.Where(l => l.Id != line.Id).ToList();
                line.TestMethodId = service.TestMethodId;
                line.UnitId = m.UnitId;
                line.BatchQuantity = m.Quantity;
                line.IsActive = true;
                updateLines.Add(line);
            }

            foreach (var line in remaining)
            {
                if (usedLineIds.Contains(line.Id))
                {
                    if (line.IsActive)
                    {
                        line.IsActive = false;
                        updateLines.Add(line);
                    }
                }
                else
                {
                    deleteLines.Add(line);
                }
            }

            // ── Thiết bị: không tham gia trừ kho → thay thế theo thiết bị
            var remainingEquipments = existingEquipments.GetValueOrDefault(header.Id) ?? new List<ServiceBatchNormEquipment>();
            foreach (var e in equipments)
            {
                var row = remainingEquipments.FirstOrDefault(r => r.ProductId == e.ProductId);
                if (row == null)
                {
                    insertEquipments.Add(new ServiceBatchNormEquipment(
                        GuidGenerator.Create(), header.Id, e.ProductId, e.Capacity, e.CapacityUnit.Trim()));
                    continue;
                }

                remainingEquipments = remainingEquipments.Where(r => r.Id != row.Id).ToList();
                row.Capacity = e.Capacity;
                row.CapacityUnit = e.CapacityUnit.Trim();
                updateEquipments.Add(row);
            }

            deleteEquipments.AddRange(remainingEquipments);
        }

        if (deleteLines.Count > 0)
        {
            await _normRepository.DeleteManyAsync(deleteLines);
        }

        if (deleteEquipments.Count > 0)
        {
            await _equipmentRepository.DeleteManyAsync(deleteEquipments);
        }

        if (updateLines.Count > 0)
        {
            await _normRepository.UpdateManyAsync(updateLines);
        }

        if (updateEquipments.Count > 0)
        {
            await _equipmentRepository.UpdateManyAsync(updateEquipments);
        }

        if (insertLines.Count > 0)
        {
            await _normRepository.InsertManyAsync(insertLines);
        }

        if (insertEquipments.Count > 0)
        {
            await _equipmentRepository.InsertManyAsync(insertEquipments);
        }

        await SaveNormsAsync();
        return new ListResultDto<ServiceBatchNormDto>(await MapServiceNormsAsync(headers.Values.ToList()));
    }
}
