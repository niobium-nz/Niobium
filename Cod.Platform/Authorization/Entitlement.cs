using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Authorization
{
    public class Entitlement : Cod.Model.Entitlement, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
