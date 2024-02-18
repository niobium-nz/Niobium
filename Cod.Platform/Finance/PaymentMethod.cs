using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Finance
{
    public class PaymentMethod : Cod.Model.PaymentMethod, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}