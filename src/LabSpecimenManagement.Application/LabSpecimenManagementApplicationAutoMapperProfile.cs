using AutoMapper;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.Subcontractors;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestResults;

namespace LabSpecimenManagement;

public class LabSpecimenManagementApplicationAutoMapperProfile : Profile
{
    public LabSpecimenManagementApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */

        // TestRequest mappings
        CreateMap<TestRequests.TestRequest, TestRequests.TestRequestDto>();
        // Note: CreateTestRequestDto -> TestRequest mapping is handled by TestRequestAppService.MapToEntityAsync()
        // to auto-generate the Request Number
        CreateMap<TestRequests.UpdateTestRequestDto, TestRequests.TestRequest>()
            .ForMember(dest => dest.RequestNumber, opt => opt.Ignore()); // Don't overwrite RequestNumber on update

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
        CreateMap<CreateUpdateSpecimenTypeDto, SpecimenType>();

        // TestMethod mappings
        CreateMap<TestMethod, TestMethodDto>();
        CreateMap<CreateUpdateTestMethodDto, TestMethod>();

        // StorageLocation mappings
        CreateMap<StorageLocation, StorageLocationDto>();
        CreateMap<CreateUpdateStorageLocationDto, StorageLocation>();

        // Customer mappings
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateUpdateCustomerDto, Customer>();

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
    }
}
