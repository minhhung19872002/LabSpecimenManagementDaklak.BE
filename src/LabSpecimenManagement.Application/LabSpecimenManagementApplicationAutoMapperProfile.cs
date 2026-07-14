using AutoMapper;
using LabSpecimenManagement.Files;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.MasterData.Organizations;
using LabSpecimenManagement.MasterData.SampleTypeCategories;
using LabSpecimenManagement.MasterData.SampleConditions;
using LabSpecimenManagement.MasterData.Units;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.WarehouseImports;
using LabSpecimenManagement.Suppliers;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.ApprovalProcesses;
using LabSpecimenManagement.Subcontractors;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.TestResults;
using LabSpecimenManagement.SupplementaryTasks;
using LabSpecimenManagement.SupplementaryTasks.Dtos;

namespace LabSpecimenManagement;

public class LabSpecimenManagementApplicationAutoMapperProfile : Profile
{
    public LabSpecimenManagementApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        // TestRequest mappings
        CreateMap<TestRequests.TestRequest, TestRequests.TestRequestDto>()
            .ForMember(dest => dest.Samples, opt => opt.Ignore()) // Loaded separately in AppService
            .ForMember(dest => dest.SupervisorName, opt => opt.Ignore())
            .ForMember(dest => dest.DirectorName, opt => opt.Ignore());
        // Note: CreateTestRequestDto -> TestRequest mapping is handled by TestRequestAppService.MapToEntityAsync()
        // to auto-generate the Request Number
        CreateMap<TestRequests.UpdateTestRequestDto, TestRequests.TestRequest>()
            .ForMember(dest => dest.RequestNumber, opt => opt.Ignore()); // Don't overwrite RequestNumber on update

        // TestRequestSample mappings
        CreateMap<TestRequests.TestRequestSample, TestRequests.TestRequestSampleDto>()
            .ForMember(dest => dest.Indicators, opt => opt.Ignore()) // Loaded separately in AppService
            .ForMember(dest => dest.Attachments, opt => opt.Ignore()); // Mapped separately with URL

