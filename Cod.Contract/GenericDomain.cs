using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod
{
    public abstract class GenericDomain<T> : IDomain<T> where T : IEntity
    {
        private readonly Lazy<IEnumerable<IEventHandler<IDomain<T>>>> eventHandlers;

        public bool Initialized { get; protected set; }

        public abstract string PartitionKey { get; }

        public abstract string RowKey { get; }

        public GenericDomain(Lazy<IEnumerable<IEventHandler<IDomain<T>>>> eventHandlers) => this.eventHandlers = eventHandlers;

        public IDomain<T> Initialize(T entity)
        {
            if (!this.Initialized)
            {
                this.OnInitialize(entity);
            }
            this.Initialized = true;
            return this;
        }

        protected virtual void OnInitialize(T entity) { }

        protected async Task TriggerAsync(object e)
        {
            foreach (var eventHandler in this.eventHandlers.Value)
            {
                await eventHandler.HandleAsync(this, e);
            }
        }
    }
}
