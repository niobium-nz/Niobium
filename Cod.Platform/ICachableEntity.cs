using System;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform
{
    public interface ICachableEntity : ITableEntity
    {
        DateTimeOffset GetExpiry(DateTimeOffset timeStart);

        IConvertible GetCache();

        void SetCache(IConvertible value);
    }
}
