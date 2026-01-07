namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Input for BPGN confirming delivery to customer
/// </summary>
public class ConfirmDeliveryInput
{
    /// <summary>
    /// Optional note when confirming delivery
    /// </summary>
    public string? Note { get; set; }
}
