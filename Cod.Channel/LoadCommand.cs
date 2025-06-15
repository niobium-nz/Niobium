namespace Cod.Channel
{
    public class LoadCommand<TDomain, TEntity>(
        IDomainRepository<TDomain, TEntity> repository,
        ILoadingStateService loadingStateService
        )
        : ICommand<LoadCommandParameter, LoadCommandResult<TDomain>>
        where TDomain : class, IDomain<TEntity>
    {
        protected virtual IDisposable NewBusyScope(LoadCommandParameter parameter)
        {
            return loadingStateService.SetBusy(typeof(TEntity).Name, parameter.RowKeyStart ?? parameter.PartitionKeyStart);
        }

        public async Task<LoadCommandResult<TDomain>> ExecuteAsync(LoadCommandParameter parameter, CancellationToken cancellationToken = default)
        {
            if (parameter.PartitionKeyStart != parameter.PartitionKeyEnd || parameter.PartitionKeyStart == null)
            {
                throw new NotSupportedException("PartitionKeyStart and PartitionKeyEnd must be the same and are both not empty.");
            }

            if (parameter.RowKeyEnd != null)
            {
                if (parameter.RowKeyStart != parameter.RowKeyEnd)
                {
                    throw new NotSupportedException("RowKeyStart and RowKeyEnd must be the same if any of them is present.");
                }

                using (NewBusyScope(parameter))
                {
                    var singleLoaded = await repository.GetAsync(parameter.PartitionKeyStart!, parameter.RowKeyStart!, forceLoad: parameter.Force, cancellationToken: cancellationToken);
                    return new LoadCommandResult<TDomain> { DomainsLoaded = new List<TDomain> { singleLoaded } };
                }
            }

            if (parameter.Force || !repository.CachedPartitions.Contains(parameter.PartitionKeyStart))
            {
                using (NewBusyScope(parameter))
                {
                    var loaded = await repository.GetAsync(parameter.PartitionKeyStart!, forceLoad: false, cancellationToken: cancellationToken).ToListAsync(cancellationToken: cancellationToken);
                    return new LoadCommandResult<TDomain> { DomainsLoaded = loaded };
                }
            }

            return new LoadCommandResult<TDomain>
            {
                DomainsLoaded = repository.CachedDomains
                    .Where(d => d.PartitionKey == parameter.PartitionKeyStart)
                    .ToList()
            };
        }
    }
}
