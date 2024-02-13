using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class Transaction : Cod.Model.Transaction, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(this.ETag); set => this.ETag = value.ToString(); }
    }
}