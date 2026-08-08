using System;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.Common;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SerializableUnitOfWorkAttribute : UnitOfWorkAttribute
{
    public SerializableUnitOfWorkAttribute()
    {
        IsTransactional = true;
        IsolationLevel = System.Data.IsolationLevel.Serializable;
    }
}
