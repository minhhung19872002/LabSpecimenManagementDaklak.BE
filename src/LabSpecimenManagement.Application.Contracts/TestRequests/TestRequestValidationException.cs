using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Custom exception for TestRequest validation errors
/// </summary>
public class TestRequestValidationException : Exception
{
    public Dictionary<string, string> Errors { get; }

    public TestRequestValidationException(Dictionary<string, string> errors)
        : base("Dữ liệu không hợp lệ")
    {
        Errors = errors;
    }

    public TestRequestValidationException(string fieldName, string message)
        : base("Dữ liệu không hợp lệ")
    {
        Errors = new Dictionary<string, string>
        {
            { fieldName, message }
        };
    }
}
