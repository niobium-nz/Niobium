using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Channel
{
    public static class IViewModelExtensions
    {
        public static IList<TViewModel> Refresh<TEntity, TViewModel, TDomain>(
            this IList<TViewModel> existings,
            IEnumerable<TDomain> refreshments,
            Func<TViewModel> createViewModel, TEntity dummy)
            where TViewModel : IViewModel<TDomain, TEntity>
            where TDomain : IChannelDomain<TEntity>
            where TEntity : IEntity
        {
            if (existings == null)
            {
                existings = new List<TViewModel>();
            }

            foreach (var refreshment in refreshments)
            {
                var changed = existings.SingleOrDefault(e =>
                    e.PartitionKey == refreshment.PartitionKey
                    && e.RowKey == refreshment.RowKey
                    && e.ETag != refreshment.Entity.ETag);
                if (changed != null)
                {
                    changed.Initialize(refreshment);
                }

                var added = !existings.Any(e =>
                    e.PartitionKey == refreshment.PartitionKey
                    && e.RowKey == refreshment.RowKey);

                if (added)
                {
                    var vm = (TViewModel)createViewModel().Initialize(refreshment);
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
