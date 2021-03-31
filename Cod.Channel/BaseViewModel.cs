using System;
using System.Threading.Tasks;

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

        public async Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain)
        {
            this.Domain = domain;
            await this.OnInitializeAsync();
            return this;
        }

        protected virtual Task OnInitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
