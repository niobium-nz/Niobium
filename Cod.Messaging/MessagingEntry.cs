using System.Text.Json;

namespace Cod.Messaging
{
    public class MessagingEntry<T> : IAsyncDisposable
    {
        private static readonly JsonSerializerOptions serializationOptions = new(JsonSerializerDefaults.Web);
        private T? value;
        private bool disposed;

        public string Body { get; set; } = "{}";

        public string? Type { get; set; }

        public DateTimeOffset? Schedule { get; set; }

        public string ID { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }

        public T Value
        {
            get
            {
                if (value == null)
                {
                    if (Type == null)
                    {
                        value = System.Text.Json.JsonSerializer.Deserialize<T>(Body, serializationOptions)!;
                    }
                    else
                    {
                        var type = System.Type.GetType(Type);
                        value = (T)System.Text.Json.JsonSerializer.Deserialize(Body, type!, serializationOptions)!;
                    }
                }

                return value;
            }

            set
            {
                if (value == null)
                {
                    Body = "{}";
                    Type = null;
                }
                else
                {
                    Body = System.Text.Json.JsonSerializer.Serialize(value, serializationOptions);
                    Type = $"{value.GetType().FullName}, {value.GetType().Assembly.GetName().Name}";
                }
            }
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
