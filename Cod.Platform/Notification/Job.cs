using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Notification
{
    public class Job : Model.Job, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
