namespace LabSpecimenManagement.Users;

/// <summary>
/// User roles in the lab specimen management system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// System administrator
    /// </summary>
    Admin = 0,

    /// <summary>
    /// Lab manager
    /// </summary>
    Manager = 1,

    /// <summary>
    /// Lab technician (KTV) - performs tests
    /// </summary>
    Technician = 2,

    /// <summary>
    /// Reviewer (B1) - first level review
    /// </summary>
    Reviewer = 3,

    /// <summary>
    /// Approver (B2) - final approval
    /// </summary>
    Approver = 4,

    /// <summary>
    /// Sample collector - collects specimens
    /// </summary>
    SampleCollector = 5,

    /// <summary>
    /// Quality assurance staff
    /// </summary>
    QualityAssurance = 6,

    /// <summary>
    /// Viewer - read-only access
    /// </summary>
    Viewer = 7
}
