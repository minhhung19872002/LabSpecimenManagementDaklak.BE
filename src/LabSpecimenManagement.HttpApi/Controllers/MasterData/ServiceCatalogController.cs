using LabSpecimenManagement.MasterData.BatchNorms;
using LabSpecimenManagement.MasterData.ServiceCatalog;
using LabSpecimenManagement.MasterData.YearlyForecasts;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

/// <summary>L2 — Dịch vụ & Bảng giá</summary>
[RemoteService(Name = "Default")]
[Route("api/v1/service-catalog")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class ServiceCatalogController : LabSpecimenManagementController
{
    private readonly IServiceCatalogAppService _appService;

    public ServiceCatalogController(IServiceCatalogAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<ListResultDto<ServiceCatalogItemDto>> GetListAsync([FromQuery] GetServiceCatalogInput input)
        => _appService.GetListAsync(input);

    [HttpPost("bulk-create")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<BulkCreateServicesResultDto> BulkCreateAsync([FromBody] BulkCreateServicesInput input)
        => _appService.BulkCreateAsync(input);

    [HttpGet("service-groups")]
    public virtual Task<ListResultDto<ServiceGroupDto>> GetServiceGroupsAsync()
        => _appService.GetServiceGroupsAsync();

    [HttpPost("service-groups")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<ServiceGroupDto> CreateServiceGroupAsync([FromBody] CreateServiceGroupInput input)
        => _appService.CreateServiceGroupAsync(input);

    [HttpPut("service-groups/{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<ServiceGroupDto> UpdateServiceGroupAsync(Guid id, [FromBody] CreateServiceGroupInput input)
        => _appService.UpdateServiceGroupAsync(id, input);

    [HttpDelete("service-groups/{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task<int> DeleteServiceGroupAsync(Guid id)
        => _appService.DeleteServiceGroupAsync(id);

    [HttpPost("assign")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<int> AssignServicesAsync([FromBody] AssignServicesInput input)
        => _appService.AssignServicesAsync(input);

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<ServiceCatalogItemDto> UpdateAsync(Guid id, [FromBody] UpdateServiceInput input)
        => _appService.UpdateAsync(id, input);

    [HttpGet("{id}/price-versions")]
    public virtual Task<ListResultDto<ServicePriceVersionDto>> GetPriceVersionsAsync(Guid id)
        => _appService.GetPriceVersionsAsync(id);

    [HttpPost("{id}/prices")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<ServiceCatalogItemDto> SetPriceAsync(Guid id, [FromBody] SetServicePriceInput input)
        => _appService.SetPriceAsync(id, input);

    [HttpDelete("price-versions/{versionId}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task DeletePriceVersionAsync(Guid versionId)
        => _appService.DeletePriceVersionAsync(versionId);

    [HttpPost("bulk-price")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<BulkUpdatePriceResultDto> BulkUpdatePriceAsync([FromBody] BulkUpdatePriceInput input)
        => _appService.BulkUpdatePriceAsync(input);

    [HttpPost("import-prices")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Import)]
    public virtual Task<ImportServicePricesResultDto> ImportPricesAsync([FromBody] ImportServicePricesInput input)
        => _appService.ImportPricesAsync(input);

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
        => _appService.ToggleActiveAsync(id);

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
        => _appService.DeleteAsync(id);

    [HttpPost("delete-many")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task<int> DeleteManyAsync([FromBody] List<Guid> ids)
        => _appService.DeleteManyAsync(ids);
}

/// <summary>L3 — Định mức tiêu hao theo mẻ</summary>
[RemoteService(Name = "Default")]
[Route("api/v1/batch-norms")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class MethodBatchNormController : LabSpecimenManagementController
{
    private readonly IMethodBatchNormAppService _appService;

    public MethodBatchNormController(IMethodBatchNormAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("overview")]
    public virtual Task<BatchNormOverviewDto> GetOverviewAsync()
        => _appService.GetOverviewAsync();

    [HttpPost("services")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<ListResultDto<ServiceBatchNormDto>> AddServicesAsync([FromBody] AddServiceBatchNormsInput input)
        => _appService.AddServicesAsync(input);

    [HttpPost("services/{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<ServiceBatchNormDto> ToggleServiceActiveAsync(Guid id)
        => _appService.ToggleServiceActiveAsync(id);

    [HttpDelete("services/{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteServiceAsync(Guid id)
        => _appService.DeleteServiceAsync(id);

    [HttpPost("services/{id}/restore")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task<ServiceBatchNormDto> RestoreServiceAsync(Guid id)
        => _appService.RestoreServiceAsync(id);

    [HttpGet("by-service/{testMethodIndicatorId}")]
    public virtual Task<ListResultDto<MethodBatchNormDto>> GetByServiceAsync(Guid testMethodIndicatorId)
        => _appService.GetByServiceAsync(testMethodIndicatorId);

    [HttpGet("by-service/{testMethodIndicatorId}/equipments")]
    public virtual Task<ListResultDto<ServiceBatchNormEquipmentDto>> GetEquipmentsByServiceAsync(Guid testMethodIndicatorId)
        => _appService.GetEquipmentsByServiceAsync(testMethodIndicatorId);

    /// <summary>Dữ liệu cho Phiếu khai báo định mức theo mẻ</summary>
    [HttpGet("create-options")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<BatchNormCreateOptionsDto> GetCreateOptionsAsync([FromQuery] GetBatchNormCreateOptionsInput input)
        => _appService.GetCreateOptionsAsync(input);

    /// <summary>Tạo nhiều định mức cùng lúc (dùng chung vật tư + thiết bị)</summary>
    [HttpPost("bulk-create")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<ListResultDto<ServiceBatchNormDto>> BulkCreateAsync([FromBody] BulkCreateBatchNormsInput input)
        => _appService.BulkCreateAsync(input);

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<MethodBatchNormDto> CreateAsync([FromBody] CreateUpdateMethodBatchNormInput input)
        => _appService.CreateAsync(input);

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<MethodBatchNormDto> UpdateAsync(Guid id, [FromBody] CreateUpdateMethodBatchNormInput input)
        => _appService.UpdateAsync(id, input);

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<MethodBatchNormDto> ToggleActiveAsync(Guid id)
        => _appService.ToggleActiveAsync(id);

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
        => _appService.DeleteAsync(id);
}

/// <summary>L4 — Dự toán số mẫu năm</summary>
[RemoteService(Name = "Default")]
[Route("api/v1/yearly-forecasts")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class YearlyForecastController : LabSpecimenManagementController
{
    private readonly IYearlyForecastAppService _appService;

    public YearlyForecastController(IYearlyForecastAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<YearlyForecastListDto> GetListAsync([FromQuery] GetYearlyForecastsInput input)
        => _appService.GetListAsync(input);

    [HttpPut]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<int> SaveAsync([FromBody] SaveYearlyForecastsInput input)
        => _appService.SaveAsync(input);

    [HttpPost("clone")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<CloneYearlyForecastResultDto> CloneFromPreviousYearAsync([FromBody] CloneYearlyForecastInput input)
        => _appService.CloneFromPreviousYearAsync(input);
}
