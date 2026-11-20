namespace LabSpecimenManagement.MasterData;

/// <summary>Loại dòng cấu hình của dự toán năm</summary>
public enum ForecastItemKind
{
    Material = 0,
    Equipment = 1,
}

/// <summary>Cách tính lượng vật tư bổ sung: theo từng mẫu hay tổng cả năm</summary>
public enum ForecastQuantityMode
{
    PerSample = 0,
    PerYear = 1,
}
