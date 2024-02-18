using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Analytics
{
    public class Report : Cod.Model.Report, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
