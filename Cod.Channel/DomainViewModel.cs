using System.Diagnostics.CodeAnalysis;

namespace Cod.Channel
{
    public abstract class DomainViewModel<TDomain, TEntity>(ILoadingStateService loadingStateService)
        : BaseViewModel, IViewModel<TDomain, TEntity>
            where TDomain : IDomain<TEntity>
            where TEntity : class
    {
        [MemberNotNullWhen(true, nameof(PartitionKey), nameof(RowKey), nameof(Domain), nameof(Entity))]
        public override bool IsInitialized => base.IsInitialized && DomainInitialized && UIRefreshableInitialized;

        public string? PartitionKey => Domain?.PartitionKey;

        public string? RowKey => Domain?.RowKey;

        public override bool IsBusy => loadingStateService.IsBusy(typeof(TEntity).Name, new StorageKey { PartitionKey = PartitionKey, RowKey = RowKey }.ToString());

        public TDomain? Domain { get; private set; }

        public TEntity? Entity { get; private set; }

        protected bool UIRefreshableInitialized { get; private set; }

        protected bool DomainInitialized { get; private set; }

        public async Task<string?> GetHashAsync(CancellationToken cancellationToken = default)
        {
            if (Domain == null)
            {
                return null;
            }

            return await Domain.GetHashAsync(cancellationToken);
        }

        public async Task<IViewModel<TDomain, TEntity>> InitializeAsync(TDomain domain, IRefreshable? parent = null, bool force = false, CancellationToken cancellationToken = default)
        {
            var shouldNotify = false;

            if (force || !DomainInitialized)
            {
                Domain = domain;
                shouldNotify = true;
                DomainInitialized = true;
            }

            if (force || !UIRefreshableInitialized)
            {
                shouldNotify = true;
                UIRefreshableInitialized = true;
            }

            if (Domain == null)
            {
                throw new InvalidOperationException("Domain cannot be initialized with valid value.");
            }

            Entity = await Domain.GetEntityAsync(cancellationToken)
                ?? throw new InvalidOperationException("Domain entity cannot be initialized with valid value.");

            await InitializeAsync(parent, cancellationToken);

            if (shouldNotify)
            {
                await OnInitializedAsync(cancellationToken);
            }

            return this;
        }

        protected virtual Task OnInitializedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
