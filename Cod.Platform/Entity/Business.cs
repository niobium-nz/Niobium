using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class Business : Cod.Model.Business, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(this.ETag); set => this.ETag = value.ToString(); }
    }
}
