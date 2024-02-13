using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class User : Cod.Model.User, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(this.ETag); set => this.ETag = value.ToString(); }
    }
}
