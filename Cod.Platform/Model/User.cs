using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;

namespace Cod.Platform.Model
{
    public class User : Cod.User, ITableEntity
    {
        public static User Reverse(User input)
            => new User
            {
                IsBusinessThePartitionKey = !input.IsBusinessThePartitionKey,
                Created = input.Created,
                Disabled = input.Disabled,
                FirstIP = input.FirstIP,
                LastIP = input.LastIP,
                PartitionKey = input.RowKey,
                RowKey = input.PartitionKey,
                Roles = input.Roles,
            };

        public User Reverse() => Reverse(this);

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext) => TableEntityHelper.ReflectionRead(this, properties, operationContext);

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext) => TableEntityHelper.ReflectionWrite(this, operationContext);
    }
}
