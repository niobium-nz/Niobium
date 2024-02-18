using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Tenants
{
    public class Business : Cod.Model.Business, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
