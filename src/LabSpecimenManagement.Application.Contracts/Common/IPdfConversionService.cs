using System.Threading.Tasks;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Service for converting documents to PDF
/// </summary>
public interface IPdfConversionService
{
    /// <summary>
    /// Convert a DOCX file to PDF
    /// </summary>
    /// <param name="inputDocxPath">Full path to the input DOCX file</param>
    /// <param name="outputPdfPath">Full path for the output PDF file (optional, will use same name if not provided)</param>
    /// <returns>Full path to the generated PDF file</returns>
    Task<string> ConvertDocxToPdfAsync(string inputDocxPath, string? outputPdfPath = null);

    /// <summary>
    /// Check if PDF conversion service is available
    /// </summary>
    bool IsLibreOfficeAvailable();
}
