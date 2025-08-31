using System.Text.Json;

namespace Niobium
{
    public static class JsonMarshaller
    {
        public static string Marshall(object value, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase)
        {
            return format switch
            {
                JsonMarshallingFormat.PascalCase => JsonSerializer.Serialize(value, JsonMarshallingSettings.PascalCase),
                JsonMarshallingFormat.CamelCase => JsonSerializer.Serialize(value, JsonMarshallingSettings.CamelCase),
                JsonMarshallingFormat.SnakeCase => JsonSerializer.Serialize(value, JsonMarshallingSettings.SnakeCase),
                JsonMarshallingFormat.KebabCase => JsonSerializer.Serialize(value, JsonMarshallingSettings.KebabCase),
                _ => JsonSerializer.Serialize(value, JsonMarshallingSettings.CamelCase),
            };
        }

        public static Task MarshallAsync<T>(this Stream output, T value, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase, CancellationToken cancellationToken = default)
        {
            return format switch
            {
                JsonMarshallingFormat.PascalCase => JsonSerializer.SerializeAsync<T>(output, value, JsonMarshallingSettings.PascalCase, cancellationToken),
                JsonMarshallingFormat.CamelCase => JsonSerializer.SerializeAsync<T>(output, value, JsonMarshallingSettings.CamelCase, cancellationToken),
                JsonMarshallingFormat.SnakeCase => JsonSerializer.SerializeAsync<T>(output, value, JsonMarshallingSettings.SnakeCase, cancellationToken),
                JsonMarshallingFormat.KebabCase => JsonSerializer.SerializeAsync<T>(output, value, JsonMarshallingSettings.KebabCase, cancellationToken),
                _ => JsonSerializer.SerializeAsync<T>(output, value, JsonMarshallingSettings.CamelCase, cancellationToken),
            };
        }

        public static Task MarshallAsync(this Stream output, object value, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase, CancellationToken cancellationToken = default)
        {
            return output.MarshallAsync<object>(value, format, cancellationToken);
        }

        public static T Unmarshall<T>(string value, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase)
        {
            T? result = format switch
            {
                JsonMarshallingFormat.PascalCase => JsonSerializer.Deserialize<T>(value, JsonMarshallingSettings.PascalCase),
                JsonMarshallingFormat.CamelCase => JsonSerializer.Deserialize<T>(value, JsonMarshallingSettings.CamelCase),
                JsonMarshallingFormat.SnakeCase => JsonSerializer.Deserialize<T>(value, JsonMarshallingSettings.SnakeCase),
                JsonMarshallingFormat.KebabCase => JsonSerializer.Deserialize<T>(value, JsonMarshallingSettings.KebabCase),
                _ => JsonSerializer.Deserialize<T>(value, JsonMarshallingSettings.CamelCase),
            };

            return result ?? throw new InvalidCastException($"Deserialization of {typeof(T).Name} from {value} resulted in null.");
        }

        public static async Task<T> UnmarshallAsync<T>(this Stream input, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase, CancellationToken cancellationToken = default)
        {
            ValueTask<T?> task = format switch
            {
                JsonMarshallingFormat.PascalCase => JsonSerializer.DeserializeAsync<T>(input, JsonMarshallingSettings.PascalCase, cancellationToken),
                JsonMarshallingFormat.CamelCase => JsonSerializer.DeserializeAsync<T>(input, JsonMarshallingSettings.CamelCase, cancellationToken),
                JsonMarshallingFormat.SnakeCase => JsonSerializer.DeserializeAsync<T>(input, JsonMarshallingSettings.SnakeCase, cancellationToken),
                JsonMarshallingFormat.KebabCase => JsonSerializer.DeserializeAsync<T>(input, JsonMarshallingSettings.KebabCase, cancellationToken),
                _ => JsonSerializer.DeserializeAsync<T>(input, JsonMarshallingSettings.CamelCase, cancellationToken),
            };

            return await task ?? throw new InvalidCastException($"Deserialization of {typeof(T).Name} resulted in null.");
        }

        public static IAsyncEnumerable<T?> UnmarshallAsyncEnumerable<T>(this Stream input, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase, CancellationToken cancellationToken = default)
        {
            return format switch
            {
                JsonMarshallingFormat.PascalCase => JsonSerializer.DeserializeAsyncEnumerable<T>(input, JsonMarshallingSettings.PascalCase, cancellationToken),
                JsonMarshallingFormat.CamelCase => JsonSerializer.DeserializeAsyncEnumerable<T>(input, JsonMarshallingSettings.CamelCase, cancellationToken),
                JsonMarshallingFormat.SnakeCase => JsonSerializer.DeserializeAsyncEnumerable<T>(input, JsonMarshallingSettings.SnakeCase, cancellationToken),
                JsonMarshallingFormat.KebabCase => JsonSerializer.DeserializeAsyncEnumerable<T>(input, JsonMarshallingSettings.KebabCase, cancellationToken),
                _ => JsonSerializer.DeserializeAsyncEnumerable<T>(input, JsonMarshallingSettings.CamelCase, cancellationToken),
            };
        }

        public static async Task<object> UnmarshallAsync(this Stream input, Type targetType, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase, CancellationToken cancellationToken = default)
        {
            ValueTask<object?> task = format switch
            {
                JsonMarshallingFormat.PascalCase => JsonSerializer.DeserializeAsync(input, targetType, JsonMarshallingSettings.PascalCase, cancellationToken),
                JsonMarshallingFormat.CamelCase => JsonSerializer.DeserializeAsync(input, targetType, JsonMarshallingSettings.CamelCase, cancellationToken),
                JsonMarshallingFormat.SnakeCase => JsonSerializer.DeserializeAsync(input, targetType, JsonMarshallingSettings.SnakeCase, cancellationToken),
                JsonMarshallingFormat.KebabCase => JsonSerializer.DeserializeAsync(input, targetType, JsonMarshallingSettings.KebabCase, cancellationToken),
                _ => JsonSerializer.DeserializeAsync(input, targetType, JsonMarshallingSettings.CamelCase, cancellationToken),
            };

            return await task ?? throw new InvalidCastException($"Deserialization of {targetType.Name} resulted in null.");
        }

        public static object Unmarshall(string value, Type targetType, JsonMarshallingFormat format = JsonMarshallingFormat.CamelCase)
        {
            object? result = format switch
            {
                JsonMarshallingFormat.PascalCase => JsonSerializer.Deserialize(value, targetType, JsonMarshallingSettings.PascalCase),
                JsonMarshallingFormat.CamelCase => JsonSerializer.Deserialize(value, targetType, JsonMarshallingSettings.CamelCase),
                JsonMarshallingFormat.SnakeCase => JsonSerializer.Deserialize(value, targetType, JsonMarshallingSettings.SnakeCase),
                JsonMarshallingFormat.KebabCase => JsonSerializer.Deserialize(value, targetType, JsonMarshallingSettings.KebabCase),
                _ => JsonSerializer.Deserialize(value, targetType, JsonMarshallingSettings.CamelCase),
            };

            return result ?? throw new InvalidCastException($"Deserialization of {targetType.Name} from {value} resulted in null.");
        }
    }
}
