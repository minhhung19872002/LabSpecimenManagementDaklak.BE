using System;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestResults;

public class GetTestResultListDto : PagedAndSortedResultRequestDto
{
    public string? Search { get; set; }
    public ResultApprovalStatus? ApprovalStatus { get; set; }
    public bool? IsPass { get; set; }
    public Guid? TestId { get; set; }
}
