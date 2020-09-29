using System;
using System.Threading.Tasks;
using Cod.Model;

namespace Cod.Model
{
    public class DisposableQueueMessage : QueueMessage, IDisposable, IAsyncDisposable
    {
        private bool disposed;
        private readonly Action dispose;
        private readonly Func<Task> asyncDispose;

        public DisposableQueueMessage(Action dispose, Func<Task> asyncDispose)
        {
            this.dispose = dispose;
            this.asyncDispose = asyncDispose;
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Dispose(true);
            }

            this.disposed = true;
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!this.disposed)
            {
                await this.DisposeAsyncCore();
            }

            this.Dispose(false);
            this.disposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.dispose();
            }
        }

        protected virtual async ValueTask DisposeAsyncCore() => await this.asyncDispose();
    }
}
