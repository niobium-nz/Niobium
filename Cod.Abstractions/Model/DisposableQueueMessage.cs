using System;
using System.Threading.Tasks;

namespace Cod.Model
{
    public class DisposableQueueMessage : QueueMessage, IAsyncDisposable
    {
        private bool disposed;
        private readonly Func<Task> asyncDispose;

        public DisposableQueueMessage(Func<Task> asyncDispose)
        {
            this.asyncDispose = asyncDispose;
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                await DisposeAsyncCore();
            }

            disposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await asyncDispose();
        }
    }
}
