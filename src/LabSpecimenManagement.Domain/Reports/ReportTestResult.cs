using System;
using Volo.Abp.Domain.Entities;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Reports;

/// <summary>
/// Junction table cho Report và TestResult
/// </summary>
public class ReportTestResult : Entity
{
    public Guid ReportId { get; set; }
    public Guid TestResultId { get; set; }

    public virtual Report Report { get; set; }
    public virtual TestResult TestResult { get; set; }

    protected ReportTestResult()
    {
    }

    public ReportTestResult(Guid reportId, Guid testResultId)
    {
        ReportId = reportId;
        TestResultId = testResultId;
    }

    public override object[] GetKeys()
    {
        return new object[] { ReportId, TestResultId };
    }
}
