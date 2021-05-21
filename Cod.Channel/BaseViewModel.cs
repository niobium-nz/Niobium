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

        public IUIRefreshable Parent { get; private set; }

        protected bool UIRefreshableInitialized { get; private set; }

        protected TDomain Domain { get; private set; }

        protected bool DomainInitialized { get; private set; }

        public async Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain, IUIRefreshable parent = null, bool force = false)
        {
            var shouldNotify = false;

            if (force || !this.DomainInitialized)
            {
                this.Domain = domain;
                shouldNotify = true;
                this.DomainInitialized = true;
            }
            if (force || !this.UIRefreshableInitialized)
            {
                this.Parent = parent;
                shouldNotify = true;
                this.UIRefreshableInitialized = true;
            }
            if (shouldNotify)
            {
                await this.OnInitializeAsync();
            }

            return this;
        }

        protected virtual Task OnInitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
