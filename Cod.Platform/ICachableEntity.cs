using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public interface ICachableEntity : ITableEntity
    {
        DateTimeOffset GetExpiry(DateTimeOffset timeStart);

        IConvertible GetCache();

        void SetCache(IConvertible value);
    }
}
