using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Niobium
{
    public abstract class JsonSetting
    {
        public static JsonSerializerSettings Default { get; set; } = CamelCase!;

        public static readonly JsonSerializerSettings PascalCase = new() { ContractResolver = new DefaultContractResolver() };

        public static readonly JsonSerializerSettings CamelCase = new() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public static readonly JsonSerializerSettings UnderstoreCase = new() { ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() } };

        public static readonly JsonSerializerSettings TypedPascalCase = new() { TypeNameHandling = TypeNameHandling.Auto };

        public static readonly JsonSerializerSettings TypedCamelCase = new() { ContractResolver = new CamelCasePropertyNamesContractResolver(), TypeNameHandling = TypeNameHandling.Auto };
    }
}
