using System.Text.Json;

namespace Cod.Messaging
{
    public class MessagingEntry<T> : IAsyncDisposable
    {
        private static readonly JsonSerializerOptions serializationOptions = new(JsonSerializerDefaults.Web);
        private bool disposed;

        public string Body { get; set; }

        public DateTimeOffset? Schedule { get; set; }

        public string ID { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }

        public T Value 
        { 
            get => System.Text.Json.JsonSerializer.Deserialize<T>(Body, serializationOptions); 
            set => Body = System.Text.Json.JsonSerializer.Serialize(value, serializationOptions); 
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                await DisposeAsync(true);
            }

            disposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual ValueTask DisposeAsync(bool disposing) => ValueTask.CompletedTask;
    }
}
