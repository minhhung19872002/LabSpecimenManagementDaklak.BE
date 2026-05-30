using System;
using System.Collections.Generic;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.DataMigration;

public partial class MigrationAppService : ApplicationService, IMigrationAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<LaboratoryTask, Guid> _labTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _labTaskIndicatorRepository;
    private readonly IRepository<QualityManagementTask, Guid> _qmTaskRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _qmIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public MigrationAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<LaboratoryTask, Guid> labTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> labTaskIndicatorRepository,
        IRepository<QualityManagementTask, Guid> qmTaskRepository,
        IRepository<QualityManagementIndicator, Guid> qmIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Department, Guid> departmentRepository,
        IIdentityUserRepository userRepository,
        IGuidGenerator guidGenerator,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _labTaskRepository = labTaskRepository;
        _labTaskIndicatorRepository = labTaskIndicatorRepository;
        _qmTaskRepository = qmTaskRepository;
        _qmIndicatorRepository = qmIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _laboratoryRepository = laboratoryRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _guidGenerator = guidGenerator;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
    }

    internal class MasterData
    {
        public Dictionary<string, Guid> IndicatorByName { get; set; } = new();
        public Dictionary<string, Guid> MethodByName { get; set; } = new();
        public Dictionary<string, Guid> UserByFullName { get; set; } = new();
        public Dictionary<Guid, Guid?> DeptIdByUserId { get; set; } = new();
        public Dictionary<Guid, Guid> LabByDeptId { get; set; } = new();
        public Dictionary<string, Guid> LabByCode { get; set; } = new();
        public HashSet<string> ExistingContracts { get; set; } = new();
    }
}
