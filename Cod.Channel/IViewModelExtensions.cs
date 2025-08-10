namespace Cod.Channel
{
    public static class IViewModelExtensions
    {
        public static async Task<IList<TViewModel>> RefreshAsync<TEntity, TViewModel, TDomain>(
            this IList<TViewModel> existings,
            IEnumerable<TDomain> refreshments,
            Func<TViewModel> createViewModel,
            TEntity? _,
            IRefreshable? parent = null,
            CancellationToken cancellationToken = default)
                where TViewModel : class, IViewModel<TDomain, TEntity>
                where TDomain : IDomain<TEntity>
        {
            existings ??= [];

            foreach (var refreshment in refreshments)
            {
                TViewModel? changed = null;
                var changes = existings.Where(e =>
                    e.PartitionKey == refreshment.PartitionKey
                    && e.RowKey == refreshment.RowKey);
                var count = changes.Count();
                if (count == 1)
                {
                    changed = changes.Single();
                }
                else if (count > 1)
                {
                    foreach (var change in changes)
                    {
                        var existingETag = await change.GetHashAsync(cancellationToken);
                        var newETag = await refreshment.GetHashAsync(cancellationToken);
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

                var added = !existings.Any(e =>
                    e.PartitionKey == refreshment.PartitionKey
                    && e.RowKey == refreshment.RowKey);

                if (added)
                {
                    var vm = (TViewModel)(await createViewModel().InitializeAsync(domain: refreshment, parent: parent, force: false, cancellationToken: cancellationToken));
                    existings.Add(vm);
                }
            }

            for (var i = existings.Count - 1; i >= 0; i--)
            {
                var removed = !refreshments.Any(e =>
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
