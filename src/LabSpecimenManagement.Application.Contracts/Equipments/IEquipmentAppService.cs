using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Equipments;

public interface IEquipmentAppService : IApplicationService
{
    Task<PagedResultDto<EquipmentListDto>> GetListAsync(GetEquipmentListInput input);
    Task<EquipmentDto> GetAsync(Guid id);
    Task<EquipmentDto> CreateAsync(CreateUpdateEquipmentDto input);
    Task<EquipmentDto> UpdateAsync(Guid id, CreateUpdateEquipmentDto input);
    Task<EquipmentDto> UpdateStatusAsync(Guid id, UpdateEquipmentStatusDto input);
    Task DeleteAsync(Guid id);
    Task DeleteAttachmentAsync(Guid equipmentId, Guid attachmentId);
    Task<List<EquipmentCostConfigDto>> GetCostConfigListAsync(Guid equipmentId);
    Task<EquipmentCostConfigDto?> GetActiveCostConfigAsync(Guid equipmentId);
    Task<EquipmentCostConfigDto> SaveCostConfigAsync(Guid equipmentId, CreateEquipmentCostConfigDto input);
}
