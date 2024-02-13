using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class Job : Cod.Model.Job, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
