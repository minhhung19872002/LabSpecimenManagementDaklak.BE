using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Service for handling offline data synchronization
/// Implements "last write wins" conflict resolution strategy
/// </summary>
public partial class SyncAppService : LabSpecimenManagementAppService, ISyncAppService
{
    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly SyncHandlerRegistry _handlerRegistry;
    private readonly ILogger<SyncAppService> _logger;

    public SyncAppService(
        ITestRequestAppService testRequestAppService,
        IRepository<TestRequest, Guid> testRequestRepository,
        SyncHandlerRegistry handlerRegistry,
        ILogger<SyncAppService> logger)
    {
        _testRequestAppService = testRequestAppService;
        _testRequestRepository = testRequestRepository;
        _handlerRegistry = handlerRegistry;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SyncBatchResponseDto> SyncTestRequestsAsync(SyncBatchRequestDto input)
    {
        var response = new SyncBatchResponseDto
        {
            Results = new List<SyncResultDto>(),
            ServerTimestamp = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Starting sync batch processing. DeviceId: {DeviceId}, Items: {Count}, SyncTimestamp: {Timestamp}",
            input.DeviceId,
            input.TestRequests.Count,
            input.SyncTimestamp);

        foreach (var item in input.TestRequests)
        {
            var result = await ProcessSingleSyncItemAsync(item);
            response.Results.Add(result);

            if (result.Success)
            {
                response.SuccessCount++;
                if (result.ConflictResolution.HasValue)
                {
                    response.ConflictCount++;
                }
            }
            else
            {
                response.FailedCount++;
            }
        }

        // If client provided last sync timestamp, include server changes
        if (input.LastSyncTimestamp.HasValue)
        {
            var serverChanges = await GetServerChangesAsync(new GetServerChangesDto
            {
                LastSyncTimestamp = input.LastSyncTimestamp.Value,
                DeviceId = input.DeviceId,
                MaxResults = 100
            });
            response.ServerChanges = serverChanges.Changes;
        }

        _logger.LogInformation(
            "Sync batch completed. Success: {Success}, Failed: {Failed}, Conflicts: {Conflicts}",
            response.SuccessCount,
            response.FailedCount,
            response.ConflictCount);

        return response;
    }

    /// <inheritdoc />
    public async Task<ServerChangesResponseDto> GetServerChangesAsync(GetServerChangesDto input)
    {
        var response = new ServerChangesResponseDto
        {
            Changes = new List<TestRequestDto>(),
            DeletedIds = new List<Guid>(),
            ServerTimestamp = DateTime.UtcNow
        };

        // Get test requests modified since last sync
        var query = await _testRequestRepository.GetQueryableAsync();
        var changedEntities = query
            .Where(x => x.LastModificationTime > input.LastSyncTimestamp ||
                       x.CreationTime > input.LastSyncTimestamp)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModificationTime ?? x.CreationTime)
            .Take(input.MaxResults + 1)
            .ToList();

        response.HasMore = changedEntities.Count > input.MaxResults;
        var entitiesToReturn = changedEntities.Take(input.MaxResults).ToList();

        foreach (var entity in entitiesToReturn)
        {
            var dto = await _testRequestAppService.GetAsync(entity.Id);
            response.Changes.Add(dto);
        }

        // Get deleted IDs since last sync (soft deleted)
        var deletedEntities = query
            .Where(x => x.IsDeleted && x.DeletionTime > input.LastSyncTimestamp)
            .Select(x => x.Id)
            .Take(input.MaxResults)
            .ToList();

        response.DeletedIds = deletedEntities;
        response.TotalCount = response.Changes.Count + response.DeletedIds.Count;

        _logger.LogInformation(
            "Server changes retrieved. Changes: {Changes}, Deleted: {Deleted}, HasMore: {HasMore}",
            response.Changes.Count,
            response.DeletedIds.Count,
            response.HasMore);

        return response;
    }

