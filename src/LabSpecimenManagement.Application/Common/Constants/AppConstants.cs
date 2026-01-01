namespace LabSpecimenManagement.Common;

/// <summary>
/// Application-wide constants for roles, folder paths, and common strings.
/// </summary>
public static class AppConstants
{
    #region Role Names

    /// <summary>
    /// Administrator role name
    /// </summary>
    public const string RoleAdmin = "admin";

    #endregion

    #region Folder Paths (relative to WebRootPath)

    /// <summary>
    /// Templates folder for document templates
    /// </summary>
    public const string FolderTemplates = "templates";

    /// <summary>
    /// Uploads folder for user-uploaded files
    /// </summary>
    public const string FolderUploads = "uploads";

    /// <summary>
    /// Temp folder for temporary files
    /// </summary>
    public const string FolderTemp = "temp";

    /// <summary>
    /// Testing images subfolder under uploads
    /// </summary>
    public const string FolderTestingImages = "testing-images";

    #endregion

    #region Sorting

    /// <summary>
    /// Descending sort direction keyword
    /// </summary>
    public const string SortDescending = "DESC";

    /// <summary>
    /// Ascending sort direction keyword
    /// </summary>
    public const string SortAscending = "ASC";

    /// <summary>
    /// Default sorting for employees
    /// </summary>
    public const string DefaultEmployeeSorting = "CreationTime DESC";

    #endregion

    #region Property Names (for ABP ExtraProperties)

    /// <summary>
    /// Department ID property name
    /// </summary>
    public const string PropertyDepartmentId = "DepartmentId";

    /// <summary>
    /// Position property name
    /// </summary>
    public const string PropertyPosition = "Position";

    /// <summary>
    /// Last login time property name
    /// </summary>
    public const string PropertyLastLoginTime = "LastLoginTime";

    #endregion
}
