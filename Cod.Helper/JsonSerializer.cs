using Newtonsoft.Json;

namespace Cod
{
    public static class JsonSerializer
    {
        public static string SerializeObject(object value, JsonSerializationFormat? format = null)
        {
            return format switch
            {
                JsonSerializationFormat.PascalCase => JsonConvert.SerializeObject(value, JsonSetting.PascalCase),
                JsonSerializationFormat.CamelCase => JsonConvert.SerializeObject(value, JsonSetting.CamelCase),
                JsonSerializationFormat.UnderstoreCase => JsonConvert.SerializeObject(value, JsonSetting.UnderstoreCase),
                JsonSerializationFormat.TypedPascalCase => JsonConvert.SerializeObject(value, JsonSetting.TypedPascalCase),
                JsonSerializationFormat.TypedCamelCase => JsonConvert.SerializeObject(value, JsonSetting.TypedCamelCase),
                _ => JsonConvert.SerializeObject(value, JsonSetting.Default),
            };
        }

        public static T DeserializeObject<T>(string value, JsonSerializationFormat? format = null)
        {
            return format switch
            {
                JsonSerializationFormat.PascalCase => JsonConvert.DeserializeObject<T>(value, JsonSetting.PascalCase)!,
                JsonSerializationFormat.CamelCase => JsonConvert.DeserializeObject<T>(value, JsonSetting.CamelCase)!,
                JsonSerializationFormat.UnderstoreCase => JsonConvert.DeserializeObject<T>(value, JsonSetting.UnderstoreCase)!,
                JsonSerializationFormat.TypedPascalCase => JsonConvert.DeserializeObject<T>(value, JsonSetting.TypedPascalCase)!,
                JsonSerializationFormat.TypedCamelCase => JsonConvert.DeserializeObject<T>(value, JsonSetting.TypedCamelCase)!,
                _ => JsonConvert.DeserializeObject<T>(value)!,
            };
        }

        public static object DeserializeObject(Type targetType, string value, JsonSerializationFormat? format = null)
        {
            return format switch
            {
                JsonSerializationFormat.PascalCase => JsonConvert.DeserializeObject(value, type: targetType, settings: JsonSetting.PascalCase)!,
                JsonSerializationFormat.CamelCase => JsonConvert.DeserializeObject(value, type: targetType, settings: JsonSetting.CamelCase)!,
                JsonSerializationFormat.UnderstoreCase => JsonConvert.DeserializeObject(value, type: targetType, settings: JsonSetting.UnderstoreCase)!,
                JsonSerializationFormat.TypedPascalCase => JsonConvert.DeserializeObject(value, type: targetType, settings: JsonSetting.TypedPascalCase)!,
                JsonSerializationFormat.TypedCamelCase => JsonConvert.DeserializeObject(value, type: targetType, settings: JsonSetting.TypedCamelCase)!,
                _ => JsonConvert.DeserializeObject(value, targetType)!,
            };
        }
    }
}
