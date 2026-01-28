using LabSpecimenManagement.Sync;
using LabSpecimenManagement.Sync.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace LabSpecimenManagement;

[DependsOn(
    typeof(LabSpecimenManagementDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(LabSpecimenManagementApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class LabSpecimenManagementApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<LabSpecimenManagementApplicationModule>();
        });

        // Register sync handlers for offline-first functionality
        context.Services.AddTransient<ISyncEntityHandler, TestRequestSyncHandler>();
        context.Services.AddTransient<ISyncEntityHandler, QuotationSyncHandler>();
        context.Services.AddTransient<ISyncEntityHandler, NCRSyncHandler>();
        context.Services.AddTransient<ISyncEntityHandler, SpecimenSyncHandler>();
        context.Services.AddTransient<ISyncEntityHandler, SubcontractorSyncHandler>();
        context.Services.AddTransient<ISyncEntityHandler, EmployeeSyncHandler>();
        context.Services.AddTransient<ISyncEntityHandler, TestResultSyncHandler>();

        // TODO: Remove this in production - Bypass all permission checks for development
        context.Services.AddSingleton<IPermissionChecker, AlwaysAllowPermissionChecker>();
    }
}
