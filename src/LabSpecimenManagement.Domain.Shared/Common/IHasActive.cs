namespace LabSpecimenManagement.Common;

/// <summary>
/// Interface for entities that have an IsActive property
/// Used for soft-delete or active/inactive status filtering
/// </summary>
public interface IHasActive
{
    /// <summary>
    /// Indicates whether the entity is active
    /// </summary>
    bool IsActive { get; set; }
}
