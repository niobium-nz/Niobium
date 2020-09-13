using System;

namespace Cod.Channel
{
    public abstract class BaseViewModel<TDomain, TEntity> : IViewModel<TDomain, TEntity>
            where TDomain : ChannelDomain<TEntity>
            where TEntity : IEntity
    {
        public string PartitionKey => this.Domain.Entity.PartitionKey;

        public string RowKey => this.Domain.Entity.RowKey;

        public string ETag => this.Domain.Entity.ETag;

        public DateTimeOffset? Created => this.Domain.Entity.Created;

        protected TDomain Domain { get; private set; }

        public IViewModel<TDomain, TEntity> Initialize(TDomain domain)
        {
            this.Domain = domain;
            this.OnInitialize();
            return this;
        }

        protected virtual void OnInitialize()
        {
        }
    }
}
