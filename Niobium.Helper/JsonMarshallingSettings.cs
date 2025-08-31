using System.Text.Json;

namespace Niobium
{
    internal static class JsonMarshallingSettings
    {
        public static readonly JsonSerializerOptions PascalCase = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

        public static readonly JsonSerializerOptions CamelCase = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static readonly JsonSerializerOptions SnakeCase = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        public static readonly JsonSerializerOptions KebabCase = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower };
    }
}
