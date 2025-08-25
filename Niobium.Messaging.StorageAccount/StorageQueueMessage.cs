namespace Niobium.Messaging.StorageAccount
{
    internal sealed class StorageQueueMessage<T>(Func<Task> asyncDispose) : MessagingEntry<T> where T : class, new()
    {
        protected override async ValueTask DisposeAsync(bool disposing)
        {
            await asyncDispose();
        }
    }
}
