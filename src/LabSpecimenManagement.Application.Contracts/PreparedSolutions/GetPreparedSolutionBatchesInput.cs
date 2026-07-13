using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.PreparedSolutions;

public class GetPreparedSolutionBatchesInput : PagedAndSortedResultRequestDto
{
    public string? Search { get; set; }

    // null = all; maps to tab selection on FE
    public PreparedSolutionStatus? Status { get; set; }

    // "HetHetHan" tab groups Expired(3) + Empty(4) — handled in AppService
    public bool? IsHetOrHetHan { get; set; }
}
