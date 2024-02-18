using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Finance
{
    public class Transaction : Cod.Model.Transaction, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}