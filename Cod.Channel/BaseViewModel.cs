namespace Cod.Channel
{
    public abstract class BaseViewModel<TDomain, TEntity> : IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
            where TEntity : class
    {
        public string PartitionKey => this.Domain.PartitionKey;

        public string RowKey => this.Domain.RowKey;

        public IRefreshable Parent { get; private set; }

        protected bool UIRefreshableInitialized { get; private set; }

        protected TDomain Domain { get; private set; }

        protected bool DomainInitialized { get; private set; }

        public async Task<string> GetHashAsync(CancellationToken cancellationToken = default)
        {
            return await Domain.GetHashAsync(cancellationToken);
        }

        public async Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain, IRefreshable parent = null, bool force = false, CancellationToken cancellationToken = default)
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
                await this.OnInitializeAsync(cancellationToken);
            }

            return this;
        }

        protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
