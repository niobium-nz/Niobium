using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Channel
{
    public static class IViewModelExtensions
    {
        public static IEnumerable<TViewModel> ToViewModel<TDomain, TViewModel, TEntity>(this IEnumerable<TDomain> domains, Func<TViewModel> createViewModel)
            where TViewModel : IViewModel<TDomain, TEntity>
            where TDomain : IChannelDomain<TEntity>
            => domains.Select(d => (TViewModel)createViewModel().Initialize(d));
    }
}
