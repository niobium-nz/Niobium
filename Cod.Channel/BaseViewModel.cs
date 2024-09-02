using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class BaseViewModel<TDomain, TEntity> : IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
            where TEntity : class, new()
    {
        public string PartitionKey => this.Domain.PartitionKey;

        public string RowKey => this.Domain.RowKey;

        public IUIRefreshable Parent { get; private set; }

        protected bool UIRefreshableInitialized { get; private set; }

        protected TDomain Domain { get; private set; }

        protected bool DomainInitialized { get; private set; }

        public async Task<string> GetHashAsync()
        {
            return await Domain.GetHashAsync();
        }

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

        protected virtual Task OnInitializeAsync() => Task.CompletedTask;
    }
}
