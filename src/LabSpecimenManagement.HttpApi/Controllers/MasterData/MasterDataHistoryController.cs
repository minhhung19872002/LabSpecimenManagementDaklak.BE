using System;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.Histories;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/master-data-histories")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class MasterDataHistoryController : LabSpecimenManagementController
{
    private readonly IMasterDataHistoryAppService _masterDataHistoryAppService;

    public MasterDataHistoryController(IMasterDataHistoryAppService masterDataHistoryAppService)
    {
        _masterDataHistoryAppService = masterDataHistoryAppService;
    }

    /// <summary>
    /// Lịch sử thay đổi của 1 bản ghi master data
    /// </summary>
    [HttpGet("{entityType}/{entityId}")]
    public virtual Task<ListResultDto<MasterDataChangeDto>> GetListAsync(string entityType, Guid entityId)
    {
        return _masterDataHistoryAppService.GetListAsync(entityType, entityId);
    }
}
