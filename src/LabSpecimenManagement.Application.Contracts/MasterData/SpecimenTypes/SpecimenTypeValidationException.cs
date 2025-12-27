using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Custom exception for SpecimenType validation errors
/// </summary>
public class SpecimenTypeValidationException : Exception
{
    public Dictionary<string, string> Errors { get; }

    public SpecimenTypeValidationException(Dictionary<string, string> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}
