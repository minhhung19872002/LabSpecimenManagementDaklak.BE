using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.Histories;

public interface IMasterDataHistoryAppService : IApplicationService
{
    /// <summary>
    /// Lịch sử thay đổi của 1 bản ghi master data (mới nhất trước)
    /// </summary>
    /// <param name="entityType">Category key bên FE: indicators, testMethods, standards...</param>
    /// <param name="entityId">Id bản ghi</param>
    Task<ListResultDto<MasterDataChangeDto>> GetListAsync(string entityType, Guid entityId);
}
