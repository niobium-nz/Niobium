using System.Text.Json;

namespace Cod.Messaging
{
    public class MessagingEntry
    {
        public static MessagingEntry<T> Parse<T>(string json, Type? type = null)
        {
            MessagingEntry<T> result = new()
            { Body = json };

            if (type != null)
            {
                result.Type = MessagingEntry<T>.BuildTypeFullName(type);
            }

            return result;
        }
    }

    public class MessagingEntry<T> : IAsyncDisposable
    {
        public static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web);
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
                        value = System.Text.Json.JsonSerializer.Deserialize<T>(Body, SerializationOptions)!;
                    }
                    else
                    {
                        Type? type = System.Type.GetType(Type);
                        value = (T)System.Text.Json.JsonSerializer.Deserialize(Body, type!, SerializationOptions)!;
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
                    Body = System.Text.Json.JsonSerializer.Serialize(value, SerializationOptions);
                    Type = BuildTypeFullName(value.GetType());
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

        protected virtual ValueTask DisposeAsync(bool disposing)
        {
            return ValueTask.CompletedTask;
        }

        internal static string BuildTypeFullName(Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }
    }
}
