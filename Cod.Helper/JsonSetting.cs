using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cod
{
    public abstract class JsonSetting
    {
        public static readonly JsonSerializerSettings CamelCase = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public static readonly JsonSerializerSettings UnderstoreCase = new JsonSerializerSettings { ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() } };

        public static readonly JsonSerializerSettings Typed = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
    }
}
