using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform.Model
{
    public class Login : Cod.Login, ITableEntity
    {
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext) => TableEntityHelper.ReflectionRead(this, properties, operationContext);

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext) => TableEntityHelper.ReflectionWrite(this, operationContext);
    }
}
