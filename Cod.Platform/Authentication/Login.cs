using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Authentication
{
    public class Login : Cod.Model.Login, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
