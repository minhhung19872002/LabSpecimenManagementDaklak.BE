namespace LabSpecimenManagement.Testing;

/// <summary>
/// Thống kê số lượng công việc xét nghiệm theo trạng thái
/// </summary>
public class TestingStatusStatisticsDto
{
    public int Total { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int PendingApproval { get; set; }
    public int Rejected { get; set; }
    public int Completed { get; set; }
}
