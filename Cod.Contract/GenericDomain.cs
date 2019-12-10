using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Contract
{
    public abstract class GenericDomain<T> : IDomain<T> where T : IEntity
    {
        private readonly Lazy<IEnumerable<IEventHandler<IDomain<T>>>> eventHandlers;

        public bool Initialized { get; private set; }

        public T Entity { get; private set; }

        public string PartitionKey => this.Entity.PartitionKey;

        public string RowKey => this.Entity.RowKey;

        public GenericDomain(Lazy<IEnumerable<IEventHandler<IDomain<T>>>> eventHandlers) => this.eventHandlers = eventHandlers;

        public IDomain<T> Initialize(T entity)
        {
            if (!this.Initialized)
            {
                this.Entity = entity;
            }
            this.Initialized = true;
            return this;
        }

        protected async Task TriggerAsync(object e)
        {
            foreach (var eventHandler in this.eventHandlers.Value)
            {
                await eventHandler.HandleAsync(this, e);
            }
        }
    }
}
