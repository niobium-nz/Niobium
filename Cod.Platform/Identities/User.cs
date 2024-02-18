using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Identities
{
    public class User : Cod.Model.User, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
