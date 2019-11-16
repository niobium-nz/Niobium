using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public abstract class DomainEventHandler<TDomain, TEntity, TEventArgs> : IEventHandler<TEntity> where TDomain : IDomain<TEntity>
    {
        public async Task HandleAsync(IDomain<TEntity> sender, object e, ILogger logger)
        {
            if (sender is TDomain && e is TEventArgs)
            {
                await this.HandleAsync((TDomain)sender, (TEventArgs)e, logger);
            }
        }

        protected abstract Task HandleAsync(TDomain sender, TEventArgs e, ILogger logger);
    }
}
