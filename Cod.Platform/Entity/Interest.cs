using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class Interest : Cod.Model.Interest, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
