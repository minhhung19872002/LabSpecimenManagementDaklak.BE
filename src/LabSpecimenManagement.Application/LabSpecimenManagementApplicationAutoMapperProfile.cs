using AutoMapper;
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
        CreateMap<TestRequests.CreateTestRequestDto, TestRequests.TestRequest>();
        CreateMap<TestRequests.UpdateTestRequestDto, TestRequests.TestRequest>();

        // Specimen mappings
        CreateMap<TestRequests.Specimen, Specimens.SpecimenDto>();
        CreateMap<Specimens.CreateSpecimenDto, TestRequests.Specimen>();
        CreateMap<Specimens.UpdateSpecimenDto, TestRequests.Specimen>();

        // Test mappings
        CreateMap<TestRequests.Test, Tests.TestDto>();
        CreateMap<Tests.CreateTestDto, TestRequests.Test>();
        CreateMap<Tests.UpdateTestDto, TestRequests.Test>();

        // TestResult mappings
        CreateMap<TestRequests.TestResult, TestResults.TestResultDto>();
        CreateMap<TestResults.CreateTestResultDto, TestRequests.TestResult>();
        CreateMap<TestResults.UpdateTestResultDto, TestRequests.TestResult>();

        // SpecimenHistory mappings
        CreateMap<TestRequests.SpecimenHistory, SpecimenHistories.SpecimenHistoryDto>();
        CreateMap<SpecimenHistories.CreateSpecimenHistoryDto, TestRequests.SpecimenHistory>();
        CreateMap<SpecimenHistories.UpdateSpecimenHistoryDto, TestRequests.SpecimenHistory>();
    }
}
