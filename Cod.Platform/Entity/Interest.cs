using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cod.Platform
{
    public class Interest : Cod.Model.Interest, ITableEntity
    {
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext) => TableEntityHelper.ReflectionRead(this, properties, operationContext);

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext) => TableEntityHelper.ReflectionWrite(this, operationContext);
    }
}
