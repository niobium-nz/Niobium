using System;
using System.Collections.Generic;
using Cod.Contract;

namespace Cod.Channel
{
    public abstract class ChannelDomain<T> : GenericDomain<T>, IChannelDomain<T> where T : IEntity
    {
        public ChannelDomain(Lazy<IEnumerable<IEventHandler<IDomain<T>>>> eventHandlers)
            : base(eventHandlers)
        {
        }

        public T Entity { get; private set; }

        public override string PartitionKey => this.Entity.PartitionKey;

        public override string RowKey => this.Entity.RowKey;

        protected override void OnInitialize(T entity) => this.Entity = entity;
    }
}
