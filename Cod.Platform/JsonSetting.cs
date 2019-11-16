using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cod.Platform
{
    public abstract class JsonSetting
    {
        public static readonly JsonSerializerSettings CamelCaseSetting = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public static readonly JsonSerializerSettings UnderstoreCaseSetting = new JsonSerializerSettings { ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() } };

        public static readonly JsonSerializerSettings TypedSetting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
    }
}
