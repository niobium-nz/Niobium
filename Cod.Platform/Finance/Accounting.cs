using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Finance
{
    public class Accounting : Cod.Model.Accounting, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}