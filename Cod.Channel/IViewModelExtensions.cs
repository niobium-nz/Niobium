namespace Cod.Channel
{
    public static class IViewModelExtensions
    {
        public static async Task<IList<TViewModel>> RefreshAsync<TEntity, TViewModel, TDomain>(
            this IList<TViewModel> existings,
            IEnumerable<TDomain> refreshments,
            ObjectFactory<TViewModel> viewModelFactory,
            TEntity? _,
            IRefreshable? parent = null,
            CancellationToken cancellationToken = default)
                where TViewModel : class, IViewModel<TDomain, TEntity>
                where TDomain : IDomain<TEntity>
        {
            existings ??= [];

            foreach (TDomain refreshment in refreshments)
            {
                TViewModel? changed = null;
                IEnumerable<TViewModel> changes = existings.Where(e =>
                    e.PartitionKey == refreshment.PartitionKey
                    && e.RowKey == refreshment.RowKey);
                int count = changes.Count();
                if (count == 1)
                {
                    changed = changes.Single();
                }
                else if (count > 1)
                {
                    foreach (TViewModel? change in changes)
                    {
                        string? existingETag = await change.GetHashAsync(cancellationToken);
                        string? newETag = await refreshment.GetHashAsync(cancellationToken);
                        if (existingETag == newETag)
                        {
                            changed = change;
                            break;
                        }
                    }
                }

                if (changed != null)
                {
                    await changed.InitializeAsync(domain: refreshment, parent: parent, force: true, cancellationToken: cancellationToken);
                }

                bool added = !existings.Any(e =>
                    e.PartitionKey == refreshment.PartitionKey
                    && e.RowKey == refreshment.RowKey);

                if (added)
                {
                    TViewModel vm = (TViewModel)await viewModelFactory.Build().InitializeAsync(domain: refreshment, parent: parent, force: false, cancellationToken: cancellationToken);
                    existings.Add(vm);
                }
            }

            for (int i = existings.Count - 1; i >= 0; i--)
            {
                bool removed = !refreshments.Any(e =>
                    e.PartitionKey == existings[i].PartitionKey
                    && e.RowKey == existings[i].RowKey);

                if (removed)
                {
                    existings.Remove(existings[i]);
                }
            }

            return existings;
        }
    }
}
