using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Equipments;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.Equipments.Default)]
public class EquipmentAppService : ApplicationService, IEquipmentAppService
{
    private readonly IRepository<Equipment, Guid> _repository;
    private readonly IRepository<EquipmentAttachment, Guid> _attachmentRepository;
    private readonly IRepository<EquipmentCostConfig, Guid> _costConfigRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IUserNameProvider _userNameProvider;

    public EquipmentAppService(
        IRepository<Equipment, Guid> repository,
        IRepository<EquipmentAttachment, Guid> attachmentRepository,
        IRepository<EquipmentCostConfig, Guid> costConfigRepository,
        IRepository<Unit, Guid> unitRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IUserNameProvider userNameProvider)
    {
        _repository = repository;
        _attachmentRepository = attachmentRepository;
        _costConfigRepository = costConfigRepository;
        _unitRepository = unitRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        _userNameProvider = userNameProvider;
    }

    public async Task<PagedResultDto<EquipmentListDto>> GetListAsync(GetEquipmentListInput input)
    {
        var query = await _repository.GetQueryableAsync();

        if (input.Status.HasValue)
            query = query.Where(x => x.Status == input.Status.Value);

        if (input.SupplierId.HasValue)
            query = query.Where(x => x.SupplierId == input.SupplierId.Value);

        if (input.FromDate.HasValue)
            query = query.Where(x => x.CreationTime >= input.FromDate.Value);

        if (input.ToDate.HasValue)
            query = query.Where(x => x.CreationTime <= input.ToDate.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                x.SerialNumber.ToLower().Contains(search));
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var itemIds = items.Select(x => x.Id).ToList();
        var attachmentQuery = await _attachmentRepository.GetQueryableAsync();
        var attachmentsByEquipment = attachmentQuery
            .Where(a => itemIds.Contains(a.EquipmentId))
            .OrderByDescending(a => a.CreationTime)
            .ToList()
            .GroupBy(a => a.EquipmentId)
            .ToDictionary(g => g.Key, g => g.Select(a => new EquipmentAttachmentDto
            {
                Id = a.Id,
                FileId = a.FileId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                FileType = a.FileType,
                Url = $"/api/v1/files/{a.FileId}",
                CreationTime = a.CreationTime,
            }).ToList());

        var dtos = items.Select(x => new EquipmentListDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            SerialNumber = x.SerialNumber,
            Status = x.Status,
            Model = x.Model,
            Manufacturer = x.Manufacturer,
            Origin = x.Origin,
            SupplierName = x.SupplierName,
            GoodsReceiptCode = x.GoodsReceiptCode,
            WarrantyExpiryDate = x.WarrantyExpiryDate,
            UsageCount = x.UsageCount,
            MaintenanceCount = x.MaintenanceCount,
            AssetCode = x.AssetCode,
            Price = x.Price,
            DepreciationPeriod = x.DepreciationPeriod,
            PowerConsumption = x.PowerConsumption,
            OperatingHoursPerYear = x.OperatingHoursPerYear,
            AverageSamplesPerYear = x.AverageSamplesPerYear,
            MaintenanceCostRatio = x.MaintenanceCostRatio,
            CalibrationCostPerYear = x.CalibrationCostPerYear,
            ImportDate = x.ImportDate,
            CreationTime = x.CreationTime,
            Attachments = attachmentsByEquipment.GetValueOrDefault(x.Id, new()),
        }).ToList();

        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => items.First(x => x.Id == dto.Id).CreatorId,
            (dto, name) => dto.CreatorName = name);

        return new PagedResultDto<EquipmentListDto>(totalCount, dtos);
    }

    public async Task<EquipmentDto> GetAsync(Guid id)
    {
        var equipment = await _repository.GetAsync(id);
        return await MapToDetailDtoAsync(equipment);
    }

    [Authorize(LabSpecimenManagementPermissions.Equipments.Create)]
    public async Task<EquipmentDto> CreateAsync(CreateUpdateEquipmentDto input)
    {
        await ValidateSerialNumberAsync(input.SerialNumber);

        var code = await GenerateCodeAsync();
        var equipment = new Equipment(GuidGenerator.Create(), code, input.Name, input.SerialNumber)
        {
            Model = input.Model,
            Manufacturer = input.Manufacturer,
            Origin = input.Origin,
            WarrantyExpiryDate = input.WarrantyExpiryDate,
            Price = input.Price,
            Notes = input.Notes,
            AssetCode = input.AssetCode,
            DepreciationPeriod = input.DepreciationPeriod,
            Location = input.Location,
            PowerConsumption = input.PowerConsumption,
            OperatingHoursPerYear = input.OperatingHoursPerYear,
            AverageSamplesPerYear = input.AverageSamplesPerYear,
            MaintenanceCostRatio = input.MaintenanceCostRatio,
            CalibrationCostPerYear = input.CalibrationCostPerYear,
        };

        if (input.UnitId.HasValue)
        {
            var unit = await _unitRepository.FindAsync(input.UnitId.Value);
            if (unit != null)
            {
                equipment.UnitId = unit.Id;
                equipment.UnitName = unit.Name;
            }
        }

        await _repository.InsertAsync(equipment, autoSave: true);

        if (input.Attachments != null)
        {
            foreach (var att in input.Attachments)
            {
                await _attachmentRepository.InsertAsync(new EquipmentAttachment(
                    GuidGenerator.Create(), equipment.Id, att.FileId, att.FileName,
                    $"uploads/samples/{att.FileId}", att.FileSize, att.FileType), autoSave: true);
            }
        }

        return await GetAsync(equipment.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.Equipments.Edit)]
    public async Task<EquipmentDto> UpdateAsync(Guid id, CreateUpdateEquipmentDto input)
    {
        var equipment = await _repository.GetAsync(id);

        if (equipment.SerialNumber != input.SerialNumber)
            await ValidateSerialNumberAsync(input.SerialNumber, id);

        equipment.Name = input.Name;
        equipment.SerialNumber = input.SerialNumber;
        equipment.Model = input.Model;
        equipment.Manufacturer = input.Manufacturer;
        equipment.Origin = input.Origin;
        equipment.WarrantyExpiryDate = input.WarrantyExpiryDate;
        equipment.Price = input.Price;
        equipment.Notes = input.Notes;
        equipment.AssetCode = input.AssetCode;
        equipment.DepreciationPeriod = input.DepreciationPeriod;
        equipment.Location = input.Location;
        equipment.PowerConsumption = input.PowerConsumption;
        equipment.OperatingHoursPerYear = input.OperatingHoursPerYear;
        equipment.AverageSamplesPerYear = input.AverageSamplesPerYear;
        equipment.MaintenanceCostRatio = input.MaintenanceCostRatio;
        equipment.CalibrationCostPerYear = input.CalibrationCostPerYear;

        if (input.UnitId.HasValue)
        {
            var unit = await _unitRepository.FindAsync(input.UnitId.Value);
            if (unit != null)
            {
                equipment.UnitId = unit.Id;
                equipment.UnitName = unit.Name;
            }
        }
        else
        {
            equipment.UnitId = null;
            equipment.UnitName = null;
        }

        await _repository.UpdateAsync(equipment, autoSave: true);

        if (input.Attachments != null)
        {
            var existingAttachments = (await _attachmentRepository.GetQueryableAsync())
                .Where(x => x.EquipmentId == id)
                .Select(x => x.FileId)
                .ToList();

            foreach (var att in input.Attachments)
            {
                if (!existingAttachments.Contains(att.FileId))
                {
                    await _attachmentRepository.InsertAsync(new EquipmentAttachment(
                        GuidGenerator.Create(), id, att.FileId, att.FileName,
                        $"uploads/samples/{att.FileId}", att.FileSize, att.FileType), autoSave: true);
                }
            }
        }

        return await GetAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.Equipments.UpdateStatus)]
    public async Task<EquipmentDto> UpdateStatusAsync(Guid id, UpdateEquipmentStatusDto input)
    {
        var equipment = await _repository.GetAsync(id);
        ValidateStatusTransition(equipment.Status, input.Status);

        equipment.Status = input.Status;
        if (!string.IsNullOrWhiteSpace(input.Notes))
            equipment.Notes = input.Notes;

        await _repository.UpdateAsync(equipment, autoSave: true);
        return await GetAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.Equipments.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var equipment = await _repository.GetAsync(id);

        if (equipment.Status != EquipmentStatus.InStock)
            throw new UserFriendlyException("Chỉ có thể xóa thiết bị ở trạng thái Trong kho.");

        await _repository.DeleteAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.Equipments.Edit)]
    public async Task DeleteAttachmentAsync(Guid equipmentId, Guid attachmentId)
    {
        var attachment = await _attachmentRepository.GetAsync(attachmentId);
        if (attachment.EquipmentId != equipmentId)
            throw new UserFriendlyException("Tài liệu không thuộc thiết bị này.");
        await _attachmentRepository.DeleteAsync(attachmentId);
    }

    #region Private Methods

    private async Task<string> GenerateCodeAsync()
    {
        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix("TB");
            var query = await _repository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate("TB", count);
        }
    }

    private async Task ValidateSerialNumberAsync(string serialNumber, Guid? excludeId = null)
    {
        var query = await _repository.GetQueryableAsync();
        var exists = query.Any(x => x.SerialNumber == serialNumber && (!excludeId.HasValue || x.Id != excludeId.Value));

        if (exists)
            throw new UserFriendlyException($"Serial number \"{serialNumber}\" đã tồn tại.");
    }

    private static void ValidateStatusTransition(EquipmentStatus current, EquipmentStatus next)
    {
        var allowed = current switch
        {
            EquipmentStatus.InStock => new[] { EquipmentStatus.InUse, EquipmentStatus.UnderMaintenance, EquipmentStatus.Broken },
            EquipmentStatus.InUse => new[] { EquipmentStatus.InStock, EquipmentStatus.UnderMaintenance, EquipmentStatus.Broken },
            EquipmentStatus.UnderMaintenance => new[] { EquipmentStatus.InStock, EquipmentStatus.Broken },
            EquipmentStatus.Broken => new[] { EquipmentStatus.Disposed },
            _ => Array.Empty<EquipmentStatus>(),
        };

        if (!allowed.Contains(next))
            throw new UserFriendlyException($"Không thể chuyển trạng thái từ \"{GetStatusLabel(current)}\" sang \"{GetStatusLabel(next)}\".");
    }

    private static string GetStatusLabel(EquipmentStatus status) => status switch
    {
        EquipmentStatus.InStock => "Trong kho",
        EquipmentStatus.InUse => "Đang sử dụng",
        EquipmentStatus.UnderMaintenance => "Đang bảo trì",
        EquipmentStatus.Broken => "Hỏng",
        EquipmentStatus.Disposed => "Thanh lý",
        _ => status.ToString(),
    };

    private async Task<EquipmentDto> MapToDetailDtoAsync(Equipment equipment)
    {
        var attachments = (await _attachmentRepository.GetQueryableAsync())
            .Where(x => x.EquipmentId == equipment.Id)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        var dto = new EquipmentDto
        {
            Id = equipment.Id,
            Code = equipment.Code,
            Name = equipment.Name,
            SerialNumber = equipment.SerialNumber,
            Status = equipment.Status,
            GoodsReceiptId = equipment.GoodsReceiptId,
            GoodsReceiptCode = equipment.GoodsReceiptCode,
            SupplierId = equipment.SupplierId,
            SupplierName = equipment.SupplierName,
            Model = equipment.Model,
            Manufacturer = equipment.Manufacturer,
            Origin = equipment.Origin,
            UnitId = equipment.UnitId,
            UnitName = equipment.UnitName,
            Price = equipment.Price,
            LotNumber = equipment.LotNumber,
            LotExpiryDate = equipment.LotExpiryDate,
            WarrantyExpiryDate = equipment.WarrantyExpiryDate,
            UsageCount = equipment.UsageCount,
            MaintenanceCount = equipment.MaintenanceCount,
            Notes = equipment.Notes,
            AssetCode = equipment.AssetCode,
            DepreciationPeriod = equipment.DepreciationPeriod,
            Location = equipment.Location,
            PowerConsumption = equipment.PowerConsumption,
            OperatingHoursPerYear = equipment.OperatingHoursPerYear,
            AverageSamplesPerYear = equipment.AverageSamplesPerYear,
            MaintenanceCostRatio = equipment.MaintenanceCostRatio,
            CalibrationCostPerYear = equipment.CalibrationCostPerYear,
            ImportDate = equipment.ImportDate,
            CreationTime = equipment.CreationTime,
            Attachments = attachments.Select(a => new EquipmentAttachmentDto
            {
                Id = a.Id,
                FileId = a.FileId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                FileType = a.FileType,
                Url = $"/api/v1/files/{a.FileId}",
                CreationTime = a.CreationTime,
            }).ToList(),
        };

        await _userNameProvider.EnrichWithUserNamesAsync(
            new List<EquipmentDto> { dto },
            _ => equipment.CreatorId,
            (d, name) => d.CreatorName = name);

        return dto;
    }

    #endregion

    public async Task<List<EquipmentCostConfigDto>> GetCostConfigListAsync(Guid equipmentId)
    {
        var query = await _costConfigRepository.GetQueryableAsync();
        var configs = query
            .Where(x => x.EquipmentId == equipmentId)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        var dtos = configs.Select(c => new EquipmentCostConfigDto
        {
            Id = c.Id,
            EquipmentId = c.EquipmentId,
            EffectiveFrom = c.EffectiveFrom,
            EffectiveTo = c.EffectiveTo,
            OperatingHoursPerYear = c.OperatingHoursPerYear,
            AverageSamplesPerYear = c.AverageSamplesPerYear,
            MaintenanceCostRatio = c.MaintenanceCostRatio,
            CalibrationCostPerYear = c.CalibrationCostPerYear,
            Description = c.Description,
            ChangeLog = c.ChangeLog,
            CreationTime = c.CreationTime,
        }).ToList();

        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => configs.First(c => c.Id == dto.Id).CreatorId,
            (dto, name) => dto.CreatorName = name);

        return dtos;
    }

    public async Task<EquipmentCostConfigDto?> GetActiveCostConfigAsync(Guid equipmentId)
    {
        var query = await _costConfigRepository.GetQueryableAsync();
        var config = query
            .Where(x => x.EquipmentId == equipmentId && x.EffectiveTo >= DateTime.Today)
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (config == null) return null;

        var dto = new EquipmentCostConfigDto
        {
            Id = config.Id,
            EquipmentId = config.EquipmentId,
            EffectiveFrom = config.EffectiveFrom,
            EffectiveTo = config.EffectiveTo,
            OperatingHoursPerYear = config.OperatingHoursPerYear,
            AverageSamplesPerYear = config.AverageSamplesPerYear,
            MaintenanceCostRatio = config.MaintenanceCostRatio,
            CalibrationCostPerYear = config.CalibrationCostPerYear,
            Description = config.Description,
            ChangeLog = config.ChangeLog,
            CreationTime = config.CreationTime,
        };

        await _userNameProvider.EnrichWithUserNamesAsync(
            new List<EquipmentCostConfigDto> { dto },
            _ => config.CreatorId,
            (d, name) => d.CreatorName = name);

        return dto;
    }

    [Authorize(LabSpecimenManagementPermissions.Equipments.Edit)]
    public async Task<EquipmentCostConfigDto> SaveCostConfigAsync(Guid equipmentId, CreateEquipmentCostConfigDto input)
    {
        var equipment = await _repository.GetAsync(equipmentId);

        if (input.EffectiveFrom.Date >= input.EffectiveTo.Date)
            throw new UserFriendlyException("Ngày bắt đầu hiệu lực phải nhỏ hơn ngày kết thúc.");

        if (input.EffectiveTo.Date <= DateTime.Today)
            throw new UserFriendlyException("Ngày kết thúc hiệu lực phải lớn hơn ngày hiện tại.");

        var query = await _costConfigRepository.GetQueryableAsync();
        var previousActive = query
            .Where(x => x.EquipmentId == equipmentId)
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        var description = "Tạo thiết lập mới";
        if (previousActive != null && previousActive.EffectiveFrom.Date == input.EffectiveFrom.Date)
        {
            description = "Chỉnh sửa thiết lập";
        }

        var changeLog = BuildChangeLog(previousActive, input);

        var config = new EquipmentCostConfig(
            GuidGenerator.Create(),
            equipmentId,
            input.EffectiveFrom,
            input.EffectiveTo)
        {
            OperatingHoursPerYear = input.OperatingHoursPerYear,
            AverageSamplesPerYear = input.AverageSamplesPerYear,
            MaintenanceCostRatio = input.MaintenanceCostRatio,
            CalibrationCostPerYear = input.CalibrationCostPerYear,
            Description = description,
            ChangeLog = changeLog,
        };

        await _costConfigRepository.InsertAsync(config, autoSave: true);

        equipment.OperatingHoursPerYear = input.OperatingHoursPerYear;
        equipment.AverageSamplesPerYear = input.AverageSamplesPerYear;
        equipment.MaintenanceCostRatio = input.MaintenanceCostRatio;
        equipment.CalibrationCostPerYear = input.CalibrationCostPerYear;
        await _repository.UpdateAsync(equipment, autoSave: true);

        var dto = new EquipmentCostConfigDto
        {
            Id = config.Id,
            EquipmentId = config.EquipmentId,
            EffectiveFrom = config.EffectiveFrom,
            EffectiveTo = config.EffectiveTo,
            OperatingHoursPerYear = config.OperatingHoursPerYear,
            AverageSamplesPerYear = config.AverageSamplesPerYear,
            MaintenanceCostRatio = config.MaintenanceCostRatio,
            CalibrationCostPerYear = config.CalibrationCostPerYear,
            Description = config.Description,
            ChangeLog = config.ChangeLog,
            CreationTime = config.CreationTime,
            CreatorName = CurrentUser.Name ?? CurrentUser.UserName,
        };

        return dto;
    }

    private static string? BuildChangeLog(EquipmentCostConfig? previous, CreateEquipmentCostConfigDto next)
    {
        if (previous == null) return null;

        var changes = new List<object>();

        void CheckDate(string label, DateTime oldVal, DateTime newVal)
        {
            if (oldVal.Date != newVal.Date)
                changes.Add(new { field = label, from = oldVal.ToString("dd/MM/yyyy"), to = newVal.ToString("dd/MM/yyyy") });
        }

        void CheckDecimal(string label, decimal? oldVal, decimal? newVal)
        {
            if (oldVal != newVal)
                changes.Add(new { field = label, from = oldVal?.ToString() ?? "", to = newVal?.ToString() ?? "" });
        }

        void CheckInt(string label, int? oldVal, int? newVal)
        {
            if (oldVal != newVal)
                changes.Add(new { field = label, from = oldVal?.ToString() ?? "", to = newVal?.ToString() ?? "" });
        }

        CheckDate("Hiệu lực từ", previous.EffectiveFrom, next.EffectiveFrom);
        CheckDate("Hiệu lực đến", previous.EffectiveTo, next.EffectiveTo);
        CheckDecimal("Thời gian chạy máy (h/năm)", previous.OperatingHoursPerYear, next.OperatingHoursPerYear);
        CheckInt("Số mẫu trung bình (mẫu/năm)", previous.AverageSamplesPerYear, next.AverageSamplesPerYear);
        CheckDecimal("Tỷ lệ duy tu bảo dưỡng (%)", previous.MaintenanceCostRatio, next.MaintenanceCostRatio);
        CheckDecimal("Chi phí hiệu chuẩn (đ/năm)", previous.CalibrationCostPerYear, next.CalibrationCostPerYear);

        return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
    }
}
