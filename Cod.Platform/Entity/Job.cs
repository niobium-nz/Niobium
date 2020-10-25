using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;

namespace Cod.Platform
{
    public class Job : Cod.Model.Job, ITableEntity
    {

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext) => TableEntityHelper.ReflectionRead(this, properties, operationContext);

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext) => TableEntityHelper.ReflectionWrite(this, operationContext);
    }
}
