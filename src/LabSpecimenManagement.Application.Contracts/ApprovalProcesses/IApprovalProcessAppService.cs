using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.ApprovalProcesses;

public interface IApprovalProcessAppService : IApplicationService
{
    Task<PagedResultDto<ApprovalProcessDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<ApprovalProcessDto> GetAsync(Guid id);
    Task<ApprovalProcessDto> CreateAsync(CreateUpdateApprovalProcessDto input);
    Task<ApprovalProcessDto> UpdateAsync(Guid id, CreateUpdateApprovalProcessDto input);
    Task DeleteAsync(Guid id);
    Task<ApprovalProcessDto?> GetByProcessTypeAsync(ApprovalProcessType processType);
    Task<ApprovalSnapshotDto?> GetSnapshotAsync(Guid referenceId);
    Task ToggleActiveAsync(Guid id);
}