        // TestRequestSampleAttachment mappings
        CreateMap<TestRequestSampleAttachment, SampleAttachmentDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => $"/api/v1/files/{src.FileId}"));

        // Specimen mappings
        CreateMap<TestRequests.Specimen, Specimens.SpecimenDto>();
        // Note: CreateSpecimenDto -> Specimen mapping is handled by SpecimenAppService.MapToEntityAsync()
        // to auto-generate the Specimen Code
        CreateMap<Specimens.UpdateSpecimenDto, TestRequests.Specimen>()
            .ForMember(dest => dest.Code, opt => opt.Ignore()); // Don't overwrite Code on update

        // Test mappings
        CreateMap<TestRequests.Test, Tests.TestDto>();
        // Note: CreateTestDto -> Test mapping is handled by TestAppService.MapToEntityAsync()
        // to auto-generate the Test Code
        CreateMap<Tests.UpdateTestDto, TestRequests.Test>()
            .ForMember(dest => dest.Code, opt => opt.Ignore()); // Don't overwrite Code on update

        // TestResult mappings
        CreateMap<TestRequests.TestResult, TestResults.TestResultDto>();
        CreateMap<TestResults.CreateTestResultDto, TestRequests.TestResult>();
        CreateMap<TestResults.UpdateTestResultDto, TestRequests.TestResult>();

        // SpecimenHistory mappings
        CreateMap<TestRequests.SpecimenHistory, SpecimenHistories.SpecimenHistoryDto>();
        CreateMap<SpecimenHistories.CreateSpecimenHistoryDto, TestRequests.SpecimenHistory>();
        CreateMap<SpecimenHistories.UpdateSpecimenHistoryDto, TestRequests.SpecimenHistory>();

        // ============ NEW MAPPINGS ============

        // Department mappings
        CreateMap<Department, DepartmentDto>();
        CreateMap<CreateUpdateDepartmentDto, Department>();

        // SpecimenType mappings
        CreateMap<SpecimenType, SpecimenTypeDto>();
        CreateMap<SpecimenType, SpecimenTypeWithIndicatorsDto>();
        CreateMap<CreateUpdateSpecimenTypeDto, SpecimenType>();

        // TestMethod mappings
        CreateMap<TestMethod, TestMethodDto>();
        CreateMap<TestMethod, TestMethodWithIndicatorsDto>();
        CreateMap<CreateTestMethodDto, TestMethod>();
        CreateMap<UpdateTestMethodDto, TestMethod>();
        CreateMap<TestMethodIndicator, TestMethodIndicatorDto>();

        // Standard mappings
        CreateMap<Standard, StandardDto>();
        CreateMap<Standard, StandardWithIndicatorsDto>();
        CreateMap<CreateStandardDto, Standard>();
        CreateMap<UpdateStandardDto, Standard>();

        // StorageLocation mappings
        CreateMap<StorageLocation, StorageLocationDto>();
        CreateMap<CreateUpdateStorageLocationDto, StorageLocation>();

        // Customer mappings
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateUpdateCustomerDto, Customer>();

        // TestResultValue mappings
        CreateMap<TestResultValue, TestResultValueDto>();
        CreateMap<CreateUpdateTestResultValueDto, TestResultValue>();

        // Note mappings
        CreateMap<Note, MasterData.Notes.NoteDto>();

        // NCR mappings
        CreateMap<NCR, NCRDto>();
        CreateMap<CreateNCRDto, NCR>();
        CreateMap<UpdateNCRDto, NCR>();

        // Report mappings
        CreateMap<Report, ReportDto>();
        CreateMap<CreateReportDto, Report>();
        CreateMap<UpdateReportDto, Report>();

        // TaskAssignment mappings
        CreateMap<TaskAssignment, TaskAssignmentDto>();
        CreateMap<CreateTaskAssignmentDto, TaskAssignment>();
        CreateMap<UpdateTaskAssignmentDto, TaskAssignment>();

        // Subcontractor mappings
        CreateMap<Subcontractor, SubcontractorDto>();
        CreateMap<CreateUpdateSubcontractorDto, Subcontractor>();
        CreateMap<SubcontractorCertification, SubcontractorCertificationDto>();
        CreateMap<CreateUpdateCertificationDto, SubcontractorCertification>();

        // IndicatorGroup mappings
        CreateMap<IndicatorGroup, IndicatorGroupDto>();
        CreateMap<CreateUpdateIndicatorGroupDto, IndicatorGroup>();

        // SampleTypeCategory mappings
        CreateMap<SampleTypeCategory, SampleTypeCategoryDto>();
        CreateMap<CreateUpdateSampleTypeCategoryDto, SampleTypeCategory>();

        // SampleCondition mappings
        CreateMap<SampleCondition, SampleConditionDto>();
        CreateMap<CreateUpdateSampleConditionDto, SampleCondition>();

        // Unit mappings
        CreateMap<Unit, UnitDto>();
        CreateMap<CreateUpdateUnitDto, Unit>();

        // Warehouse mappings
        CreateMap<Warehouse, WarehouseDto>();
        CreateMap<CreateUpdateWarehouseDto, Warehouse>();

        // Supplier mappings
        CreateMap<Supplier, SupplierDto>()
            .ForMember(dest => dest.LogoUrl, opt => opt.Ignore());

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.BaseUnitName, opt => opt.Ignore())
            .ForMember(dest => dest.StorageAvailableQuantity, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.Ignore())
            .ForMember(dest => dest.UnitConversions, opt => opt.Ignore())
            .ForMember(dest => dest.Skus, opt => opt.Ignore())
            .ForMember(dest => dest.SkuCount, opt => opt.Ignore());

        // Manufacturer mappings
        CreateMap<Manufacturer, ManufacturerDto>();

        // GoodsReceipt mappings
        CreateMap<GoodsReceipt, GoodsReceiptListDto>()
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore());
        CreateMap<GoodsReceipt, GoodsReceiptDto>()
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaserFullName, opt => opt.Ignore());
        CreateMap<GoodsReceiptItem, GoodsReceiptItemDto>();
        CreateMap<GoodsReceiptPayment, GoodsReceiptPaymentDto>();

        // WarehouseImportReceipt mappings
        CreateMap<WarehouseImportReceipt, WarehouseImportReceiptListDto>()
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore())
            .ForMember(dest => dest.HasDefectiveItem, opt => opt.Ignore());
        CreateMap<WarehouseImportReceipt, WarehouseImportReceiptDto>()
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore())
            .ForMember(dest => dest.HasDefectiveItem, opt => opt.Ignore());
        CreateMap<WarehouseImportReceiptItem, WarehouseImportReceiptItemDto>();

        // Indicator mappings
        CreateMap<Indicator, IndicatorDto>()
            .ForMember(dest => dest.IndicatorGroupName, opt => opt.Ignore())
            .ForMember(dest => dest.CanDelete, opt => opt.Ignore());

        // Organization mappings
        CreateMap<Organization, OrganizationDto>();
        CreateMap<CreateUpdateOrganizationDto, Organization>();

        // Laboratory mappings
        // Note: Laboratory -> LaboratoryDto mapping is done manually in LaboratoryAppService
        // because Name is computed from Department.Name via join
        CreateMap<Laboratory, LaboratoryDto>()
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ForMember(dest => dest.TechnicalManagerName, opt => opt.Ignore());
        CreateMap<CreateUpdateLaboratoryDto, Laboratory>();

        // SupplementaryTask mappings
        CreateMap<SupplementaryTask, SupplementaryTaskDto>()
            .ForMember(dest => dest.ImageUrls, opt => opt.Ignore())
            .ForMember(dest => dest.CompletionImageUrls, opt => opt.Ignore());

        // ApprovalProcess mappings
        CreateMap<ApprovalProcess, ApprovalProcessDto>();
        CreateMap<ApprovalProcessStep, ApprovalProcessStepDto>();
        CreateMap<ApprovalSnapshot, ApprovalSnapshotDto>();
        CreateMap<ApprovalSnapshotStep, ApprovalSnapshotStepDto>();
    }
}
