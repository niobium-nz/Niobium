using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cod
{
    public abstract class JsonSetting
    {
        public static JsonSerializerSettings Default { get; set; } = CamelCase;

        public static readonly JsonSerializerSettings PascalCase = new JsonSerializerSettings { ContractResolver = new DefaultContractResolver() };

        public static readonly JsonSerializerSettings CamelCase = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public static readonly JsonSerializerSettings UnderstoreCase = new JsonSerializerSettings { ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() } };

        public static readonly JsonSerializerSettings TypedPascalCase = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        public static readonly JsonSerializerSettings TypedCamelCase = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), TypeNameHandling = TypeNameHandling.Auto };
    }
}
