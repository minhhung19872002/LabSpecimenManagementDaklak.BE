namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO for task status statistics (counts per status)
/// </summary>
public class TaskStatusStatisticsDto
{
    public int Total { get; set; }
    public int UnderEvaluation { get; set; }
    public int WaitingCustomerResponse { get; set; }
    public int PartialAccepted { get; set; }
    public int FullyAccepted { get; set; }
    public int Assigned { get; set; }
    public int InProgress { get; set; }
    public int WaitingApproval { get; set; }
    public int Approved { get; set; }
    public int SentToQuality { get; set; }
}
