namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class SupplementaryTaskStatsDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Overdue { get; set; }
    public int Completed { get; set; }
    public int WaitingForConfirmation { get; set; }
    public int Rejected { get; set; }
    public int Cancelled { get; set; }
    public bool IsDirector { get; set; }
}