    /// <inheritdoc />
    public Task<SyncTimestampDto> GetServerTimestampAsync()
    {
        return Task.FromResult(new SyncTimestampDto
        {
            ServerTimestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Process a single sync item with conflict resolution
    /// </summary>
    private async Task<SyncResultDto> ProcessSingleSyncItemAsync(SyncTestRequestDto item)
    {
        var result = new SyncResultDto
        {
            ClientId = item.ClientId
        };

        try
        {
            switch (item.OperationType)
            {
                case SyncOperationType.Create:
                    result = await ProcessCreateAsync(item);
                    break;

                case SyncOperationType.Update:
                    result = await ProcessUpdateAsync(item);
                    break;

                case SyncOperationType.Delete:
                    result = await ProcessDeleteAsync(item);
                    break;

                default:
                    result.Success = false;
                    result.Error = $"Unknown operation type: {item.OperationType}";
                    break;
            }
        }
        catch (AbpValidationException ex)
        {
            result.Success = false;
            result.Error = "Validation failed";
            result.ValidationErrors = ex.ValidationErrors
                .Select(e => $"{e.MemberNames.FirstOrDefault()}: {e.ErrorMessage}")
                .ToList();

            _logger.LogWarning(
                "Sync validation failed for ClientId: {ClientId}. Errors: {Errors}",
                item.ClientId,
                string.Join(", ", result.ValidationErrors));
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;

            _logger.LogError(ex,
                "Sync failed for ClientId: {ClientId}, OperationType: {OperationType}",
                item.ClientId,
                item.OperationType);
        }

        return result;
    }

    /// <summary>
    /// Process create operation
    /// </summary>
    private async Task<SyncResultDto> ProcessCreateAsync(SyncTestRequestDto item)
    {
        var result = new SyncResultDto
        {
            ClientId = item.ClientId
        };

        // Create the test request using the existing service
        var createdDto = await _testRequestAppService.CreateAsync(item.Data);

        result.Success = true;
        result.ServerId = createdDto.Id;
        result.ServerData = createdDto;

        _logger.LogInformation(
            "Sync create succeeded. ClientId: {ClientId}, ServerId: {ServerId}",
            item.ClientId,
            createdDto.Id);

        return result;
    }

    /// <summary>
    /// Process update operation with conflict resolution
    /// </summary>
    private async Task<SyncResultDto> ProcessUpdateAsync(SyncTestRequestDto item)
    {
        var result = new SyncResultDto
        {
            ClientId = item.ClientId,
            ServerId = item.ServerId
        };

        if (!item.ServerId.HasValue)
        {
            result.Success = false;
            result.Error = "ServerId is required for update operations";
            return result;
        }

        // Get existing entity to check for conflicts
        var existingEntity = await _testRequestRepository.FindAsync(item.ServerId.Value);
        if (existingEntity == null)
        {
            result.Success = false;
            result.Error = $"Test request with ID {item.ServerId} not found";
            return result;
        }

        // Conflict detection using timestamps
        var serverLastModified = existingEntity.LastModificationTime ?? existingEntity.CreationTime;
        var conflict = item.ClientLastModified < serverLastModified;

        if (conflict)
        {
            // "Last write wins" - compare timestamps
            if (item.ClientLastModified > serverLastModified)
            {
                // Client wins - apply client changes
                result.ConflictResolution = SyncConflictResolution.ClientWins;
                var updateDto = MapToUpdateDto(item.Data);
                var updatedDto = await _testRequestAppService.UpdateAsync(item.ServerId.Value, updateDto);
                result.Success = true;
                result.ServerData = updatedDto;

                _logger.LogInformation(
                    "Sync conflict resolved: ClientWins. ClientId: {ClientId}, ServerId: {ServerId}",
                    item.ClientId,
                    item.ServerId);
            }
            else
            {
                // Server wins - return current server state
                result.ConflictResolution = SyncConflictResolution.ServerWins;
                result.Success = true;
                result.ServerData = await _testRequestAppService.GetAsync(item.ServerId.Value);

                _logger.LogInformation(
                    "Sync conflict resolved: ServerWins. ClientId: {ClientId}, ServerId: {ServerId}",
                    item.ClientId,
                    item.ServerId);
            }
        }
        else
        {
            // No conflict - apply update
            var updateDto = MapToUpdateDto(item.Data);
            var updatedDto = await _testRequestAppService.UpdateAsync(item.ServerId.Value, updateDto);
            result.Success = true;
            result.ServerData = updatedDto;

            _logger.LogInformation(
                "Sync update succeeded (no conflict). ClientId: {ClientId}, ServerId: {ServerId}",
                item.ClientId,
                item.ServerId);
        }

        return result;
    }

    /// <summary>
    /// Process delete operation
    /// </summary>
    private async Task<SyncResultDto> ProcessDeleteAsync(SyncTestRequestDto item)
    {
        var result = new SyncResultDto
        {
            ClientId = item.ClientId,
            ServerId = item.ServerId
        };

        if (!item.ServerId.HasValue)
        {
            result.Success = false;
            result.Error = "ServerId is required for delete operations";
            return result;
        }

        await _testRequestAppService.DeleteAsync(item.ServerId.Value);
        result.Success = true;

        _logger.LogInformation(
            "Sync delete succeeded. ClientId: {ClientId}, ServerId: {ServerId}",
            item.ClientId,
            item.ServerId);

        return result;
    }

    /// <summary>
    /// Map CreateTestRequestDto to UpdateTestRequestDto
    /// </summary>
    private UpdateTestRequestDto MapToUpdateDto(CreateTestRequestDto createDto)
    {
        return new UpdateTestRequestDto
        {
            ContractNumber = createDto.ContractNumber,
            ContractDate = createDto.ContractDate,
            SentToAccounting = createDto.SentToAccounting,
            PaymentReceived = createDto.PaymentReceived,
            SentToDepartment = createDto.SentToDepartment,
            ResultReturned = createDto.ResultReturned,
            CustomerName = createDto.CustomerName,
            ProvinceCode = createDto.ProvinceCode,
            Province = createDto.Province,
            DistrictCode = createDto.DistrictCode,
            District = createDto.District,
            WardCode = createDto.WardCode,
            Ward = createDto.Ward,
            Address = createDto.Address,
            TaxCode = createDto.TaxCode,
            ContactPerson = createDto.ContactPerson,
            Phone = createDto.Phone,
            Email = createDto.Email,
            TotalSampleCount = createDto.TotalSampleCount,
            MethodWestHighlandInstitute = createDto.MethodWestHighlandInstitute,
            MethodCustomer = createDto.MethodCustomer,
            MethodCustomerDetail = createDto.MethodCustomerDetail,
            ResultLanguage = createDto.ResultLanguage,
            ResultCopyCount = createDto.ResultCopyCount,
            SeparatePassedResults = createDto.SeparatePassedResults,
            ConformityDeclaration = createDto.ConformityDeclaration,
            OtherRequirements = createDto.OtherRequirements,
            SubContract = createDto.SubContract,
            SamplingRequirement = createDto.SamplingRequirement,
            DeliveryDirectPickup = createDto.DeliveryDirectPickup,
            DeliveryPostal = createDto.DeliveryPostal,
            ReturnStoredSample = createDto.ReturnStoredSample,
            SampleTypeService = createDto.SampleTypeService,
            SampleTypeContract = createDto.SampleTypeContract,
            SampleTypeTask = createDto.SampleTypeTask,
            SampleTypeSubContract = createDto.SampleTypeSubContract,
            ResultDeliveryDate = createDto.ResultDeliveryDate,
            SampleStorage = createDto.SampleStorage,
            TotalCost = createDto.TotalCost,
            TotalCostInWords = createDto.TotalCostInWords,
            ReceiptNumber = createDto.ReceiptNumber,
            SupervisorId = createDto.SupervisorId,
            DirectorId = createDto.DirectorId,
            ChangeRequest = createDto.ChangeRequest,
            AgreeTerms = createDto.AgreeTerms,
            RequestDate = createDto.RequestDate,
            Notes = createDto.Notes,
            ExpectedCompletionDate = createDto.ExpectedCompletionDate,
            Subcontractor = createDto.Subcontractor,
            Samples = createDto.Samples.Select(s => new UpdateTestRequestSampleDto
            {
                Id = null, // New samples don't have server IDs yet
                SortOrder = s.SortOrder,
                SampleCode = s.SampleCode,
                SampleName = s.SampleName,
                SamplingAddress = s.SamplingAddress,
                SampleQuantity = s.SampleQuantity,
                Status = s.Status,
                StatusText = s.StatusText,
                ProcessingStatus = s.ProcessingStatus,
                Notes = s.Notes,
                StorageLocation = s.StorageLocation,
                SpecimenTypeId = s.SpecimenTypeId,
                SpecimenTypeName = s.SpecimenTypeName,
                Indicators = s.Indicators.Select(i => new UpdateTestRequestSampleIndicatorDto
                {
                    Id = null, // New indicators don't have server IDs yet
                    IndicatorId = i.IndicatorId,
                    TestMethodId = i.TestMethodId,
                    LaboratoryId = i.LaboratoryId,
                    UnitPrice = i.UnitPrice,
                    Notes = i.Notes,
                    TransferDate = i.TransferDate,
                    RequestDate = i.RequestDate,
                    SentToDepartment = i.SentToDepartment
                }).ToList(),
                Attachments = s.Attachments
            }).ToList()
        };
    }
}
