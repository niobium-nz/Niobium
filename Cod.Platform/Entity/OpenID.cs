using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class OpenID : Cod.Model.OpenID, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
