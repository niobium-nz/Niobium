namespace Cod.Messaging.StorageAccount
{
    internal class StorageQueueMessage<T> : MessagingEntry<T> where T : class, new()
    {
        private readonly Func<Task> asyncDispose;

        public StorageQueueMessage(Func<Task> asyncDispose)
        {
            this.asyncDispose = asyncDispose;
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            await asyncDispose();
        }
    }
}
