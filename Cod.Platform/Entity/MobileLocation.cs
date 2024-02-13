using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class MobileLocation : Cod.Model.MobileLocation, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
