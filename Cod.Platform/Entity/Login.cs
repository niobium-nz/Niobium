using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class Login : Cod.Model.Login, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
