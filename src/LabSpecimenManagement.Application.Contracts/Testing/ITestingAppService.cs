using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.QualityManagement;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// Interface for Testing Application Service
/// </summary>
public interface ITestingAppService : IApplicationService
{
    /// <summary>
    /// Get list of testing tasks with filters
    /// </summary>
    Task<PagedResultDto<TestingListDto>> GetListAsync(GetTestingListInput input);

    /// <summary>
    /// Get testing task detail by LaboratoryTask ID
    /// </summary>
    /// <param name="laboratoryTaskId">The LaboratoryTask ID</param>
    /// <param name="viewMode">Optional view mode - MyAssignments shows only current user's data, All shows full data</param>
    Task<TestingDetailDto> GetAsync(Guid laboratoryTaskId, TestingTabType? viewMode = null);

    /// <summary>
    /// Save results (draft) for indicators
    /// </summary>
    Task SaveResultsAsync(Guid laboratoryTaskId, SaveResultsInput input);

    /// <summary>
    /// Submit indicators for approval
    /// </summary>
    Task SubmitForApprovalAsync(Guid laboratoryTaskId, SubmitForApprovalInput input);

    /// <summary>
    /// Get rejection history for an indicator
    /// </summary>
    Task<List<RejectionHistoryDto>> GetRejectionHistoryAsync(Guid indicatorId);

    /// <summary>
    /// Check-in indicators (bắt đầu thử nghiệm - ấn "Thực hiện")
    /// </summary>
    Task CheckInIndicatorsAsync(Guid laboratoryTaskId, CheckInIndicatorsInput input);

    /// <summary>
    /// Submit results for approval (gửi kết quả cho QLKT)
    /// </summary>
    Task SubmitResultsAsync(Guid laboratoryTaskId, SubmitResultsInput input);

    /// <summary>
    /// Upload sample testing image (before/after testing)
    /// </summary>
    Task<SampleTestingImageDto> UploadSampleTestingImageAsync(Guid laboratoryTaskId, UploadSampleTestingImageInput input, string fileId, string fileName, string filePath, long fileSize);

    /// <summary>
    /// Delete sample testing image
    /// </summary>
    Task DeleteSampleTestingImageAsync(Guid imageId);

    /// <summary>
    /// Get sample testing images
    /// </summary>
    Task<List<SampleTestingImageDto>> GetSampleTestingImagesAsync(Guid laboratoryTaskId, Guid sampleId, SampleTestingImageType? imageType = null);

    /// <summary>
    /// Upload indicator image
    /// </summary>
    Task<IndicatorImageDto> UploadIndicatorImageAsync(Guid laboratoryTaskId, Guid indicatorId, string fileId, string fileName, string filePath, long fileSize);

    /// <summary>
    /// Delete indicator image
    /// </summary>
    Task DeleteIndicatorImageAsync(Guid imageId);

    /// <summary>
    /// Get indicator images
    /// </summary>
    Task<List<IndicatorImageDto>> GetIndicatorImagesAsync(Guid indicatorId);

    /// <summary>
    /// KTV nhận mẫu - đánh dấu mẫu đã nhận trước khi thực hiện thí nghiệm
    /// </summary>
    Task ReceiveSampleAsync(Guid laboratoryTaskId, Guid sampleId);

    /// <summary>
    /// Kiểm tra mẫu đã được nhận chưa
    /// </summary>
    Task<bool> IsSampleReceivedAsync(Guid laboratoryTaskId, Guid sampleId);

    /// <summary>
    /// KTV báo không thể thử nghiệm chỉ tiêu
    /// </summary>
    /// <param name="laboratoryTaskId">Laboratory Task ID</param>
    /// <param name="indicatorId">Indicator ID</param>
    /// <param name="input">Thông tin báo không TN được</param>
    /// <returns>Indicator sau khi cập nhật</returns>
    Task<TestingIndicatorDto> MarkCannotTestAsync(Guid laboratoryTaskId, Guid indicatorId, CannotTestDto input);

    /// <summary>
    /// Upload hình ảnh cho rejection history
    /// </summary>
    /// <param name="rejectionHistoryId">Rejection History ID</param>
    /// <param name="fileId">File ID từ file storage</param>
    /// <param name="fileName">Tên file</param>
    /// <param name="filePath">Đường dẫn file</param>
    /// <param name="fileSize">Kích thước file</param>
    /// <returns>Image DTO đã upload</returns>
    Task<RejectionHistoryImageDto> UploadRejectionHistoryImageAsync(Guid rejectionHistoryId, string fileId, string fileName, string filePath, long fileSize);

    /// <summary>
    /// Xóa hình ảnh rejection history
    /// </summary>
    Task DeleteRejectionHistoryImageAsync(Guid imageId);

    /// <summary>
    /// Lấy danh sách Sổ nhận mẫu giao việc - query từ LaboratoryTaskIndicator
    /// (Không yêu cầu QualityManagementTask đã tạo)
    /// </summary>
    Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync(GetSampleAssignmentLogInput input);
}
