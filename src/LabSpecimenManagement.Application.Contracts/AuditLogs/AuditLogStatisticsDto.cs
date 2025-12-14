namespace LabSpecimenManagement.AuditLogs;

public class AuditLogStatisticsDto
{
    public long Total { get; set; }
    public long Today { get; set; }
    public long Gets { get; set; }
    public long Posts { get; set; }
    public long Puts { get; set; }
    public long Deletes { get; set; }
}
