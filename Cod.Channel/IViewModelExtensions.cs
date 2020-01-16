using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Channel
{
    public static class IViewModelExtensions
    {
        public static IEnumerable<TViewModel> ToViewModel<TViewModel, TEntity>(this IEnumerable<IDomain<TEntity>> domains, Func<TViewModel> createViewModel)
            where TViewModel : IViewModel<TEntity>
            => domains.Select(d => (TViewModel)createViewModel().Initialize(d));
    }
}
