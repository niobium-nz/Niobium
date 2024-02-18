using System.Threading.Tasks;

namespace Cod
{
    public abstract class DomainEventHandler<TDomain, TEventArgs> : IEventHandler<TDomain>
    {
        public async Task HandleAsync(TDomain sender, object e)
        {
            if (e is TEventArgs args)
            {
                await CoreHandleAsync(sender, args);
            }
        }

        protected abstract Task CoreHandleAsync(TDomain sender, TEventArgs e);
    }

    public abstract class DomainEventHandler<TDomain> : IEventHandler<TDomain>
    {
        public async Task HandleAsync(TDomain sender, object e)
        {
            await CoreHandleAsync(sender, e);
        }

        protected abstract Task CoreHandleAsync(TDomain sender, object e);
    }
}
