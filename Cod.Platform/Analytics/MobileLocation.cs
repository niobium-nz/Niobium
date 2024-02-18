using Azure;
using Azure.Data.Tables;

namespace Cod.Platform.Analytics
{
    public class MobileLocation : Model.MobileLocation, ITableEntity
    {
        ETag ITableEntity.ETag { get => new(ETag); set => ETag = value.ToString(); }
    }
}
