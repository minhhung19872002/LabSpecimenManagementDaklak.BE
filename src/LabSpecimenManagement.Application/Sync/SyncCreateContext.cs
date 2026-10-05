using System;
using System.Threading;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Id đề xuất cho bản ghi đang được tạo từ sync offline (= ClientId FE sinh ra).
/// Dùng ClientId làm Id → gửi lặp cùng 1 item (retry sau khi mất response, BE restart, nhiều replica)
/// đụng khóa chính chứ không tạo bản ghi trùng.
/// </summary>
public static class SyncCreateContext
{
    private static readonly AsyncLocal<Guid?> PreferredIdHolder = new();

    public static IDisposable UsePreferredId(Guid id)
    {
        var previous = PreferredIdHolder.Value;
        PreferredIdHolder.Value = id;
        return new RestoreScope(() => PreferredIdHolder.Value = previous);
    }

    /// <summary>Lấy Id đề xuất (chỉ 1 lần) - bản ghi con tạo sau đó vẫn dùng Id tự sinh</summary>
    public static Guid? TakePreferredId()
    {
        var id = PreferredIdHolder.Value;
        PreferredIdHolder.Value = null;
        return id;
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly Action _restore;

        public RestoreScope(Action restore) => _restore = restore;

        public void Dispose() => _restore();
    }
}
