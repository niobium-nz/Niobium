using System.Threading.Tasks;

namespace Cod.Contract
{
    public abstract class DomainEventHandler<TDomain, TEventArgs> : IEventHandler<TDomain>
    {
        public async Task HandleAsync(TDomain sender, object e)
        {
            if (e is TEventArgs)
            {
                await this.CoreHandleAsync(sender, (TEventArgs)e);
            }
        }

        protected abstract Task CoreHandleAsync(TDomain sender, TEventArgs e);
    }

    public abstract class DomainEventHandler<TDomain> : IEventHandler<TDomain>
    {
        public async Task HandleAsync(TDomain sender, object e) => await this.CoreHandleAsync(sender, e);

        protected abstract Task CoreHandleAsync(TDomain sender, object e);
    }
}
