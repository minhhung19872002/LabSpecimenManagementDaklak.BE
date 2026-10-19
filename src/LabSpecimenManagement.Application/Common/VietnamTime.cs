using System;

namespace LabSpecimenManagement.Common;

/// <summary>
/// Quy đổi ngày người dùng chọn (theo lịch Việt Nam) sang giờ server.
/// Server có thể chạy UTC nên không dùng trực tiếp .Date / DateTime.Today cho ngày nghiệp vụ.
/// </summary>
public static class VietnamTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    /// <summary>
    /// Ngày hôm nay theo giờ Việt Nam
    /// </summary>
    public static DateTime Today => DateTimeOffset.UtcNow.ToOffset(Offset).Date;

    /// <summary>
    /// 23:59:59 cuối ngày được chọn theo giờ Việt Nam, quy về giờ server để so sánh được với DateTime.Now.
    /// Ví dụ: chọn 15/10 → 15/10 23:59:59 (+07) = 15/10 16:59:59 trên server UTC.
    /// </summary>
    public static DateTime EndOfDayToServerTime(DateTime selectedDate)
    {
        var endOfDayVn = DateTime.SpecifyKind(selectedDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);
        return new DateTimeOffset(endOfDayVn, Offset).LocalDateTime;
    }
}
