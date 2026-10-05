using System;
using Volo.Abp.Caching;

namespace LabSpecimenManagement.Sync;

/// <summary>
/// Ghi nhớ ClientId (id tạm phía FE) đã được tạo thành bản ghi nào trên server,
/// để item create gửi lặp (nhiều tab, retry sau khi mất response) không tạo thêm bản ghi trùng
/// </summary>
[CacheName("SyncCreated")]
public class SyncCreatedCacheItem
{
    public Guid ServerId { get; set; }
}
