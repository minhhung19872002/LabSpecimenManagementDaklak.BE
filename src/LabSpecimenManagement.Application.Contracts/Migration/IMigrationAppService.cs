using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.DataMigration;

public interface IMigrationAppService : IApplicationService
{
    Task<MigrationPreviewDto> PreviewVnptMigrationAsync(byte[] fileBytes);

    Task<MigrationResultDto> ImportVnptMigrationAsync(byte[] fileBytes);

    Task<byte[]> ExportPreviewReportAsync(byte[] fileBytes);

    Task<MigrationRollbackResultDto> RollbackVnptMigrationAsync(byte[] fileBytes);
}
