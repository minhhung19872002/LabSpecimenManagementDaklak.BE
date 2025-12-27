using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Validation;

namespace LabSpecimenManagement.Files;

/// <summary>
/// Service để xử lý upload/download files
/// </summary>
public interface IFileAppService : IApplicationService
{
    /// <summary>
    /// Upload file và lưu vào thư mục uploads
    /// </summary>
    /// <param name="fileName">Tên file gốc</param>
    /// <param name="fileContent">Nội dung file dạng byte array</param>
    /// <returns>Thông tin file đã upload</returns>
    [DisableValidation]
    Task<FileUploadResultDto> UploadAsync(string fileName, byte[] fileContent);

    /// <summary>
    /// Lấy thông tin file theo FileId
    /// </summary>
    /// <param name="fileId">ID của file</param>
    /// <returns>Stream và thông tin file</returns>
    Task<(Stream? FileStream, string FileName, string ContentType)> GetFileAsync(string fileId);

    /// <summary>
    /// Xóa file theo FileId
    /// </summary>
    /// <param name="fileId">ID của file</param>
    Task DeleteAsync(string fileId);

    /// <summary>
    /// Kiểm tra file có tồn tại không
    /// </summary>
    /// <param name="fileId">ID của file</param>
    /// <returns>True nếu file tồn tại</returns>
    Task<bool> ExistsAsync(string fileId);
}
