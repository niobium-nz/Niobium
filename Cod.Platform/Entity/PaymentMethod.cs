using Azure;
using Azure.Data.Tables;

namespace Cod.Platform
{
    public class PaymentMethod : Cod.Model.PaymentMethod, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(this.ETag); set => this.ETag = value.ToString(); }
    }
}