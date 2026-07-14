using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Statistics;

// Chart 1: Số mẫu & doanh thu theo ngày
public class LabDailyStatDto
{
    public string Date { get; set; } = string.Empty;   // "2026-07-01"
    public int SampleCount { get; set; }
    public decimal Revenue { get; set; }
}

// Chart 2: Tổng chỉ tiêu theo hợp đồng/tháng
public class ContractMonthlyIndicatorDto
{
    public Guid TestRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string? ContractNumber { get; set; }
    public DateTime? ContractDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int IndicatorCount { get; set; }
}

// Section 3 left panel: Danh sách hợp đồng
public class LabContractSummaryDto
{
    public Guid TestRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string? ContractNumber { get; set; }
    public DateTime? ContractDate { get; set; }
    public int SampleCount { get; set; }
    public int IndicatorCount { get; set; }
    public decimal TotalPrice { get; set; }
}

// Chart 3: Số hợp đồng theo chỉ tiêu / tháng
public class IndicatorContractCountDto
{
    public string IndicatorName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int ContractCount { get; set; }
}

// Wrapper cho main API call
public class LabReportDataDto
{
    public List<LabDailyStatDto> DailyStats { get; set; } = new();
    public List<ContractMonthlyIndicatorDto> ContractIndicators { get; set; } = new();
    public List<LabContractSummaryDto> Contracts { get; set; } = new();
    public List<IndicatorContractCountDto> IndicatorContractCounts { get; set; } = new();
}

// Section 3 right panel: Chi tiết hợp đồng
public class SamplePriceItemDto
{
    public string SampleCode { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class LabContractDetailDto
{
    public string RequestNumber { get; set; } = string.Empty;
    public string? ContractNumber { get; set; }
    public DateTime? ContractDate { get; set; }
    public int TotalSamples { get; set; }
    public decimal TotalPrice { get; set; }
    public List<SamplePriceItemDto> Samples { get; set; } = new();
}
