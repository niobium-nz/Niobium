using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class Report : Cod.Model.Report, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(this.ETag); set => this.ETag = value.ToString(); }
    }
}
